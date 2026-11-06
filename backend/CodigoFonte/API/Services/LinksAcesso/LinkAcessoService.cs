using System.Security.Cryptography;
using System.Text;
using Captacao.Api.DTOs.LinksAcesso;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.LinksAcesso;

public class LinkAcessoService
{
    private readonly CaptacaoDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuditoriaService _auditoriaService;

    public LinkAcessoService(
        CaptacaoDbContext context,
        IConfiguration configuration,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _configuration = configuration;
        _auditoriaService = auditoriaService;
    }

    // Gera um novo link de acesso para uma solicitação
    public async Task<LinkAcessoRespostaDto> GerarAsync(
        int solicitacaoId)
    {
        // Verifica se a solicitação existe
        var solicitacaoExiste = await _context.Solicitacoes
            .AnyAsync(s => s.SolicitacaoId == solicitacaoId);

        if (!solicitacaoExiste)
        {
            throw new KeyNotFoundException(
                "Solicitação não encontrada.");
        }

        // Recupera a configuração da URL do formulário
        var urlBaseFormulario =
            _configuration["LinksAcesso:UrlBaseFormulario"];

        if (string.IsNullOrWhiteSpace(urlBaseFormulario) ||
            !Uri.TryCreate(
                urlBaseFormulario,
                UriKind.Absolute,
                out var uriBase) ||
            (uriBase.Scheme != Uri.UriSchemeHttps &&
             uriBase.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "A URL base do formulário não foi configurada corretamente.");
        }

        // Recupera o prazo de validade do link
        var validadeHoras =
            _configuration.GetValue<int?>(
                "LinksAcesso:ValidadeHoras") ?? 24;

        if (validadeHoras < 1 || validadeHoras > 168)
        {
            throw new InvalidOperationException(
                "A validade deve estar entre 1 e 168 horas.");
        }

        // Gera um token aleatório de 256 bits
        var token = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));

        // Calcula o hash do token para armazenamento
        var tokenHash = GerarHashToken(token);

        var agora = DateTime.UtcNow;

        var dataExpiracao = agora.AddHours(validadeHoras);

        // Inicia uma transação
        await using var transacao =
            await _context.Database.BeginTransactionAsync();

        // Invalida links anteriores ainda utilizáveis
        var linksAnteriores = await _context.LinksAcesso
            .Where(l =>
                l.SolicitacaoId == solicitacaoId &&
                l.DataInvalidacao == null &&
                l.DataUtilizacao == null &&
                l.DataExpiracao > agora)
            .ToListAsync();

        foreach (var link in linksAnteriores)
        {
            link.DataInvalidacao = agora;
        }

        // Cria o novo link
        var novoLink = new LinkAcesso
        {
            SolicitacaoId = solicitacaoId,
            TokenHash = tokenHash,
            DataCriacao = agora,
            DataExpiracao = dataExpiracao
        };

        _context.LinksAcesso.Add(novoLink);

        await _context.SaveChangesAsync();

        await transacao.CommitAsync();

        // Monta a URL que será enviada ao motorista
        var urlAcesso =
            $"{urlBaseFormulario.TrimEnd('/')}/{token}";

        // Registra a geração do link na auditoria.
        // O token não é armazenado na auditoria.
        await _auditoriaService.RegistrarAsync(
            "Link de acesso gerado",
            $"Link ID: {novoLink.LinkAcessoId}. " +
            $"Solicitação ID: {novoLink.SolicitacaoId}. " +
            $"Validade até: {novoLink.DataExpiracao:u}."
        );

        // Dados para o atendente enviar o link pelo WhatsApp.
        var telefone = await _context.Solicitacoes
            .AsNoTracking()
            .Where(s => s.SolicitacaoId == solicitacaoId)
            .Select(s => s.Motorista.Telefone)
            .FirstOrDefaultAsync();

        var mensagem = MontarMensagem(urlAcesso, validadeHoras);

        // Retorna o token somente dentro da URL, nunca o hash
        return new LinkAcessoRespostaDto
        {
            LinkAcessoId = novoLink.LinkAcessoId,
            SolicitacaoId = novoLink.SolicitacaoId,
            UrlAcesso = urlAcesso,
            DataCriacao = novoLink.DataCriacao,
            DataExpiracao = novoLink.DataExpiracao,
            MensagemSugerida = mensagem,
            UrlWhatsApp = MontarUrlWhatsApp(telefone, mensagem)
        };
    }

    internal static string MontarMensagem(string url, int validadeHoras) =>
        "Olá! Para continuar o seu cadastro, preencha o formulário e envie " +
        "os documentos pelo link abaixo:\n" + url + "\n" +
        $"O link é pessoal e vale por {validadeHoras} horas.";

    // wa.me exige o telefone só com dígitos e com o código do país. Números
    // brasileiros salvos sem o 55 (10 ou 11 dígitos) recebem o prefixo.
    internal static string? MontarUrlWhatsApp(string? telefone, string mensagem)
    {
        var digitos = FormatacaoDados.AdicionarDdiBrasilSeNecessario(telefone);

        if (!FormatacaoDados.TelefoneValido(digitos)) return null;

        return $"https://wa.me/{digitos}?text={Uri.EscapeDataString(mensagem)}";
    }

    // Valida se um token pode ser utilizado
    public async Task<LinkAcessoValidacaoRespostaDto> ValidarAsync(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 200)
        {
            return LinkInvalido();
        }

        // Calcula o hash do token recebido
        var tokenHash = GerarHashToken(token.Trim());

        // Pesquisa o link pelo hash
        var link = await _context.LinksAcesso
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.TokenHash == tokenHash);

        var agora = DateTime.UtcNow;

        // Verifica as condições de validade
        if (link == null ||
            link.DataInvalidacao.HasValue ||
            link.DataUtilizacao.HasValue ||
            link.DataExpiracao <= agora)
        {
            return LinkInvalido();
        }

        return new LinkAcessoValidacaoRespostaDto
        {
            Valido = true,
            SolicitacaoId = link.SolicitacaoId,
            DataExpiracao = link.DataExpiracao,
            Mensagem = "Link válido."
        };
    }

    // Resolve o token para a solicitação, sem consumir o link. Usado pelo
    // formulário do motorista: motorista e solicitação são sempre
    // descobertos pelo token no banco, nunca por ids enviados pelo cliente.
    public async Task<int> ObterSolicitacaoIdValidaAsync(string token)
    {
        var resultado = await ValidarAsync(token);

        if (!resultado.Valido)
            throw new UnauthorizedAccessException("Link inválido ou expirado.");

        return resultado.SolicitacaoId;
    }

    // Lista os links de uma solicitação
    public async Task<List<LinkAcessoConsultaDto>>
        ListarPorSolicitacaoAsync(int solicitacaoId)
    {
        var solicitacaoExiste = await _context.Solicitacoes
            .AnyAsync(s => s.SolicitacaoId == solicitacaoId);

        if (!solicitacaoExiste)
        {
            throw new KeyNotFoundException(
                "Solicitação não encontrada.");
        }

        var agora = DateTime.UtcNow;

        var links = await _context.LinksAcesso
            .AsNoTracking()
            .Where(l => l.SolicitacaoId == solicitacaoId)
            .OrderByDescending(l => l.DataCriacao)
            .ToListAsync();

        return links.Select(l => new LinkAcessoConsultaDto
        {
            LinkAcessoId = l.LinkAcessoId,
            SolicitacaoId = l.SolicitacaoId,
            DataCriacao = l.DataCriacao,
            DataExpiracao = l.DataExpiracao,
            DataUtilizacao = l.DataUtilizacao,
            DataInvalidacao = l.DataInvalidacao,

            Utilizavel =
                !l.DataInvalidacao.HasValue &&
                !l.DataUtilizacao.HasValue &&
                l.DataExpiracao > agora

        }).ToList();
    }

    // Invalida manualmente um link
    public async Task InvalidarAsync(int linkAcessoId)
    {
        var link = await _context.LinksAcesso
            .FirstOrDefaultAsync(
                l => l.LinkAcessoId == linkAcessoId);

        if (link == null)
        {
            throw new KeyNotFoundException(
                "Link de acesso não encontrado.");
        }

        if (!link.DataInvalidacao.HasValue &&
            !link.DataUtilizacao.HasValue &&
            link.DataExpiracao > DateTime.UtcNow)
        {
            link.DataInvalidacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Registra a invalidação na auditoria
            await _auditoriaService.RegistrarAsync(
                "Link de acesso invalidado",
                $"Link ID: {link.LinkAcessoId}. " +
                $"Solicitação ID: {link.SolicitacaoId}."
            );
        }
    }

    // Consome o link após o envio bem-sucedido do formulário
    public async Task<int> ConsumirAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 200)
        {
            throw new UnauthorizedAccessException(
                "Link inválido ou expirado.");
        }

        var tokenHash = GerarHashToken(token.Trim());

        var link = await _context.LinksAcesso
            .FirstOrDefaultAsync(
                l => l.TokenHash == tokenHash);

        var agora = DateTime.UtcNow;

        if (link == null ||
            link.DataInvalidacao.HasValue ||
            link.DataUtilizacao.HasValue ||
            link.DataExpiracao <= agora)
        {
            throw new UnauthorizedAccessException(
                "Link inválido ou expirado.");
        }

        link.DataUtilizacao = agora;

        try
        {
            // DataUtilizacao é token de concorrência: se outro envio
            // consumiu o link ao mesmo tempo, este UPDATE não afeta nenhuma
            // linha e o link não é consumido duas vezes.
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedAccessException("Link inválido ou expirado.");
        }

        // Registra o consumo do link na auditoria.
        // O token não é registrado.
        await _auditoriaService.RegistrarAsync(
            "Link de acesso utilizado",
            $"Link ID: {link.LinkAcessoId}. " +
            $"Solicitação ID: {link.SolicitacaoId}."
        );

        return link.SolicitacaoId;
    }

    // Calcula o SHA-256 do token
    private static string GerarHashToken(string token)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }

    // Retorna uma resposta padrão para links inválidos
    private static LinkAcessoValidacaoRespostaDto LinkInvalido()
    {
        return new LinkAcessoValidacaoRespostaDto
        {
            Valido = false,
            SolicitacaoId = 0,
            DataExpiracao = DateTime.MinValue,
            Mensagem =
                "Link inválido ou expirado. Solicite um novo link."
        };
    }
}