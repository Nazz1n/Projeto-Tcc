using Captacao.Api.DTOs.CamposFormulario;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.CamposFormulario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// RF05. Os campos e documentos são predefinidos (criados na
// inicialização); o administrador só configura obrigatoriedade e ativação
// dos documentos.
[ApiController]
[Route("api/[controller]")]
public class CamposFormularioController : ControllerBase
{
    private readonly CampoFormularioService _service;

    public CamposFormularioController(CampoFormularioService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> ListarTodos() => Ok(await _service.ListarTodosAsync());

    // O formulário do motorista usa a configuração pelo endpoint
    // api/Formulario (autenticado pelo token do link). Este é mantido
    // anônimo apenas para consulta da configuração ativa.
    [HttpGet("ativos")]
    [AllowAnonymous]
    public async Task<IActionResult> ListarAtivos() => Ok(await _service.ListarAtivosAsync());

    [HttpGet("{campoFormularioId:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> ObterPorId(int campoFormularioId)
    {
        try { return Ok(await _service.ObterPorIdAsync(campoFormularioId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
    }

    [HttpPut("{campoFormularioId:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(
        int campoFormularioId, [FromBody] CampoFormularioAtualizacaoDto dto)
    {
        try { return Ok(await _service.AtualizarAsync(campoFormularioId, dto, User.ObterAtendenteId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }

    [HttpPatch("{campoFormularioId:int}/ativar")]
    [Authorize(Roles = Perfis.Administrador)]
    public Task<IActionResult> Ativar(int campoFormularioId) => Alterar(campoFormularioId, true);

    [HttpPatch("{campoFormularioId:int}/desativar")]
    [Authorize(Roles = Perfis.Administrador)]
    public Task<IActionResult> Desativar(int campoFormularioId) => Alterar(campoFormularioId, false);

    private async Task<IActionResult> Alterar(int id, bool ativo)
    {
        try { return Ok(await _service.AlterarAtivoAsync(id, ativo, User.ObterAtendenteId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensagem = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensagem = ex.Message }); }
    }
}
