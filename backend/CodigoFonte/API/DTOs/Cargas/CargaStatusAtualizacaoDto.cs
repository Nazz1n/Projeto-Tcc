
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Cargas;

public class CargaStatusAtualizacaoDto
{
    [Required(ErrorMessage = "Informe o novo status.")]
    [StringLength(30)]
    public string NovoStatus { get; set; } = string.Empty;
}
