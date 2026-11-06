
namespace Captacao.Api.DTOs.LinksAcesso;

public class LinkAcessoRespostaDto
{
    public int LinkAcessoId { get; set; }

    public int SolicitacaoId { get; set; }

    public string UrlAcesso { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; }

    public DateTime DataExpiracao { get; set; }

    // Texto pronto para o atendente colar no WhatsApp.
    public string MensagemSugerida { get; set; } = string.Empty;

    // Abre o WhatsApp já com o telefone do motorista e a mensagem
    // preenchida. Nulo se o telefone não permitir montar o endereço.
    public string? UrlWhatsApp { get; set; }
}
