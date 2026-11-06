
using Captacao.Api.Services.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class AuditoriaController : ControllerBase
{
    private readonly AuditoriaService _auditoriaService;

    public AuditoriaController(AuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    // GET: api/Auditoria
    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var registros = await _auditoriaService.ListarTodosAsync();

        return Ok(registros);
    }

    // GET: api/Auditoria/5
    [HttpGet("{registroAuditoriaId:int}")]
    public async Task<IActionResult> ObterPorId(int registroAuditoriaId)
    {
        try
        {
            var registro = await _auditoriaService
                .ObterPorIdAsync(registroAuditoriaId);

            return Ok(registro);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
