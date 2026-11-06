using System.Globalization;
using System.Text;
using Captacao.Api.Services.Auditoria;
using Captacao.Api.Services.Motoristas;
using Captacao.Domain.Constantes;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Relatorios;

// RF10 (indicadores) e RF11 (exportação). A exportação é CSV: não há
// biblioteca de planilhas no projeto. O CPF sai sempre mascarado.
public class RelatorioService
{
    private readonly CaptacaoDbContext _context;
    private readonly CpfProtecaoService _cpf;
    private readonly AuditoriaService _auditoria;

    public RelatorioService(
        CaptacaoDbContext context,
        CpfProtecaoService cpf,
        AuditoriaService auditoria)
    {
        _context = context;
        _cpf = cpf;
        _auditoria = auditoria;
    }

    public async Task<IndicadoresGerenciais> ObterIndicadoresAsync(
        DateTime? de, DateTime? ate)
    {
        var consulta = _context.Solicitacoes.AsNoTracking().AsQueryable();

        if (de.HasValue) consulta = consulta.Where(s => s.DataCriacao >= de.Value);
        if (ate.HasValue) consulta = consulta.Where(s => s.DataCriacao < ate.Value.Date.AddDays(1));

        var solicitacoes = await consulta
            .Select(s => new SolicitacaoResumo(s.SolicitacaoId, s.Status, s.DataCriacao))
            .ToListAsync();

        var ids = solicitacoes.Select(s => s.SolicitacaoId).ToList();

        var historico = await _context.HistoricosStatus
            .AsNoTracking()
            .Where(h => ids.Contains(h.SolicitacaoId))
            .Select(h => new TransicaoResumo(h.SolicitacaoId, h.NovoStatus, h.DataAlteracao))
            .ToListAsync();

        return CalculadoraIndicadores.Calcular(solicitacoes, historico);
    }

    public async Task<byte[]> ExportarSolicitacoesCsvAsync(
        DateTime? de, DateTime? ate, string? status)
    {
        var consulta = _context.Solicitacoes.AsNoTracking()
            .Include(s => s.Motorista)
            .Include(s => s.Veiculo)
            .Include(s => s.Carga)
            .Include(s => s.Atendente)
            .AsQueryable();

        if (de.HasValue) consulta = consulta.Where(s => s.DataCriacao >= de.Value);
        if (ate.HasValue) consulta = consulta.Where(s => s.DataCriacao < ate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizado = StatusSolicitacao.Normalizar(status)
                ?? throw new ArgumentException("Status inválido.");
            consulta = consulta.Where(s => s.Status == normalizado);
        }

        var lista = await consulta.OrderBy(s => s.DataCriacao).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', new[]
        {
            "Solicitação", "Data de criação", "Status", "Prioridade", "Motorista",
            "CPF", "Telefone", "Placa", "Capacidade (t)", "Carga", "Atendente"
        }));

        foreach (var s in lista)
        {
            sb.AppendLine(string.Join(';', new[]
            {
                Celula(s.SolicitacaoId.ToString(CultureInfo.InvariantCulture)),
                Celula(s.DataCriacao.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                Celula(StatusSolicitacao.Rotulo(s.Status)),
                Celula(s.Prioridade),
                Celula(s.Motorista?.NomeCompleto),
                Celula(_cpf.Mascarar(s.Motorista?.Cpf)),
                Celula(s.Motorista?.Telefone),
                Celula(s.Veiculo?.Placa),
                Celula(s.Veiculo?.CapacidadeToneladas.ToString(CultureInfo.InvariantCulture)),
                Celula(s.Carga == null ? null : $"{s.Carga.Origem} → {s.Carga.Destino}"),
                Celula(s.Atendente?.Nome)
            }));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            .GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    public Task RegistrarExportacaoAsync(int? atendenteId, int linhas) =>
        _auditoria.RegistrarAsync(
            "Exportação de solicitações",
            $"Formato: CSV. Registros: {linhas}. CPF mascarado.",
            atendenteId);

    // Neutraliza injeção de fórmulas em planilhas e escapa o separador.
    internal static string Celula(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;

        var texto = valor.Replace("\r", " ").Replace("\n", " ");

        if ("=+-@\t".Contains(texto[0])) texto = "'" + texto;

        if (texto.Contains(';') || texto.Contains('"'))
            texto = "\"" + texto.Replace("\"", "\"\"") + "\"";

        return texto;
    }
}
