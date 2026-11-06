using System.Text.RegularExpressions;

namespace Captacao.Domain.Regras;

public static class FormatacaoDados
{
    // Placa antiga (ABC1234) ou Mercosul (ABC1D23).
    private static readonly Regex PadraoPlaca =
        new("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.Compiled);

    public static string SomenteDigitos(string? texto) =>
        string.IsNullOrEmpty(texto)
            ? string.Empty
            : new string(texto.Where(char.IsDigit).ToArray());

    // O telefone é a identidade do contato (vem do WhatsApp): guardado só
    // com dígitos para "(16) 99128-4420" e "16991284420" serem o mesmo.
    public static string NormalizarTelefone(string? telefone) => SomenteDigitos(telefone);

    public static string AdicionarDdiBrasilSeNecessario(string? telefone)
    {
        var digitos = SomenteDigitos(telefone);
        var celularBrasileiroComDddDezenove =
            digitos.Length == 11 &&
            digitos.StartsWith('1') &&
            digitos[2] == '9';

        return digitos.Length == 10 || digitos.Length == 11 &&
            (!digitos.StartsWith('1') || celularBrasileiroComDddDezenove)
            ? "55" + digitos
            : digitos;
    }

    public static bool TelefoneValido(string normalizado) =>
        normalizado.Length is >= 10 and <= 15;

    public static string NormalizarPlaca(string? placa) =>
        (placa ?? string.Empty).Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();

    public static bool PlacaValida(string normalizada) => PadraoPlaca.IsMatch(normalizada);

    // 52998224725 -> ***.982.***-25 (RF13)
    public static string? MascararCpf(string? cpf)
    {
        var d = SomenteDigitos(cpf);
        return d.Length != 11 ? null : $"***.{d.Substring(3, 3)}.***-{d.Substring(9, 2)}";
    }
}
