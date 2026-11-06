namespace Captacao.Domain.Entidades;

public class Carga
{
    public int CargaId { get; set; }

    public int AtendenteId { get; set; }

    public string Origem { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal? CapacidadeMinimaToneladas { get; set; }

    public string Status { get; set; } = "Disponivel";

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public Atendente Atendente { get; set; } = null!;

    public ICollection<Solicitacao> Solicitacoes { get; set; } = new List<Solicitacao>();
}