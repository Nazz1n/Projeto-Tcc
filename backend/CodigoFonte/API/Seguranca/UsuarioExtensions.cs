using System.Security.Claims;

namespace Captacao.Api.Seguranca;

public static class UsuarioExtensions
{
    // Atendente autenticado, lido do JWT. Use SEMPRE este valor para
    // registrar "quem fez a ação": receber o id no corpo da requisição
    // permitiria registrar uma ação em nome de outra pessoa.
    public static int? ObterAtendenteId(this ClaimsPrincipal usuario)
    {
        var texto = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(texto, out var id) ? id : null;
    }
}
