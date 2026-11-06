
namespace Captacao.Api.DTOs.LinksAcesso;

public class LinkAcessoValidacaoRespostaDto
{
    public bool Valido { get; set; }

    public int SolicitacaoId { get; set; }

    public DateTime DataExpiracao { get; set; }

    public string Mensagem { get; set; } = string.Empty;
}
