using Captacao.Api.DTOs.Motoristas;
using Captacao.Api.DTOs.Veiculos;
using Captacao.Api.Services.Motoristas;
using Captacao.Api.Services.Veiculos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// ANTES: sem [Authorize], qualquer pessoa na internet podia listar todos
// os motoristas (nome, telefone, e-mail e RNTRC) e cadastrar novos.
//
// AGORA: exige usuário autenticado da equipe interna (Atendente ou
// Administrador).
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MotoristasController : ControllerBase
{
    private readonly MotoristaService _motoristaService;
    private readonly VeiculoService _veiculoService;

    public MotoristasController(
        MotoristaService motoristaService,
        VeiculoService veiculoService)
    {
        _motoristaService = motoristaService;
        _veiculoService = veiculoService;
    }

    // GET: api/Motoristas?busca=nome|telefone|placa  (RF16)
    [HttpGet]
    public async Task<ActionResult<List<MotoristaRespostaDto>>> ListarTodos(
        [FromQuery] string? busca)
    {
        var motoristas = await _motoristaService.ListarTodosAsync(busca);

        return Ok(motoristas);
    }

    // GET: api/Motoristas/{id}
    [HttpGet("{motoristaId:int}")]
    public async Task<ActionResult<MotoristaRespostaDto>> BuscarPorId(
        int motoristaId)
    {
        var motorista = await _motoristaService.BuscarPorIdAsync(motoristaId);

        if (motorista == null)
            return NotFound(new { mensagem = "Motorista não encontrado." });

        return Ok(motorista);
    }

    [HttpPost]
    public async Task<ActionResult<MotoristaRespostaDto>> Criar(
        [FromBody] MotoristaCadastroDto dto)
    {
        try
        {
            var motorista = await _motoristaService.CriarAsync(dto);

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { motoristaId = motorista.MotoristaId },
                motorista
            );
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

    [HttpGet("{motoristaId:int}/Veiculos")]
    public async Task<ActionResult<List<VeiculoRespostaDto>>> ListarVeiculos(
        int motoristaId)
    {
        var veiculos = await _veiculoService.ListarPorMotoristaAsync(
            motoristaId);

        return Ok(veiculos);
    }
}
