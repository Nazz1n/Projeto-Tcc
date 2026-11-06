namespace Captacao.Api.DTOs.Solicitacoes;

public class HistoricoStatusRespostaDto
{
    public int HistoricoStatusId { get; set; }
    public int SolicitacaoId { get; set; }
    // Nulo quando a alteração foi feita pelo sistema.
    public int? AtendenteId { get; set; }
    public string Origem { get; set; } = "Sistema";
    public string? StatusAnterior { get; set; }
    public string NovoStatus { get; set; } = string.Empty;
    public DateTime DataAlteracao { get; set; }
}
