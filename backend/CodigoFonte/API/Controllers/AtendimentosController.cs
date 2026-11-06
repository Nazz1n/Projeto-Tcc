using Captacao.Api.DTOs.Atendimentos;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.Atendimentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AtendimentosController : ControllerBase
{
    private readonly AtendimentoService _service;

    public AtendimentosController(AtendimentoService service) => _service = service;

    // POST: api/Atendimentos/iniciar
    // Cria/reaproveita motorista e solicitação e gera o link do formulário
    // (com a mensagem sugerida e o endereço wa.me) em uma única chamada.
    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar([FromBody] IniciarAtendimentoDto dto)
    {
        try
        {
            return Ok(await _service.IniciarAsync(dto, User.ObterAtendenteId()));
        }
        catch (ArgumentException ex) { return BadRequest(new { mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { mensagem = ex.Message }); }
    }
}
