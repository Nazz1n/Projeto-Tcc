using Captacao.Api.Seguranca;
using Captacao.Api.Services.Relatorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// RF10 e RF11: somente Administrador.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Administrador)]
public class RelatoriosController : ControllerBase
{
    private readonly RelatorioService _service;

    public RelatoriosController(RelatorioService service) => _service = service;

    [HttpGet("indicadores")]
    public async Task<IActionResult> Indicadores([FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        Ok(await _service.ObterIndicadoresAsync(de, ate));

    [HttpGet("solicitacoes/exportar")]
    public async Task<IActionResult> Exportar(
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromQuery] string? status)
    {
        try
        {
            var bytes = await _service.ExportarSolicitacoesCsvAsync(de, ate, status);

            await _service.RegistrarExportacaoAsync(
                User.ObterAtendenteId(), bytes.Count(b => b == (byte)'\n') - 1);

            return File(bytes, "text/csv; charset=utf-8",
                $"solicitacoes-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}
