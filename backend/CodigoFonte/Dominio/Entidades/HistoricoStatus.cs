namespace Captacao.Domain.Entidades;

public class HistoricoStatus
{
    public int HistoricoStatusId { get; set; }

    public int SolicitacaoId { get; set; }

    public int? AtendenteId { get; set; }

    // Nulo no registro de criação da solicitação.
    public string? StatusAnterior { get; set; }

    public string NovoStatus { get; set; } = string.Empty;

    public DateTime DataAlteracao { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public Solicitacao Solicitacao { get; set; } = null!;

    public Atendente? Atendente { get; set; }
}