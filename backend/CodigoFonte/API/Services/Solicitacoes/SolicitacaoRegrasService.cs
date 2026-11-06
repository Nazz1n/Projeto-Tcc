using Captacao.Api.Infra;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Solicitacoes;

// Aplica as regras do RF07 (prioridade) e RF14 (status automático). As
// regras em si estão em Captacao.Domain.Regras (puras e testáveis); este
// serviço carrega os dados, aplica o resultado e registra histórico,
// auditoria e notificação do painel.
public class SolicitacaoRegrasService
{
    private readonly CaptacaoDbContext _context;
    private readonly AuditoriaService _auditoriaService;
    private readonly IPainelNotificador _notificador;

    public SolicitacaoRegrasService(
        CaptacaoDbContext context,
        AuditoriaService auditoriaService,
        IPainelNotificador notificador)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _notificador = notificador;
    }

    public Task<Solicitacao?> CarregarAsync(int solicitacaoId) =>
        _context.Solicitacoes
            .Include(s => s.Motorista)
            .Include(s => s.Veiculo)
            .Include(s => s.Carga)
            .FirstOrDefaultAsync(s => s.SolicitacaoId == solicitacaoId);

    public async Task<List<CampoFormulario>> ObterConfiguracaoAsync() =>
        await _context.CamposFormulario.AsNoTracking().ToListAsync();

    public async Task<AvaliacaoSolicitacao> AvaliarAsync(Solicitacao solicitacao)
    {
        var configuracao = await ObterConfiguracaoAsync();

        var documentos = await _context.Documentos
            .AsNoTracking()
            .Where(d => d.MotoristaId == solicitacao.MotoristaId)
            .ToListAsync();

        return AvaliadorSolicitacao.Avaliar(solicitacao, configuracao, documentos);
    }

    // Avalia várias solicitações com uma leitura só da configuração e dos
    // documentos (evita uma consulta por cartão do painel).
    public async Task<List<(Solicitacao Solicitacao, AvaliacaoSolicitacao Avaliacao)>>
        AvaliarVariasAsync(IReadOnlyCollection<Solicitacao> solicitacoes)
    {
        var configuracao = await ObterConfiguracaoAsync();
        var motoristaIds = solicitacoes.Select(s => s.MotoristaId).Distinct().ToList();

        var documentos = motoristaIds.Count == 0
            ? new List<Documento>()
            : await _context.Documentos.AsNoTracking()
                .Where(d => motoristaIds.Contains(d.MotoristaId))
                .ToListAsync();

        var porMotorista = documentos
            .GroupBy(d => d.MotoristaId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Documento>)g.ToList());

        return solicitacoes
            .Select(s => (s, AvaliadorSolicitacao.Avaliar(
                s, configuracao,
                porMotorista.TryGetValue(s.MotoristaId, out var docs)
                    ? docs : Array.Empty<Documento>())))
            .ToList();
    }

    // Muda o status e registra no histórico, SEM salvar (o chamador salva).
    public void AplicarStatus(Solicitacao solicitacao, string novoStatus, int? atendenteId)
    {
        var anterior = solicitacao.Status;

        solicitacao.Status = novoStatus;
        solicitacao.DataAtualizacao = DateTime.UtcNow;

        _context.HistoricosStatus.Add(new HistoricoStatus
        {
            Solicitacao = solicitacao,
            AtendenteId = atendenteId,
            StatusAnterior = anterior,
            NovoStatus = novoStatus,
            DataAlteracao = DateTime.UtcNow
        });
    }

    public async Task RecalcularAsync(int solicitacaoId)
    {
        var solicitacao = await CarregarAsync(solicitacaoId)
            ?? throw new KeyNotFoundException("Solicitação não encontrada.");

        await RecalcularAsync(solicitacao);
    }

    // Aplica a transição automática (se houver), recalcula a prioridade e
    // salva TUDO que estiver pendente no contexto. Devolve true se status ou
    // prioridade mudaram.
    public async Task<bool> RecalcularAsync(
        Solicitacao solicitacao,
        bool notificar = true,
        bool transicaoAutomatica = true)
    {
        var statusAntes = solicitacao.Status;
        var prioridadeAntes = solicitacao.Prioridade;

        var avaliacao = await AvaliarAsync(solicitacao);

        var proximo = transicaoAutomatica
            ? RegrasStatus.ProximoStatusAutomatico(solicitacao.Status, avaliacao)
            : null;

        if (proximo != null)
        {
            AplicarStatus(solicitacao, proximo, null);
            // A prioridade depende do status (formulário enviado ou não).
            avaliacao = await AvaliarAsync(solicitacao);
        }

        solicitacao.Prioridade = avaliacao.Prioridade;

        var statusMudou = solicitacao.Status != statusAntes;
        var prioridadeMudou = solicitacao.Prioridade != prioridadeAntes;

        if (prioridadeMudou && !statusMudou)
        {
            solicitacao.DataAtualizacao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        if (!statusMudou && !prioridadeMudou)
        {
            return false;
        }

        if (statusMudou)
        {
            await _auditoriaService.RegistrarAsync(
                "Status da solicitação alterado automaticamente",
                $"Solicitação ID: {solicitacao.SolicitacaoId}. " +
                $"Status anterior: {statusAntes}. Novo status: {solicitacao.Status}.");
        }

        if (prioridadeMudou)
        {
            await _auditoriaService.RegistrarAsync(
                "Prioridade da solicitação recalculada",
                $"Solicitação ID: {solicitacao.SolicitacaoId}. " +
                $"Prioridade anterior: {prioridadeAntes}. Nova prioridade: {solicitacao.Prioridade}.");
        }

        if (notificar)
        {
            await NotificarAsync(solicitacao, statusMudou);
        }

        return true;
    }

    public async Task NotificarAsync(Solicitacao solicitacao, bool statusMudou)
    {
        await _notificador.SolicitacaoAtualizadaAsync(solicitacao.SolicitacaoId);

        if (statusMudou && solicitacao.Status == StatusSolicitacao.ProntoParaAnalise)
        {
            await _notificador.SolicitacaoProntaParaAnaliseAsync(solicitacao.SolicitacaoId);
        }
    }

    // Documento sem solicitação vinculada (ou configuração alterada).
    public async Task RecalcularAbertasDoMotoristaAsync(int motoristaId)
    {
        var ids = await _context.Solicitacoes.AsNoTracking()
            .Where(s => s.MotoristaId == motoristaId &&
                        s.Status != StatusSolicitacao.Contratado &&
                        s.Status != StatusSolicitacao.Encerrado)
            .Select(s => s.SolicitacaoId)
            .ToListAsync();

        foreach (var id in ids) await RecalcularAsync(id);
    }

    public async Task RecalcularTodasAbertasAsync()
    {
        var ids = await _context.Solicitacoes.AsNoTracking()
            .Where(s => s.Status != StatusSolicitacao.Contratado &&
                        s.Status != StatusSolicitacao.Encerrado)
            .Select(s => s.SolicitacaoId)
            .ToListAsync();

        foreach (var id in ids) await RecalcularAsync(id);
    }
}
