using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Infra;

// Rede de segurança para exceções que os controllers NÃO tratam.
// Os controllers continuam tratando os erros de negócio (ArgumentException,
// KeyNotFoundException etc.) como já fazem hoje; este handler só entra
// quando algo escapa, evitando que o cliente receba a página de erro padrão
// ou detalhes internos (stack trace, nomes de tabelas, SQL).
//
// O formato da resposta segue o padrão que a API já usa: { "mensagem": "..." }.
public class ExcecaoGlobalHandler : IExceptionHandler
{
    private readonly ILogger<ExcecaoGlobalHandler> _logger;

    public ExcecaoGlobalHandler(ILogger<ExcecaoGlobalHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Se a resposta já começou a ser enviada, não é possível trocar
        // o status code. Devolve false para o pipeline padrão agir.
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        // Caso 1: violação de índice único.
        // Acontece quando duas requisições simultâneas passam pela checagem
        // "já existe?" e tentam inserir o mesmo telefone, placa, CPF
        // ou hash de token. O índice único do banco
        // barra a segunda, e aqui ela vira 409 em vez de 500.
        if (exception is DbUpdateException && EhViolacaoDeUnicidade(exception))
        {
            // Atenção: a mensagem original do MySQL contém o valor duplicado
            // (ex.: o telefone). Por isso NÃO logamos a exceção inteira aqui,
            // para não gravar dado pessoal nos logs.
            _logger.LogWarning(
                "Violação de unicidade em {Metodo} {Caminho}.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    mensagem =
                        "Já existe um registro com esses dados."
                },
                cancellationToken);

            return true;
        }

        // Caso 2: qualquer outra exceção é um erro inesperado.
        // O detalhe completo vai para o log; o cliente recebe só um aviso
        // genérico.
        _logger.LogError(
            exception,
            "Erro não tratado em {Metodo} {Caminho}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                mensagem =
                    "Ocorreu um erro inesperado. Tente novamente mais tarde."
            },
            cancellationToken);

        return true;
    }

    // Percorre as exceções internas procurando a mensagem de chave duplicada
    // do MySQL ("Duplicate entry '...' for key '...'", código 1062).
    // A busca por texto evita criar dependência direta com a classe de
    // exceção do provedor MySQL.
    private static bool EhViolacaoDeUnicidade(Exception exception)
    {
        for (var atual = exception; atual != null; atual = atual.InnerException)
        {
            if (atual.Message.Contains(
                    "Duplicate entry",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
