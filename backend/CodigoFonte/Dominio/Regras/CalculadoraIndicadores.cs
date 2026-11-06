using Captacao.Domain.Constantes;

namespace Captacao.Domain.Regras;

public sealed record SolicitacaoResumo(int SolicitacaoId, string Status, DateTime DataCriacao);
public sealed record TransicaoResumo(int SolicitacaoId, string NovoStatus, DateTime DataAlteracao);

public sealed class IndicadoresGerenciais
{
    public int TotalSolicitacoes { get; init; }
    public IReadOnlyDictionary<string, int> PorStatus { get; init; } = new Dictionary<string, int>();
    // Solicitações que chegaram a "Pronto para análise" (histórico de status).
    public int CadastrosCompletos { get; init; }
    public double PercentualCadastrosCompletos { get; init; }
    public int Contratadas { get; init; }
    public double TaxaConversaoPercentual { get; init; }
    public double? TempoMedioAteProntaParaAnaliseMinutos { get; init; }
    public double? TempoMedioTotalAtendimentoMinutos { get; init; }
}

public static class CalculadoraIndicadores
{
    public static IndicadoresGerenciais Calcular(
        IReadOnlyCollection<SolicitacaoResumo> solicitacoes,
        IReadOnlyCollection<TransicaoResumo> historico)
    {
        var total = solicitacoes.Count;
        var porStatus = StatusSolicitacao.Todos.ToDictionary(
            st => st, st => solicitacoes.Count(s => s.Status == st));

        var hist = historico.GroupBy(h => h.SolicitacaoId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.DataAlteracao).ToList());

        var ateProntaMin = new List<double>();
        var totalMin = new List<double>();
        var completos = 0;

        foreach (var s in solicitacoes)
        {
            if (!hist.TryGetValue(s.SolicitacaoId, out var trans)) continue;

            var pronta = trans.FirstOrDefault(t => t.NovoStatus == StatusSolicitacao.ProntoParaAnalise);
            if (pronta != null)
            {
                completos++;
                var m = (pronta.DataAlteracao - s.DataCriacao).TotalMinutes;
                if (m >= 0) ateProntaMin.Add(m);
            }

            if (StatusSolicitacao.EhFinal(s.Status))
            {
                var fim = trans.LastOrDefault(t => t.NovoStatus == s.Status);
                if (fim != null)
                {
                    var m = (fim.DataAlteracao - s.DataCriacao).TotalMinutes;
                    if (m >= 0) totalMin.Add(m);
                }
            }
        }

        var contratadas = porStatus[StatusSolicitacao.Contratado];

        return new IndicadoresGerenciais
        {
            TotalSolicitacoes = total,
            PorStatus = porStatus,
            CadastrosCompletos = completos,
            PercentualCadastrosCompletos = Pct(completos, total),
            Contratadas = contratadas,
            TaxaConversaoPercentual = Pct(contratadas, total),
            TempoMedioAteProntaParaAnaliseMinutos = ateProntaMin.Count > 0 ? Math.Round(ateProntaMin.Average(), 1) : null,
            TempoMedioTotalAtendimentoMinutos = totalMin.Count > 0 ? Math.Round(totalMin.Average(), 1) : null
        };
    }

    private static double Pct(int parte, int total) =>
        total == 0 ? 0 : Math.Round(parte * 100.0 / total, 1);
}
