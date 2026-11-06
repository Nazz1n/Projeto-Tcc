
namespace Captacao.Api.DTOs.Documentos;

public class DocumentoRespostaDto
{
    public int DocumentoId { get; set; }

    public int MotoristaId { get; set; }

    public int? VeiculoId { get; set; }

    public int? SolicitacaoId { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;

    public string NomeOriginal { get; set; } = string.Empty;

    public string TipoConteudo { get; set; } = string.Empty;

    public long TamanhoBytes { get; set; }

    public string StatusValidacao { get; set; } = string.Empty;

    public DateTime DataEnvio { get; set; }

    public DateTime? DataValidacao { get; set; }
}
