using Captacao.Api.DTOs.Formulario;
using Captacao.Api.Services.Formulario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Captacao.Api.Controllers;

// Endpoints públicos do formulário do motorista (UC02). A identificação é
// feita pelo token do link no header X-Token-Acesso (não na URL, para não
// aparecer em logs de acesso).
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("formulario")]
public class FormularioController : ControllerBase
{
    private const string HeaderToken = "X-Token-Acesso";

    private readonly FormularioService _service;

    public FormularioController(FormularioService service) => _service = service;

    [HttpGet]
    public Task<IActionResult> Obter() => Executar(t => _service.ObterAsync(t));

    [HttpPut("dados")]
    public Task<IActionResult> SalvarDados([FromBody] FormularioDadosDto dto) =>
        Executar(t => _service.SalvarDadosAsync(t, dto));

    [HttpPost("documentos")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public Task<IActionResult> EnviarDocumento([FromForm] FormularioDocumentoEnvioDto dto) =>
        Executar(t => _service.EnviarDocumentoAsync(t, dto));

    [HttpPost("enviar")]
    public Task<IActionResult> Enviar() => Executar(t => _service.EnviarAsync(t));

    private async Task<IActionResult> Executar<T>(Func<string, Task<T>> acao)
    {
        var token = Request.Headers[HeaderToken].ToString();

        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized(new { mensagem = "Link inválido ou expirado." });

        try
        {
            return Ok(await acao(token));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { mensagem = "Link inválido ou expirado." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }
}
