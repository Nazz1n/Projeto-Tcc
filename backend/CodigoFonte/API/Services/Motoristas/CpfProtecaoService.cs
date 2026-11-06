using System.Security.Cryptography;
using System.Text;
using Captacao.Domain.Regras;
using Microsoft.AspNetCore.DataProtection;

namespace Captacao.Api.Services.Motoristas;

public class CpfProtecaoService
{
    private readonly IDataProtector _protector;
    private readonly IConfiguration _configuration;

    public CpfProtecaoService(
        IDataProtectionProvider provider,
        IConfiguration configuration)
    {
        _protector = provider.CreateProtector("Captacao.Cpf.v1");
        _configuration = configuration;
    }

    public string Normalizar(string cpf)
    {
        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    public bool Validar(string cpf)
    {
        cpf = Normalizar(cpf);

        if (cpf.Length != 11)
            return false;

        if (cpf.Distinct().Count() == 1)
            return false;

        int[] numeros = cpf.Select(c => c - '0').ToArray();

        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += numeros[i] * (10 - i);

        int primeiroDigito = soma % 11;
        primeiroDigito = primeiroDigito < 2 ? 0 : 11 - primeiroDigito;

        if (numeros[9] != primeiroDigito)
            return false;

        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += numeros[i] * (11 - i);

        int segundoDigito = soma % 11;
        segundoDigito = segundoDigito < 2 ? 0 : 11 - segundoDigito;

        return numeros[10] == segundoDigito;
    }

    public string Criptografar(string cpf)
    {
        return _protector.Protect(cpf);
    }

    // CPF (11 dígitos), ou null se não puder ser lido (ex.: chave perdida).
    public string? Descriptografar(string? cpfCriptografado)
    {
        if (string.IsNullOrWhiteSpace(cpfCriptografado)) return null;

        try { return _protector.Unprotect(cpfCriptografado); }
        catch (CryptographicException) { return null; }
    }

    // ***.982.***-25 (RF13). Nunca devolve o CPF completo.
    public string? Mascarar(string? cpfCriptografado) =>
        FormatacaoDados.MascararCpf(Descriptografar(cpfCriptografado));

    public string GerarHash(string cpf)
    {
        var chave = _configuration["Seguranca:ChaveHashCpf"]
            ?? throw new InvalidOperationException(
                "Chave de hash do CPF não configurada.");

        var chaveBytes = Convert.FromBase64String(chave);
        var cpfBytes = Encoding.UTF8.GetBytes(cpf);

        using var hmac = new HMACSHA256(chaveBytes);

        return Convert.ToHexString(hmac.ComputeHash(cpfBytes));
    }
}