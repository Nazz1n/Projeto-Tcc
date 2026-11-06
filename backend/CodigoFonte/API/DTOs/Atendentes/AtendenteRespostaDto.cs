
namespace Captacao.Api.DTOs.Atendentes;

public class AtendenteRespostaDto
{
    public int AtendenteId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public bool Ativo { get; set; }
}
