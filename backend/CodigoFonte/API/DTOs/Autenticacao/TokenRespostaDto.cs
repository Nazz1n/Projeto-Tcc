
namespace Captacao.Api.DTOs.Autenticacao;

public class TokenRespostaDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime DataExpiracao { get; set; }

    public int AtendenteId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;
}
