
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Atendentes;

public class AtendenteAtualizacaoDto
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;
}
