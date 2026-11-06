
namespace Captacao.Domain.Entidades;

public class Documento
{
    public int DocumentoId { get; set; }

    public int MotoristaId { get; set; }

    public int? VeiculoId { get; set; }

    public int? SolicitacaoId { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;

    public string ChaveArquivo { get; set; } = string.Empty;

    public string NomeOriginal { get; set; } = string.Empty;

    public string TipoConteudo { get; set; } = string.Empty;

    public long TamanhoBytes { get; set; }

    public string StatusValidacao { get; set; } = "Pendente";

    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;

    public DateTime? DataValidacao { get; set; }

    public Motorista Motorista { get; set; } = null!;

    public Veiculo? Veiculo { get; set; }

    public Solicitacao? Solicitacao { get; set; }
}
