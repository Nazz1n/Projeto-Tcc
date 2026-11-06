namespace Captacao.Domain.Entidades;

public class CampoFormulario
{
    public int CampoFormularioId { get; set; }

    public string Chave { get; set; } = string.Empty;

    public string Rotulo { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public string TipoCampo { get; set; } = string.Empty;

    public bool Obrigatorio { get; set; } = false;

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}