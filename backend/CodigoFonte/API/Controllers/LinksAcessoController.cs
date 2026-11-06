
using Captacao.Api.Services.LinksAcesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LinksAcessoController : ControllerBase
{
    private readonly LinkAcessoService _service;

    public LinksAcessoController(LinkAcessoService service)
    {
        _service = service;
    }

    // Gera um novo link para uma solicitação
    [HttpPost("solicitacao/{solicitacaoId:int}")]
    [Authorize]
    public async Task<IActionResult> Gerar(int solicitacaoId)
    {
        try
        {
            var resultado = await _service.GerarAsync(solicitacaoId);

            return CreatedAtAction(
                nameof(ListarPorSolicitacao),
                new { solicitacaoId },
                resultado
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    // Valida um token de acesso
    [HttpPost("validar")]
    [AllowAnonymous]
    public async Task<IActionResult> Validar(
        [FromBody] ValidarLinkRequest request)
    {
        var resultado = await _service.ValidarAsync(request.Token);

        if (!resultado.Valido)
        {
            return BadRequest(resultado);
        }

        return Ok(resultado);
    }

    // Consulta os links de uma solicitação
    [HttpGet("solicitacao/{solicitacaoId:int}")]
    [Authorize]
    public async Task<IActionResult> ListarPorSolicitacao(
        int solicitacaoId)
    {
        try
        {
            var resultado =
                await _service.ListarPorSolicitacaoAsync(solicitacaoId);

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    // Invalida manualmente um link
    [HttpPatch("{linkAcessoId:int}/invalidar")]
    [Authorize]
    public async Task<IActionResult> Invalidar(int linkAcessoId)
    {
        try
        {
            await _service.InvalidarAsync(linkAcessoId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}

// DTO simples para receber o token na validação
public class ValidarLinkRequest
{
    public string Token { get; set; } = string.Empty;
}
