
namespace Captacao.Api.DTOs.LinksAcesso;

public class LinkAcessoConsultaDto
{
    public int LinkAcessoId { get; set; }

    public int SolicitacaoId { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime DataExpiracao { get; set; }

    public DateTime? DataUtilizacao { get; set; }

    public DateTime? DataInvalidacao { get; set; }

    public bool Utilizavel { get; set; }
}
