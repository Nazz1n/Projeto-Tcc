using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Veiculos;

public class VeiculoAtualizacaoDto
{
    [Required(ErrorMessage = "A placa é obrigatória.")]
    [StringLength(8, MinimumLength = 7,
        ErrorMessage = "Informe a placa com 7 caracteres (hífen opcional).")]
    public string Placa { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TipoVeiculo { get; set; }

    [Range(typeof(decimal), "0,01", "999999,99",
        ErrorMessage = "A capacidade deve ser maior que zero.")]
    public decimal CapacidadeToneladas { get; set; }

    public bool Ativo { get; set; }
}