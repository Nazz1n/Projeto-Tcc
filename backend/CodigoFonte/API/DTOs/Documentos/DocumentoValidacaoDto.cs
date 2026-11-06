using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Documentos;

public class DocumentoValidacaoDto
{
    [Required]
    [RegularExpression("^(Validado|Reprovado)$",
        ErrorMessage = "O status deve ser Validado ou Reprovado.")]
    public string StatusValidacao { get; set; } = string.Empty;
}
