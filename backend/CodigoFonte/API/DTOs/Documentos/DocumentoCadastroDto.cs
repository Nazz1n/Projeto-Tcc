
using System.ComponentModel.DataAnnotations;

namespace Captacao.Api.DTOs.Documentos;

public class DocumentoCadastroDto
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Informe um motorista válido.")]
    public int MotoristaId { get; set; }

    public int? VeiculoId { get; set; }

    public int? SolicitacaoId { get; set; }

    [Required(ErrorMessage = "O tipo do documento é obrigatório.")]
    [StringLength(50)]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "O arquivo é obrigatório.")]
    public IFormFile? Arquivo { get; set; }
}
