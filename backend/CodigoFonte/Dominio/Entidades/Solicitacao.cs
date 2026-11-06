namespace Captacao.Domain.Entidades;

public class Solicitacao
{
    public int SolicitacaoId { get; set; }

    public int MotoristaId { get; set; }

    public int? VeiculoId { get; set; }

    public int? CargaId { get; set; }

    public int? AtendenteId { get; set; }

    public string Status { get; set; } = "NovoContato";

    public string Prioridade { get; set; } = "Baixa";

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    // Relacionamentos
    public Motorista Motorista { get; set; } = null!;

    public Veiculo? Veiculo { get; set; }

    public Carga? Carga { get; set; }

    public Atendente? Atendente { get; set; }

    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();


    public ICollection<HistoricoStatus> HistoricosStatus { get; set; } = new List<HistoricoStatus>();

    public ICollection<LinkAcesso> LinksAcesso { get; set; } = new List<LinkAcesso>();
}
