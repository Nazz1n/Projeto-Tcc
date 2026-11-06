using Captacao.Api.Services.Atendimentos;
using Captacao.Api.Services.LinksAcesso;

namespace Captacao.Tests.Dominio;

public class AtendimentoTelefoneTests
{
    [Fact]
    public void SemDdi_GeraFormaCanonicaComDdiEAlternativaSemDdi() =>
        Assert.Equal(new[] { "5516997392221", "16997392221" },
            AtendimentoService.CandidatosDeTelefone("16997392221"));

    [Fact]
    public void ComDdi_GeraAlternativaSemDdi() =>
        Assert.Equal(new[] { "5516997392221", "16997392221" },
            AtendimentoService.CandidatosDeTelefone("5516997392221"));

    [Fact]
    public void FixoSemDdi_DezDigitos() =>
        Assert.Equal(new[] { "551633221100", "1633221100" },
            AtendimentoService.CandidatosDeTelefone("1633221100"));

    [Fact]
    public void NumeroEstrangeiro_FicaComoEstá() =>
        Assert.Equal(new[] { "15556731300" },
            AtendimentoService.CandidatosDeTelefone("15556731300"));

    [Fact]
    public void CelularComDddDezenove_EhNormalizadoComoBrasileiro() =>
        Assert.Equal(new[] { "5511991234567", "11991234567" },
            AtendimentoService.CandidatosDeTelefone("11991234567"));
}

public class LinkWhatsAppTests
{
    [Fact]
    public void UrlWhatsApp_AcrescentaDdiEEscapaTexto()
    {
        var url = LinkAcessoService.MontarUrlWhatsApp("16997392221", "Olá, tudo bem?\nhttps://x/y");

        Assert.NotNull(url);
        Assert.StartsWith("https://wa.me/5516997392221?text=", url);
        Assert.DoesNotContain("\n", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void UrlWhatsApp_TelefoneInvalido_RetornaNulo()
    {
        Assert.Null(LinkAcessoService.MontarUrlWhatsApp("123", "oi"));
        Assert.Null(LinkAcessoService.MontarUrlWhatsApp(null, "oi"));
    }

    [Fact]
    public void UrlWhatsApp_NumeroEstrangeiro_PreservaDdi()
    {
        var url = LinkAcessoService.MontarUrlWhatsApp("15556731300", "oi");

        Assert.StartsWith("https://wa.me/15556731300?text=", url);
    }

    [Fact]
    public void Mensagem_ContemLinkEValidade()
    {
        var texto = LinkAcessoService.MontarMensagem("https://site/acesso/abc", 24);

        Assert.Contains("https://site/acesso/abc", texto);
        Assert.Contains("24 horas", texto);
    }
}
