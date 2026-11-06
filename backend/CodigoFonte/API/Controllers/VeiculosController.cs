using Captacao.Api.DTOs.Veiculos;
using Captacao.Api.Services.Veiculos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// ANTES: sem [Authorize], qualquer pessoa na internet podia listar,
// cadastrar, alterar e inativar veículos.
//
// AGORA: exige usuário autenticado da equipe interna (Atendente ou
// Administrador).
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VeiculosController : ControllerBase
{
    private readonly VeiculoService _veiculoService;

    public VeiculosController(VeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    // CADASTRAR VEÍCULO
    [HttpPost]
    public async Task<ActionResult<VeiculoRespostaDto>> Criar(
        VeiculoCadastroDto dto)
    {
        try
        {
            var veiculo = await _veiculoService.CriarAsync(dto);

            return CreatedAtAction(
                nameof(ListarTodos),
                new { id = veiculo.VeiculoId },
                veiculo);
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

    // LISTAR TODOS OS VEÍCULOS
    [HttpGet]
    public async Task<ActionResult<List<VeiculoRespostaDto>>> ListarTodos()
    {
        var veiculos = await _veiculoService.ListarTodosAsync();

        return Ok(veiculos);
    }

    // LISTAR VEÍCULOS ATIVOS
    [HttpGet("ativos")]
    public async Task<ActionResult<List<VeiculoRespostaDto>>> ListarAtivos()
    {
        var veiculos = await _veiculoService.ListarAtivosAsync();

        return Ok(veiculos);
    }

    // BUSCAR VEÍCULO PELA PLACA
    [HttpGet("placa/{placa}")]
    public async Task<ActionResult<VeiculoRespostaDto>> BuscarPorPlaca(
        string placa)
    {
        var veiculo = await _veiculoService.BuscarPorPlacaAsync(placa);

        if (veiculo == null)
            return NotFound(new
            {
                mensagem = "Veículo não encontrado."
            });

        return Ok(veiculo);
    }

    // FILTRAR VEÍCULOS PELO TIPO
    [HttpGet("tipo/{tipoVeiculo}")]
    public async Task<ActionResult<List<VeiculoRespostaDto>>> ListarPorTipo(
        string tipoVeiculo)
    {
        var veiculos = await _veiculoService.ListarPorTipoAsync(tipoVeiculo);

        return Ok(veiculos);
    }

    // ATUALIZAR VEÍCULO
    [HttpPut("{id:int}")]
    public async Task<ActionResult<VeiculoRespostaDto>> Atualizar(
        int id,
        VeiculoAtualizacaoDto dto)
    {
        try
        {
            var veiculo = await _veiculoService.AtualizarAsync(id, dto);

            return Ok(veiculo);
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

    // INATIVAR VEÍCULO
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Inativar(int id)
    {
        try
        {
            await _veiculoService.InativarAsync(id);

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

    // REATIVAR VEÍCULO
    [HttpPatch("{id:int}/reativar")]
    public async Task<IActionResult> Reativar(int id)
    {
        try
        {
            await _veiculoService.ReativarAsync(id);

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
