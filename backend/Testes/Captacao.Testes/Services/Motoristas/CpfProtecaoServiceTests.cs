using Captacao.Api.Services.Motoristas;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace Captacao.Tests.Services.Motoristas;

public class CpfProtecaoServiceTests
{
    private readonly CpfProtecaoService _service;

    public CpfProtecaoServiceTests()
    {
        var provider = DataProtectionProvider.Create("Captacao.Tests");

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seguranca:ChaveHashCpf"] =
                    Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            })
            .Build();

        _service = new CpfProtecaoService(provider, configuracao);
    }

    [Fact]
    public void Normalizar_DeveRemoverPontosTracosEEspacos()
    {
        var resultado = _service.Normalizar("529.982.247-25");

        Assert.Equal("52998224725", resultado);
    }

    [Fact]
    public void Validar_DeveAceitarCpfValido()
    {
        var resultado = _service.Validar("529.982.247-25");

        Assert.True(resultado);
    }

    [Fact]
    public void Validar_DeveRejeitarCpfComDigitosIncorretos()
    {
        var resultado = _service.Validar("529.982.247-26");

        Assert.False(resultado);
    }

    [Fact]
    public void Validar_DeveRejeitarCpfComTodosOsDigitosIguais()
    {
        var resultado = _service.Validar("111.111.111-11");

        Assert.False(resultado);
    }

    [Fact]
    public void Validar_DeveRejeitarCpfComQuantidadeIncorretaDeDigitos()
    {
        var resultado = _service.Validar("123456789");

        Assert.False(resultado);
    }

    [Fact]
    public void Criptografar_DeveGerarValorDiferenteDoCpfOriginal()
    {
        var cpf = "52998224725";

        var resultado = _service.Criptografar(cpf);

        Assert.NotEqual(cpf, resultado);
    }

    [Fact]
    public void GerarHash_DeveGerarHashCom64Caracteres()
    {
        var resultado = _service.GerarHash("52998224725");

        Assert.Equal(64, resultado.Length);
    }
}