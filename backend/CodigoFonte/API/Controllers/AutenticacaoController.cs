
using Captacao.Api.DTOs.Autenticacao;
using Captacao.Api.Services.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Captacao.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticacaoController : ControllerBase
{
    private readonly AutenticacaoService _autenticacaoService;

    public AutenticacaoController(
        AutenticacaoService autenticacaoService)
    {
        _autenticacaoService = autenticacaoService;
    }

    // POST: api/Autenticacao/login
    // Realiza o login do atendente
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        try
        {
            var resultado = await _autenticacaoService.LoginAsync(dto);

            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                mensagem = ex.Message
            });
        }
    }
}
