
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Veiculos;

public class VeiculoCadastroDto
{
    [Required(ErrorMessage = "O motorista é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "Informe um motorista válido.")]
    public int MotoristaId { get; set; }

    [Required(ErrorMessage = "A placa é obrigatória.")]
    [StringLength(8, MinimumLength = 7,
        ErrorMessage = "Informe a placa com 7 caracteres (hífen opcional).")]
    public string Placa { get; set; } = string.Empty;

    // Opcional: o formulário do motorista só coleta placa e capacidade.
    [StringLength(50)]
    public string? TipoVeiculo { get; set; }

    [Range(typeof(decimal), "0,01", "999999,99",
        ErrorMessage = "A capacidade deve ser maior que zero.")]
    public decimal CapacidadeToneladas { get; set; }
}
