using System.Globalization;
using System.Text;

namespace Captacao.Domain.Constantes;

// Valores gravados no banco e trafegados pela API (estágios do RF14).
public static class StatusSolicitacao
{
    public const string NovoContato = "NovoContato";
    public const string AguardandoPreenchimento = "AguardandoPreenchimento";
    public const string ComPendencias = "ComPendencias";
    public const string ProntoParaAnalise = "ProntoParaAnalise";
    public const string EmAnalise = "EmAnalise";
    public const string Contratado = "Contratado";
    public const string Encerrado = "Encerrado";

    // Ordem das colunas do painel Kanban.
    public static readonly IReadOnlyList<string> Todos = new[]
    {
        NovoContato, AguardandoPreenchimento, ComPendencias,
        ProntoParaAnalise, EmAnalise, Contratado, Encerrado
    };

    private static readonly Dictionary<string, string> Rotulos = new()
    {
        [NovoContato] = "Novo contato",
        [AguardandoPreenchimento] = "Aguardando preenchimento",
        [ComPendencias] = "Com pendências",
        [ProntoParaAnalise] = "Pronto para análise",
        [EmAnalise] = "Em análise",
        [Contratado] = "Contratado",
        [Encerrado] = "Encerrado"
    };

    public static bool EhFinal(string status) =>
        status == Contratado || status == Encerrado;

    public static bool FormularioNaoConcluido(string status) =>
        status == NovoContato || status == AguardandoPreenchimento;

    public static string Rotulo(string status) =>
        Rotulos.TryGetValue(status, out var r) ? r : status;

    public static string? Normalizar(string? valor) =>
        TextoCanonico.Resolver(valor, Todos);
}

public static class PrioridadeSolicitacao
{
    public const string Alta = "Alta";
    public const string Media = "Media";
    public const string Baixa = "Baixa";

    public static int Ordem(string prioridade) => prioridade switch
    {
        Alta => 0,
        Media => 1,
        _ => 2
    };
}

public static class StatusDocumento
{
    public const string Pendente = "Pendente";
    public const string Validado = "Validado";
    public const string Reprovado = "Reprovado";

    public static readonly IReadOnlyList<string> Todos =
        new[] { Pendente, Validado, Reprovado };

    public static string? Normalizar(string? valor) =>
        TextoCanonico.Resolver(valor, Todos);
}

public static class StatusCarga
{
    public const string Disponivel = "Disponivel";
    public const string Contratada = "Contratada";

    public static readonly IReadOnlyList<string> Todos =
        new[] { Disponivel, Contratada };

    public static string? Normalizar(string? valor) =>
        TextoCanonico.Resolver(valor, Todos);
}

public static class CategoriaCampoFormulario
{
    public const string Campo = "Campo";
    public const string Documento = "Documento";
}

// Campos e documentos predefinidos do formulário (RF05). O administrador
// configura obrigatoriedade e ativação.
public static class CatalogoFormulario
{
    public const string Nome = "nome";
    public const string Cpf = "cpf";
    public const string Telefone = "telefone";
    public const string Email = "email";
    public const string Placa = "placa";
    public const string Capacidade = "capacidade";

    public const string Cnh = "cnh";
    public const string Crlv = "crlv";
    public const string ComprovanteRntrc = "comprovante_rntrc";
    public const string DocumentoAdicionalCarga = "documento_adicional_carga";

    public sealed record ItemPadrao(
        string Chave, string Rotulo, string Categoria,
        string TipoCampo, bool ObrigatorioPadrao);

    // Valores iniciais iguais aos da Figura 13 do protótipo.
    public static readonly IReadOnlyList<ItemPadrao> Padrao = new[]
    {
        new ItemPadrao(Nome, "Nome completo", CategoriaCampoFormulario.Campo, "text", true),
        new ItemPadrao(Cpf, "CPF", CategoriaCampoFormulario.Campo, "text", true),
        new ItemPadrao(Telefone, "Telefone para contato", CategoriaCampoFormulario.Campo, "tel", true),
        new ItemPadrao(Email, "E-mail", CategoriaCampoFormulario.Campo, "email", false),
        new ItemPadrao(Placa, "Placa do veículo", CategoriaCampoFormulario.Campo, "text", true),
        new ItemPadrao(Capacidade, "Capacidade em toneladas", CategoriaCampoFormulario.Campo, "number", true),
        new ItemPadrao(Cnh, "CNH", CategoriaCampoFormulario.Documento, "file", true),
        new ItemPadrao(Crlv, "CRLV", CategoriaCampoFormulario.Documento, "file", true),
        new ItemPadrao(ComprovanteRntrc, "Comprovante RNTRC", CategoriaCampoFormulario.Documento, "file", true),
        new ItemPadrao(DocumentoAdicionalCarga, "Documento adicional da carga", CategoriaCampoFormulario.Documento, "file", false)
    };

    // O CRLV pertence ao veículo; os demais documentos ao motorista.
    public static bool DocumentoDoVeiculo(string chave) =>
        chave.Equals(Crlv, StringComparison.OrdinalIgnoreCase);
}

// Comparação tolerante a caixa, espaços e acentos.
internal static class TextoCanonico
{
    public static string? Resolver(string? valor, IEnumerable<string> permitidos)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var chave = Simplificar(valor);
        return permitidos.FirstOrDefault(p => Simplificar(p) == chave);
    }

    private static string Simplificar(string texto)
    {
        var d = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(c) || c == '_' || c == '-') continue;
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }
}
