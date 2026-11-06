using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Motoristas;

public class MotoristaCadastroDto
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(150)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(254)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Rntrc { get; set; }

    [StringLength(14)]
    public string? Cpf { get; set; }
}