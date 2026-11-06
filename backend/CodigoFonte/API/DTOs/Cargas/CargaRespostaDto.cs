namespace Captacao.Api.DTOs.Cargas;

public class CargaRespostaDto
{
    public int CargaId { get; set; }

    public int AtendenteId { get; set; }

    public string Origem { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal? CapacidadeMinimaToneladas { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime DataCadastro { get; set; }
}