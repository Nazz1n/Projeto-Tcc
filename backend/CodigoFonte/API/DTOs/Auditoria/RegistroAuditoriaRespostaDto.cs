
namespace Captacao.Api.DTOs.Auditoria;

public class RegistroAuditoriaRespostaDto
{
    public int RegistroAuditoriaId { get; set; }

    public int? AtendenteId { get; set; }

    public string? NomeAtendente { get; set; }

    public string Acao { get; set; } = string.Empty;

    public string? Detalhes { get; set; }

    public DateTime DataRegistro { get; set; }
}
