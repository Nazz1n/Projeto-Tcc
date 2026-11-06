using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Cargas;

public class CargaCadastroDto
{
    [Required(ErrorMessage = "Informe a origem.")]
    [StringLength(150)]
    public string Origem { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o destino.")]
    [StringLength(150)]
    public string Destino { get; set; } = string.Empty;

    [Range(0.01, 999999.99,
        ErrorMessage = "Informe uma capacidade mínima válida.")]
    public decimal? CapacidadeMinimaToneladas { get; set; }
}