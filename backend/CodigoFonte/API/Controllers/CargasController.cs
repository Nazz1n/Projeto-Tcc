using System.Security.Claims;
using Captacao.Api.DTOs.Cargas;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.Cargas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// Administradores gerenciam as cargas; atendentes podem apenas consultá-las.
// A contratação de uma solicitação atualiza automaticamente a carga vinculada.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CargasController : ControllerBase
{
    private readonly CargaService _cargaService;

    public CargasController(CargaService cargaService)
    {
        _cargaService = cargaService;
    }

    // POST: api/Cargas
    // Cadastra uma carga vinculada ao administrador autenticado
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Criar(
        [FromBody] CargaCadastroDto dto)
    {
        try
        {
            var atendenteIdTexto =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(atendenteIdTexto, out var atendenteId))
            {
                return Unauthorized(new
                {
                    mensagem = "Não foi possível identificar o atendente."
                });
            }

            var carga = await _cargaService.CriarAsync(dto, atendenteId);

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = carga.CargaId },
                carga);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    // GET: api/Cargas
    // Lista todas as cargas
    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var cargas = await _cargaService.ListarTodosAsync();

        return Ok(cargas);
    }

    // GET: api/Cargas/disponiveis
    // Lista somente as cargas disponíveis
    [HttpGet("disponiveis")]
    public async Task<IActionResult> ListarDisponiveis()
    {
        var cargas = await _cargaService.ListarDisponiveisAsync();

        return Ok(cargas);
    }

    // GET: api/Cargas/minhas
    // Lista as cargas cadastradas pelo atendente autenticado
    [HttpGet("minhas")]
    public async Task<IActionResult> ListarMinhas()
    {
        var atendenteIdTexto =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(atendenteIdTexto, out var atendenteId))
        {
            return Unauthorized(new
            {
                mensagem = "Não foi possível identificar o atendente."
            });
        }

        try
        {
            var cargas = await _cargaService
                .ListarPorAtendenteAsync(atendenteId);

            return Ok(cargas);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    // GET: api/Cargas/{id}
    // Busca uma carga pelo ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var carga = await _cargaService.BuscarPorIdAsync(id);

        if (carga == null)
        {
            return NotFound(new
            {
                mensagem = "Carga não encontrada."
            });
        }

        return Ok(carga);
    }

    // PUT: api/Cargas/{id}
    // Atualiza os dados de uma carga
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] CargaAtualizacaoDto dto)
    {
        try
        {
            var carga = await _cargaService.AtualizarAsync(id, dto, User.ObterAtendenteId());

            return Ok(carga);
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

    // PATCH: api/Cargas/{id}/status
    // Atualiza o status de uma carga
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> AtualizarStatus(
        int id,
        [FromBody] CargaStatusAtualizacaoDto dto)
    {
        try
        {
            var carga = await _cargaService
                .AtualizarStatusAsync(id, dto.NovoStatus, User.ObterAtendenteId());

            return Ok(carga);
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

    // DELETE: api/Cargas/{id}
    // Um administrador pode excluir apenas cargas próprias e ainda sem atendimentos.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Excluir(int id)
    {
        var atendenteId = User.ObterAtendenteId();
        if (atendenteId is null)
            return Unauthorized(new { mensagem = "Não foi possível identificar o administrador." });

        try
        {
            await _cargaService.ExcluirAsync(id, atendenteId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }
}
