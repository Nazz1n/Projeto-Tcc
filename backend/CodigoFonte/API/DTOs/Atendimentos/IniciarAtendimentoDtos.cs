using System.ComponentModel.DataAnnotations;
using Captacao.Api.DTOs.LinksAcesso;

namespace Captacao.Api.DTOs.Atendimentos;

public class IniciarAtendimentoDto
{
    // Telefone do motorista, com ou sem DDI/formatação:
    // "(16) 99739-2221", "16997392221" ou "5516997392221".
    [Required(ErrorMessage = "Informe o telefone do motorista.")]
    [StringLength(25)]
    public string Telefone { get; set; } = string.Empty;
}

public class IniciarAtendimentoRespostaDto
{
    public int MotoristaId { get; set; }

    // Nulo até o motorista preencher o formulário.
    public string? NomeMotorista { get; set; }

    public bool MotoristaCriado { get; set; }

    public int SolicitacaoId { get; set; }

    // false = já existia uma solicitação aberta e ela foi reaproveitada.
    public bool SolicitacaoCriada { get; set; }

    public string StatusSolicitacao { get; set; } = string.Empty;

    // Link único recém-gerado (o anterior, se havia, foi invalidado), com
    // o texto sugerido e o endereço wa.me para o atendente enviar.
    public LinkAcessoRespostaDto Link { get; set; } = null!;
}
