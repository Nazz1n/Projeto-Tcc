using Captacao.Api.DTOs.Solicitacoes;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.Solicitacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// A prioridade NÃO pode ser alterada por endpoint: ela é calculada pelo
// sistema (RF07) e recalculada a cada evento relevante. O status só aceita
// as transições manuais do RF14 (Em análise, Contratado, Encerrado).
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SolicitacoesController : ControllerBase
{
    private readonly SolicitacaoService _service;

    public SolicitacoesController(SolicitacaoService service) => _service = service;

    // POST: api/Solicitacoes
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SolicitacaoCadastroDto dto)
    {
        try
        {
            var s = await _service.CriarAsync(dto, User.ObterAtendenteId());
            return CreatedAtAction(nameof(BuscarPorId), new { id = s.SolicitacaoId }, s);
        }
        catch (ArgumentException ex) { return BadRequest(new { mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { mensagem = ex.Message }); }
    }

    // GET: api/Solicitacoes?status=ProntoParaAnalise&prioridade=Alta
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? status, [FromQuery] string? prioridade)
    {
        try { return Ok(await _service.ListarTodosAsync(status, prioridade)); }
        catch (ArgumentException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }

    // GET: api/Solicitacoes/kanban
    [HttpGet("kanban")]
    public async Task<IActionResult> Kanban() => Ok(await _service.ObterKanbanAsync());

    // GET: api/Solicitacoes/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var s = await _service.BuscarPorIdAsync(id);
        return s == null ? NotFound(new { mensagem = "Solicitação não encontrada." }) : Ok(s);
    }

    // GET: api/Solicitacoes/motorista/{motoristaId}
    [HttpGet("motorista/{motoristaId:int}")]
    public async Task<IActionResult> ListarPorMotorista(int motoristaId) =>
        Ok(await _service.ListarPorMotoristaAsync(motoristaId));

    // GET: api/Solicitacoes/{id}/historico
    [HttpGet("{id:int}/historico")]
    public async Task<IActionResult> ListarHistorico(int id)
    {
        try { return Ok(await _service.ListarHistoricoAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
    }

    // PATCH: api/Solicitacoes/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(int id, [FromBody] SolicitacaoStatusAtualizacaoDto dto)
    {
        var atendenteId = User.ObterAtendenteId();
        if (atendenteId == null)
            return Unauthorized(new { mensagem = "Não foi possível identificar o atendente." });

        try { return Ok(await _service.AtualizarStatusAsync(id, dto, atendenteId.Value)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }

    // PATCH: api/Solicitacoes/{id}/atendente
    [HttpPatch("{id:int}/atendente")]
    public async Task<IActionResult> AtualizarAtendente(int id, [FromBody] SolicitacaoAtendenteAtualizacaoDto dto)
    {
        try { return Ok(await _service.AtualizarAtendenteAsync(id, dto, User.ObterAtendenteId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }

    // PATCH: api/Solicitacoes/{id}/carga
    [HttpPatch("{id:int}/carga")]
    public async Task<IActionResult> AtualizarCarga(int id, [FromBody] SolicitacaoCargaAtualizacaoDto dto)
    {
        try { return Ok(await _service.AtualizarCargaAsync(id, dto, User.ObterAtendenteId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }
}
