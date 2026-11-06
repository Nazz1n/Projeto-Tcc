
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.LinksAcesso;

public class LinkAcessoValidacaoDto
{
    [Required(ErrorMessage = "Informe o token de acesso.")]
    public string Token { get; set; } = string.Empty;
}
