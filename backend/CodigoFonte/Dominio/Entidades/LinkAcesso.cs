namespace Captacao.Domain.Entidades;

public class LinkAcesso
{
    public int LinkAcessoId { get; set; }

    public int SolicitacaoId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataExpiracao { get; set; }

    public DateTime? DataUtilizacao { get; set; }

    public DateTime? DataInvalidacao { get; set; }

    // Relacionamento
    public Solicitacao Solicitacao { get; set; } = null!;
}