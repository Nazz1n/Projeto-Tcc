
namespace Captacao.Api.Services.Documentos;

public class ArmazenamentoDocumentoService
{
    private readonly string _pastaDocumentos;

    private static readonly Dictionary<string, string> ExtensoesPermitidas =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", "application/pdf" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" }
        };

    // A pasta pode ser configurada em "Armazenamento:PastaDocumentos"
    // (user-secrets ou variável de ambiente). Sem configuração, mantém o
    // caminho relativo anterior, para não perder os arquivos já enviados.
    public ArmazenamentoDocumentoService(
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        var configurada = configuration["Armazenamento:PastaDocumentos"];

        _pastaDocumentos = !string.IsNullOrWhiteSpace(configurada)
            ? Path.GetFullPath(configurada)
            : Path.GetFullPath(
                Path.Combine(
                    environment.ContentRootPath,
                    "..", "..", "..", "..",
                    "dados-seguranca",
                    "documentos"));

        Directory.CreateDirectory(_pastaDocumentos);
    }

    public async Task<(string chave, string tipoConteudo)> SalvarAsync(
        IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
            throw new ArgumentException("O arquivo está vazio.");

        if (arquivo.Length > 10 * 1024 * 1024)
            throw new ArgumentException(
                "O arquivo não pode ultrapassar 10 MB.");

        var extensao = Path.GetExtension(arquivo.FileName);

        if (!ExtensoesPermitidas.TryGetValue(
            extensao, out var tipoEsperado))
        {
            throw new ArgumentException(
                "Formato não permitido. Envie PDF, JPG ou PNG.");
        }

        var tipoDetectado = await DetectarTipoAsync(arquivo);

        if (tipoDetectado != tipoEsperado)
            throw new ArgumentException(
                "O conteúdo do arquivo não corresponde à extensão informada.");

        var chave = $"{Guid.NewGuid():N}{extensao.ToLowerInvariant()}";

        var caminho = Path.Combine(_pastaDocumentos, chave);

        await using var stream = new FileStream(
            caminho,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await arquivo.CopyToAsync(stream);

        return (chave, tipoDetectado);
    }

    public async Task<byte[]> LerAsync(string chave)
    {
        ValidarChave(chave);

        var caminho = Path.Combine(_pastaDocumentos, chave);

        if (!File.Exists(caminho))
            throw new FileNotFoundException(
                "O arquivo do documento não foi encontrado.");

        return await File.ReadAllBytesAsync(caminho);
    }

    public void Excluir(string chave)
    {
        ValidarChave(chave);

        var caminho = Path.Combine(_pastaDocumentos, chave);

        if (File.Exists(caminho))
            File.Delete(caminho);
    }

    private static void ValidarChave(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave) ||
            Path.GetFileName(chave) != chave)
        {
            throw new InvalidOperationException(
                "Chave de armazenamento inválida.");
        }
    }

    private static async Task<string> DetectarTipoAsync(
        IFormFile arquivo)
    {
        var cabecalho = new byte[8];

        await using var stream = arquivo.OpenReadStream();

        var quantidade = await stream.ReadAsync(cabecalho);

        // PDF: %PDF
        if (quantidade >= 4 &&
            cabecalho[0] == 0x25 &&
            cabecalho[1] == 0x50 &&
            cabecalho[2] == 0x44 &&
            cabecalho[3] == 0x46)
            return "application/pdf";

        // JPG
        if (quantidade >= 3 &&
            cabecalho[0] == 0xFF &&
            cabecalho[1] == 0xD8 &&
            cabecalho[2] == 0xFF)
            return "image/jpeg";

        // PNG
        if (quantidade >= 8 &&
            cabecalho[0] == 0x89 &&
            cabecalho[1] == 0x50 &&
            cabecalho[2] == 0x4E &&
            cabecalho[3] == 0x47 &&
            cabecalho[4] == 0x0D &&
            cabecalho[5] == 0x0A &&
            cabecalho[6] == 0x1A &&
            cabecalho[7] == 0x0A)
            return "image/png";

        return "desconhecido";
    }
}
