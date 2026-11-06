using Captacao.Api.DTOs.Atendentes;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.Atendentes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// Regra de acesso (RF12):
//  - Administrador: cadastra, consulta, atualiza, inativa e reativa usuários.
//  - Atendente: só pode consultar a lista de atendentes ATIVOS, necessária
//    para atribuir uma solicitação a um responsável.
//
// O [Authorize] da classe exige apenas usuário autenticado. Cada método
// que é exclusivo do Administrador acrescenta a restrição de Role.
// (Não colocamos Roles na classe porque, quando classe e método têm
// [Authorize], os DOIS precisam ser satisfeitos: não seria possível
// liberar "ativos" para o Atendente depois.)
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AtendentesController : ControllerBase
{
    private readonly AtendenteService _atendenteService;

    public AtendentesController(AtendenteService atendenteService)
    {
        _atendenteService = atendenteService;
    }

    // POST: api/Atendentes
    // Cadastra um novo atendente
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Criar(
        [FromBody] AtendenteCadastroDto dto)
    {
        try
        {
            var atendente = await _atendenteService.CriarAsync(dto);

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = atendente.AtendenteId },
                atendente);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }

    // GET: api/Atendentes
    // Lista todos os atendentes (ativos e inativos)
    [HttpGet]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> ListarTodos()
    {
        var atendentes = await _atendenteService.ListarTodosAsync();

        return Ok(atendentes);
    }

    // GET: api/Atendentes/ativos
    // Lista somente os atendentes ativos.
    // Qualquer usuário autenticado pode consultar (uso: escolher o
    // responsável de uma solicitação).
    [HttpGet("ativos")]
    public async Task<IActionResult> ListarAtivos()
    {
        var atendentes = await _atendenteService.ListarAtivosAsync();

        return Ok(atendentes);
    }

    // GET: api/Atendentes/{id}
    // Busca um atendente pelo ID
    [HttpGet("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var atendente = await _atendenteService.BuscarPorIdAsync(id);

        if (atendente == null)
        {
            return NotFound(new
            {
                mensagem = "Atendente não encontrado."
            });
        }

        return Ok(atendente);
    }

    // PUT: api/Atendentes/{id}
    // Atualiza nome e e-mail
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] AtendenteAtualizacaoDto dto)
    {
        try
        {
            var atendente = await _atendenteService.AtualizarAsync(id, dto);

            return Ok(atendente);
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

    // PATCH: api/Atendentes/{id}/inativar
    [HttpPatch("{id:int}/inativar")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Inativar(int id)
    {
        try
        {
            await _atendenteService.InativarAsync(id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    // PATCH: api/Atendentes/{id}/reativar
    [HttpPatch("{id:int}/reativar")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Reativar(int id)
    {
        try
        {
            await _atendenteService.ReativarAsync(id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
