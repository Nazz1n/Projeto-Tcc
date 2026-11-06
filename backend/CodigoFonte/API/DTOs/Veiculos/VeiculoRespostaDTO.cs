
namespace Captacao.Api.DTOs.Veiculos;

public class VeiculoRespostaDto
{
    public int VeiculoId { get; set; }

    public int MotoristaId { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string? TipoVeiculo { get; set; }

    public decimal CapacidadeToneladas { get; set; }

    public bool Ativo { get; set; }
}
