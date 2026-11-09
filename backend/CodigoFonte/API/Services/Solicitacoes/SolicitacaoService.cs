using Captacao.Api.DTOs.Solicitacoes;
using Captacao.Api.Services.Auditoria;
using Captacao.Api.Services.Motoristas;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Solicitacoes;

public class SolicitacaoService
{
    private readonly CaptacaoDbContext _context;
    private readonly AuditoriaService _auditoriaService;
    private readonly SolicitacaoRegrasService _regras;
    private readonly CpfProtecaoService _cpfService;

    public SolicitacaoService(
        CaptacaoDbContext context,
        AuditoriaService auditoriaService,
        SolicitacaoRegrasService regras,
        CpfProtecaoService cpfService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _regras = regras;
        _cpfService = cpfService;
    }

    // ---------------------------------------------------------------
    // CRIAÇÃO
    // ---------------------------------------------------------------

    // Criação manual pela equipe. Status e prioridade são definidos pelo
    // sistema. Só pode haver uma solicitação aberta por motorista (RF01).
    public async Task<SolicitacaoRespostaDto> CriarAsync(
        SolicitacaoCadastroDto dto, int? atendenteId)
    {
        var motorista = await _context.Motoristas
            .FirstOrDefaultAsync(m => m.MotoristaId == dto.MotoristaId)
            ?? throw new ArgumentException("Motorista não encontrado.");

        Veiculo? veiculo = null;

        if (dto.VeiculoId.HasValue)
        {
            veiculo = await _context.Veiculos
                .FirstOrDefaultAsync(v => v.VeiculoId == dto.VeiculoId.Value)
                ?? throw new ArgumentException("Veículo não encontrado.");

            if (veiculo.MotoristaId != dto.MotoristaId)
                throw new ArgumentException("O veículo informado não pertence ao motorista.");

            if (!veiculo.Ativo)
                throw new ArgumentException("Não é possível utilizar um veículo inativo.");
        }

        Carga? carga = null;

        if (dto.CargaId.HasValue)
        {
            carga = await _context.Cargas
                .FirstOrDefaultAsync(c => c.CargaId == dto.CargaId.Value)
                ?? throw new ArgumentException("Carga não encontrada.");
        }

        var jaAberta = await _context.Solicitacoes.AnyAsync(s =>
            s.MotoristaId == dto.MotoristaId &&
            s.Status != StatusSolicitacao.Contratado &&
            s.Status != StatusSolicitacao.Encerrado);

        if (jaAberta)
            throw new InvalidOperationException(
                "Já existe uma solicitação em andamento para este motorista.");

        var solicitacao = await CriarInternoAsync(motorista, veiculo, carga, atendenteId);

        return await MontarRespostaAsync(solicitacao.SolicitacaoId);
    }

    // Início de atendimento (RF01): reaproveita a solicitação aberta do
    // motorista ou cria uma nova com "Novo contato".
    public async Task<(Solicitacao Solicitacao, bool Criada)> ObterOuCriarAbertaAsync(
        int motoristaId, int? atendenteId = null)
    {
        var aberta = await _context.Solicitacoes
            .Where(s => s.MotoristaId == motoristaId &&
                        s.Status != StatusSolicitacao.Contratado &&
                        s.Status != StatusSolicitacao.Encerrado)
            .OrderByDescending(s => s.DataCriacao)
            .FirstOrDefaultAsync();

        if (aberta != null) return (aberta, false);

        var motorista = await _context.Motoristas
            .FirstOrDefaultAsync(m => m.MotoristaId == motoristaId)
            ?? throw new KeyNotFoundException("Motorista não encontrado.");

        var nova = await CriarInternoAsync(motorista, null, null, atendenteId);

        return (nova, true);
    }

    private async Task<Solicitacao> CriarInternoAsync(
        Motorista motorista, Veiculo? veiculo, Carga? carga, int? atendenteId)
    {
        var solicitacao = new Solicitacao
        {
            Motorista = motorista,
            Veiculo = veiculo,
            Carga = carga,
            Status = StatusSolicitacao.NovoContato,
            Prioridade = PrioridadeSolicitacao.Baixa,
            DataCriacao = DateTime.UtcNow
        };

        _context.Solicitacoes.Add(solicitacao);

        // Primeiro registro do histórico: sem status anterior.
        _context.HistoricosStatus.Add(new HistoricoStatus
        {
            Solicitacao = solicitacao,
            AtendenteId = atendenteId,
            StatusAnterior = null,
            NovoStatus = StatusSolicitacao.NovoContato,
            DataAlteracao = solicitacao.DataCriacao
        });

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Solicitação criada",
            $"Solicitação ID: {solicitacao.SolicitacaoId}. " +
            $"Motorista ID: {solicitacao.MotoristaId}. " +
            $"Veículo ID: {solicitacao.VeiculoId?.ToString() ?? "Não informado"}. " +
            $"Carga ID: {solicitacao.CargaId?.ToString() ?? "Não informada"}.",
            atendenteId);

        // Calcula a prioridade inicial e avisa o painel.
        await _regras.RecalcularAsync(solicitacao);
        await _regras.NotificarAsync(solicitacao, false);

        return solicitacao;
    }

    // ---------------------------------------------------------------
    // CONSULTAS
    // ---------------------------------------------------------------

    public async Task<List<SolicitacaoRespostaDto>> ListarTodosAsync(
        string? status = null, string? prioridade = null)
    {
        var consulta = ConsultaBase();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normal = StatusSolicitacao.Normalizar(status)
                ?? throw new ArgumentException("Status informado inválido.");
            consulta = consulta.Where(s => s.Status == normal);
        }

        var solicitacoes = await consulta.ToListAsync();
        var avaliadas = await _regras.AvaliarVariasAsync(solicitacoes);

        var lista = avaliadas.Select(x => Mapear(x.Solicitacao, x.Avaliacao));

        if (!string.IsNullOrWhiteSpace(prioridade))
        {
            var p = prioridade.Trim();
            lista = lista.Where(s => s.Prioridade.Equals(p, StringComparison.OrdinalIgnoreCase));
        }

        return Ordenar(lista).ToList();
    }

    // Colunas do painel (RF06/RF14). Em cada coluna: prioridade
    // (Alta > Média > Baixa) e, dentro dela, data de abertura (mais antiga
    // primeiro), como definido no RF07.
    public async Task<List<KanbanColunaDto>> ObterKanbanAsync()
    {
        var todas = await ListarTodosAsync();

        return StatusSolicitacao.Todos
            .Select(status => new KanbanColunaDto
            {
                Status = status,
                Rotulo = StatusSolicitacao.Rotulo(status),
                Solicitacoes = todas.Where(s => s.Status == status).ToList()
            })
            .ToList();
    }

    public async Task<List<SolicitacaoRespostaDto>> ListarPorMotoristaAsync(int motoristaId)
    {
        var solicitacoes = await ConsultaBase()
            .Where(s => s.MotoristaId == motoristaId)
            .ToListAsync();

        var avaliadas = await _regras.AvaliarVariasAsync(solicitacoes);

        return avaliadas
            .Select(x => Mapear(x.Solicitacao, x.Avaliacao))
            .OrderByDescending(s => s.DataCriacao)
            .ToList();
    }

    public async Task<SolicitacaoDetalheDto?> BuscarPorIdAsync(int id)
    {
        var solicitacao = await _context.Solicitacoes
            .AsNoTracking()
            .Include(s => s.Motorista)
            .Include(s => s.Veiculo)
            .Include(s => s.Carga)
            .FirstOrDefaultAsync(s => s.SolicitacaoId == id);

        if (solicitacao == null) return null;

        var avaliacao = await _regras.AvaliarAsync(solicitacao);

        return MapearDetalhe(solicitacao, avaliacao);
    }

    public async Task<List<HistoricoStatusRespostaDto>> ListarHistoricoAsync(int solicitacaoId)
    {
        var existe = await _context.Solicitacoes.AnyAsync(s => s.SolicitacaoId == solicitacaoId);

        if (!existe) throw new KeyNotFoundException("Solicitação não encontrada.");

        var historicos = await _context.HistoricosStatus
            .AsNoTracking()
            .Where(h => h.SolicitacaoId == solicitacaoId)
            .OrderBy(h => h.DataAlteracao).ThenBy(h => h.HistoricoStatusId)
            .ToListAsync();

        return historicos.Select(h => new HistoricoStatusRespostaDto
        {
            HistoricoStatusId = h.HistoricoStatusId,
            SolicitacaoId = h.SolicitacaoId,
            AtendenteId = h.AtendenteId,
            Origem = h.AtendenteId.HasValue ? "Atendente" : "Sistema",
            StatusAnterior = h.StatusAnterior,
            NovoStatus = h.NovoStatus,
            DataAlteracao = h.DataAlteracao
        }).ToList();
    }

    // ---------------------------------------------------------------
    // AÇÕES DO ATENDENTE
    // ---------------------------------------------------------------

    // Só as transições manuais do RF14: Em análise, Contratado, Encerrado.
    public async Task<SolicitacaoDetalheDto> AtualizarStatusAsync(
        int solicitacaoId, SolicitacaoStatusAtualizacaoDto dto, int atendenteId)
    {
        var solicitacao = await _regras.CarregarAsync(solicitacaoId)
            ?? throw new KeyNotFoundException("Solicitação não encontrada.");

        var novoStatus = StatusSolicitacao.Normalizar(dto.NovoStatus)
            ?? throw new ArgumentException("Status informado inválido.");

        if (!RegrasStatus.TransicaoManualPermitida(solicitacao.Status, novoStatus, out var motivo))
            throw new ArgumentException(motivo ?? "Transição de status não permitida.");

        if (novoStatus == StatusSolicitacao.Contratado && solicitacao.Carga == null)
            throw new ArgumentException(
                "Vincule uma carga disponível à solicitação antes de contratar.");

        if (novoStatus == StatusSolicitacao.Contratado &&
            solicitacao.Carga!.Status != StatusCarga.Disponivel)
        {
            throw new ArgumentException(
                "A carga vinculada não está mais disponível. Selecione outra carga disponível.");
        }

        if (novoStatus == StatusSolicitacao.Contratado &&
            !RegrasStatus.ContratacaoPermitida(
                await _regras.AvaliarAsync(solicitacao),
                out var motivoContratacao))
        {
            throw new ArgumentException(
                motivoContratacao ?? "Não é possível contratar enquanto houver pendências.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var anterior = solicitacao.Status;
        var cargaContratada = novoStatus == StatusSolicitacao.Contratado
            ? solicitacao.Carga
            : null;

        _regras.AplicarStatus(solicitacao, novoStatus, atendenteId);

        if (cargaContratada != null)
            cargaContratada.Status = StatusCarga.Contratada;

        // Quem assume a solicitação passa a ser o responsável.
        if (novoStatus == StatusSolicitacao.EmAnalise)
            solicitacao.AtendenteId = atendenteId;

        // Salva e recalcula a prioridade (depende do status).
        await _regras.RecalcularAsync(solicitacao, notificar: false, transicaoAutomatica: false);

        await _auditoriaService.RegistrarAsync(
            "Status da solicitação alterado",
            $"Solicitação ID: {solicitacao.SolicitacaoId}. " +
            $"Status anterior: {anterior}. Novo status: {novoStatus}.",
            atendenteId);

        if (cargaContratada != null)
        {
            await _auditoriaService.RegistrarAsync(
                "Carga contratada pela solicitação",
                $"Carga {cargaContratada.CargaId} ({cargaContratada.Origem} → {cargaContratada.Destino}) marcada como contratada pela solicitação {solicitacao.SolicitacaoId}.",
                atendenteId);
        }

        await transaction.CommitAsync();
        await _regras.NotificarAsync(solicitacao, statusMudou: true);

        return (await BuscarPorIdAsync(solicitacaoId))!;
    }

    public async Task<SolicitacaoDetalheDto> AtualizarAtendenteAsync(
        int solicitacaoId, SolicitacaoAtendenteAtualizacaoDto dto, int? executorId)
    {
        var solicitacao = await _context.Solicitacoes
            .FirstOrDefaultAsync(s => s.SolicitacaoId == solicitacaoId)
            ?? throw new KeyNotFoundException("Solicitação não encontrada.");

        var existe = await _context.Atendentes
            .AnyAsync(a => a.AtendenteId == dto.AtendenteId && a.Ativo);

        if (!existe) throw new ArgumentException("Atendente não encontrado ou inativo.");

        var anterior = solicitacao.AtendenteId;

        if (anterior != dto.AtendenteId)
        {
            solicitacao.AtendenteId = dto.AtendenteId;
            solicitacao.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync(
                "Atendente atribuído à solicitação",
                $"Solicitação ID: {solicitacao.SolicitacaoId}. " +
                $"Atendente anterior: {anterior?.ToString() ?? "Nenhum"}. " +
                $"Novo atendente: {dto.AtendenteId}.",
                executorId);

            await _regras.NotificarAsync(solicitacao, false);
        }

        return (await BuscarPorIdAsync(solicitacaoId))!;
    }

    // Associa (ou remove) a carga e recalcula compatibilidade e
    // prioridade (RF07, RF15).
    public async Task<SolicitacaoDetalheDto> AtualizarCargaAsync(
        int solicitacaoId, SolicitacaoCargaAtualizacaoDto dto, int? atendenteId)
    {
        var solicitacao = await _regras.CarregarAsync(solicitacaoId)
            ?? throw new KeyNotFoundException("Solicitação não encontrada.");

        if (StatusSolicitacao.EhFinal(solicitacao.Status))
            throw new ArgumentException("A solicitação já foi finalizada e não pode mais ser alterada.");

        Carga? carga = null;

        if (dto.CargaId.HasValue)
        {
            carga = await _context.Cargas
                .FirstOrDefaultAsync(c => c.CargaId == dto.CargaId.Value)
                ?? throw new ArgumentException("Carga não encontrada.");

            if (carga.Status != StatusCarga.Disponivel &&
                solicitacao.CargaId != carga.CargaId)
                throw new ArgumentException("A carga selecionada não está disponível.");
        }

        var anterior = solicitacao.CargaId;

        solicitacao.CargaId = carga?.CargaId;
        solicitacao.Carga = carga;
        solicitacao.DataAtualizacao = DateTime.UtcNow;

        await _regras.RecalcularAsync(solicitacao);

        if (anterior != solicitacao.CargaId)
        {
            await _auditoriaService.RegistrarAsync(
                "Carga associada à solicitação",
                $"Solicitação ID: {solicitacao.SolicitacaoId}. " +
                $"Carga anterior: {anterior?.ToString() ?? "Nenhuma"}. " +
                $"Nova carga: {solicitacao.CargaId?.ToString() ?? "Nenhuma"}.",
                atendenteId);
        }

        return (await BuscarPorIdAsync(solicitacaoId))!;
    }

    // ---------------------------------------------------------------
    // APOIO
    // ---------------------------------------------------------------

    private IQueryable<Solicitacao> ConsultaBase() =>
        _context.Solicitacoes
            .AsNoTracking()
            .Include(s => s.Motorista)
            .Include(s => s.Veiculo)
            .Include(s => s.Carga);

    private async Task<SolicitacaoRespostaDto> MontarRespostaAsync(int id)
    {
        var s = await ConsultaBase().FirstAsync(x => x.SolicitacaoId == id);
        var avaliacao = await _regras.AvaliarAsync(s);
        return Mapear(s, avaliacao);
    }

    private static IEnumerable<SolicitacaoRespostaDto> Ordenar(
        IEnumerable<SolicitacaoRespostaDto> lista) =>
        lista
            .OrderBy(s => PrioridadeSolicitacao.Ordem(s.Prioridade))
            .ThenBy(s => s.DataCriacao);

    private static void Preencher(
        SolicitacaoRespostaDto dto, Solicitacao s, AvaliacaoSolicitacao a)
    {
        dto.SolicitacaoId = s.SolicitacaoId;
        dto.MotoristaId = s.MotoristaId;
        dto.NomeMotorista = s.Motorista?.NomeCompleto;
        dto.Telefone = s.Motorista?.Telefone ?? string.Empty;
        dto.VeiculoId = s.VeiculoId;
        dto.Placa = s.Veiculo?.Placa;
        dto.CapacidadeToneladas = s.Veiculo?.CapacidadeToneladas;
        dto.CargaId = s.CargaId;
        dto.CargaDescricao = s.Carga == null
            ? null
            : $"Carga #{s.Carga.CargaId} · {s.Carga.Origem} → {s.Carga.Destino}";
        dto.AtendenteId = s.AtendenteId;
        dto.Status = s.Status;
        dto.StatusRotulo = StatusSolicitacao.Rotulo(s.Status);
        // A prioridade exibida é a do cálculo atual.
        dto.Prioridade = a.Prioridade;
        dto.Compatibilidade = a.Compatibilidade.ToString();
        dto.PendenciasDocumentais = a.PendenciasDocumentais;
        dto.DataCriacao = s.DataCriacao;
        dto.DataAtualizacao = s.DataAtualizacao;
    }

    private static SolicitacaoRespostaDto Mapear(Solicitacao s, AvaliacaoSolicitacao a)
    {
        var dto = new SolicitacaoRespostaDto();
        Preencher(dto, s, a);
        return dto;
    }

    private SolicitacaoDetalheDto MapearDetalhe(Solicitacao s, AvaliacaoSolicitacao a)
    {
        var dto = new SolicitacaoDetalheDto
        {
            // CPF sempre parcialmente oculto (RF13).
            CpfMascarado = _cpfService.Mascarar(s.Motorista?.Cpf),
            Email = s.Motorista?.Email,
            DescricaoCompatibilidade = a.DescricaoCompatibilidade,
            CamposPendentes = a.CamposPendentes.ToList(),
            Documentos = a.Documentos.Select(d => new DocumentoAvaliadoDto
            {
                Chave = d.Chave,
                Rotulo = d.Rotulo,
                Obrigatorio = d.Obrigatorio,
                Situacao = d.Estado.ToString(),
                DocumentoId = d.DocumentoId,
                NomeOriginal = d.NomeOriginal,
                DataEnvio = d.DataEnvio,
                Reutilizado = d.Reutilizado
            }).ToList()
        };

        Preencher(dto, s, a);
        return dto;
    }
}
