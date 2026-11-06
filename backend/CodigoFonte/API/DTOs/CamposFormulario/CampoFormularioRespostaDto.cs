
namespace Captacao.Api.DTOs.CamposFormulario;

public class CampoFormularioRespostaDto
{
    public int CampoFormularioId { get; set; }

    public string Chave { get; set; } = string.Empty;

    public string Rotulo { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public string TipoCampo { get; set; } = string.Empty;

    public bool Obrigatorio { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; }
}
