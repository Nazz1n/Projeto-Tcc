
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Atendentes;

public class AtendenteCadastroDto
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(128, MinimumLength = 10,
        ErrorMessage = "A senha deve possuir entre 10 e 128 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}
