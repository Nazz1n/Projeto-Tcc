namespace Captacao.Domain.Entidades;

public class RegistroAuditoria
{
    public int RegistroAuditoriaId { get; set; }

    public int? AtendenteId { get; set; }

    public string Acao { get; set; } = string.Empty;

    public string? Detalhes { get; set; }

    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Relacionamento
    public Atendente? Atendente { get; set; }
}