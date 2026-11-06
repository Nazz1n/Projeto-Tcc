
namespace Captacao.Domain.Entidades;

public class Veiculo
{
    public int VeiculoId { get; set; }

    public int MotoristaId { get; set; }

    public string Placa { get; set; } = string.Empty;

    // Opcional: o formulário do motorista coleta apenas placa e capacidade (RF04).
    public string? TipoVeiculo { get; set; }

    public decimal CapacidadeToneladas { get; set; }

    public bool Ativo { get; set; } = true;

    // Relacionamento
    public Motorista Motorista { get; set; } = null!;

    public ICollection<Solicitacao> Solicitacoes { get; set; } = new List<Solicitacao>();

    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
}
