using Captacao.Api.DTOs.Veiculos;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;
using Captacao.Api.Services.Solicitacoes;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Veiculos;

public class VeiculoService
{
    private readonly CaptacaoDbContext _context;
    private readonly AuditoriaService _auditoriaService;
    private readonly SolicitacaoRegrasService _regras;

    public VeiculoService(
        CaptacaoDbContext context,
        AuditoriaService auditoriaService,
        SolicitacaoRegrasService regras)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _regras = regras;
    }

    // Normaliza (sem hífen, maiúsculas) e valida o formato da placa.
    private static string NormalizarEValidarPlaca(string placaInformada)
    {
        var placa = FormatacaoDados.NormalizarPlaca(placaInformada);

        if (!FormatacaoDados.PlacaValida(placa))
            throw new ArgumentException(
                "Placa inválida. Use o formato ABC1234 ou ABC1D23.");

        return placa;
    }

    // CADASTRAR VEÍCULO
    public async Task<VeiculoRespostaDto> CriarAsync(
        VeiculoCadastroDto dto)
    {
        var motoristaExiste = await _context.Motoristas
            .AnyAsync(m => m.MotoristaId == dto.MotoristaId);

        if (!motoristaExiste)
        {
            throw new ArgumentException(
                "O motorista informado não foi encontrado.");
        }

        var placa = NormalizarEValidarPlaca(dto.Placa);

        var placaExiste = await _context.Veiculos
            .AnyAsync(v => v.Placa == placa);

        if (placaExiste)
        {
            throw new InvalidOperationException(
                "Já existe um veículo cadastrado com essa placa.");
        }

        var veiculo = new Veiculo
        {
            MotoristaId = dto.MotoristaId,
            Placa = placa,
            TipoVeiculo = string.IsNullOrWhiteSpace(dto.TipoVeiculo) ? null : dto.TipoVeiculo.Trim(),
            CapacidadeToneladas = dto.CapacidadeToneladas,
            Ativo = true
        };

        _context.Veiculos.Add(veiculo);

        await _context.SaveChangesAsync();

        // Auditoria da criação
        await _auditoriaService.RegistrarAsync(
            "Veículo criado",
            $"Veículo ID: {veiculo.VeiculoId}. " +
            $"Motorista ID: {veiculo.MotoristaId}. " +
            $"Placa: {veiculo.Placa}. " +
            $"Tipo: {veiculo.TipoVeiculo}."
        );

        return MapearResposta(veiculo);
    }

    // ATUALIZAR VEÍCULO
    public async Task<VeiculoRespostaDto> AtualizarAsync(
        int veiculoId,
        VeiculoAtualizacaoDto dto)
    {
        var veiculo = await _context.Veiculos
            .FirstOrDefaultAsync(v => v.VeiculoId == veiculoId);

        if (veiculo == null)
        {
            throw new KeyNotFoundException(
                "O veículo informado não foi encontrado.");
        }

        var placa = NormalizarEValidarPlaca(dto.Placa);

        var placaExiste = await _context.Veiculos
            .AnyAsync(v =>
                v.Placa == placa &&
                v.VeiculoId != veiculoId);

        if (placaExiste)
        {
            throw new InvalidOperationException(
                "Já existe outro veículo cadastrado com essa placa.");
        }

        var placaAnterior = veiculo.Placa;
        var tipoAnterior = veiculo.TipoVeiculo;
        var capacidadeAnterior = veiculo.CapacidadeToneladas;
        var ativoAnterior = veiculo.Ativo;

        veiculo.Placa = placa;
        veiculo.TipoVeiculo = string.IsNullOrWhiteSpace(dto.TipoVeiculo) ? null : dto.TipoVeiculo.Trim();
        veiculo.CapacidadeToneladas = dto.CapacidadeToneladas;
        veiculo.Ativo = dto.Ativo;

        await _context.SaveChangesAsync();

        // Auditoria da atualização
        await _auditoriaService.RegistrarAsync(
            "Veículo atualizado",
            $"Veículo ID: {veiculoId}. " +
            $"Placa anterior: {placaAnterior}; atual: {veiculo.Placa}. " +
            $"Tipo anterior: {tipoAnterior}; atual: {veiculo.TipoVeiculo}. " +
            $"Capacidade anterior: {capacidadeAnterior}; atual: {veiculo.CapacidadeToneladas}. " +
            $"Ativo anterior: {ativoAnterior}; atual: {veiculo.Ativo}."
        );

        // Capacidade e placa influenciam a compatibilidade com a carga
        // (RF07): recalcula as solicitações abertas do motorista.
        await _regras.RecalcularAbertasDoMotoristaAsync(veiculo.MotoristaId);

        return MapearResposta(veiculo);
    }

    // INATIVAR VEÍCULO
    public async Task<bool> InativarAsync(int veiculoId)
    {
        var veiculo = await _context.Veiculos
            .FirstOrDefaultAsync(v => v.VeiculoId == veiculoId);

        if (veiculo == null)
        {
            throw new KeyNotFoundException(
                "O veículo informado não foi encontrado.");
        }

        if (!veiculo.Ativo)
        {
            throw new InvalidOperationException(
                "O veículo já está inativo.");
        }

        veiculo.Ativo = false;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Veículo inativado",
            $"Veículo ID: {veiculoId}. " +
            $"Placa: {veiculo.Placa}."
        );

        return true;
    }

    // REATIVAR VEÍCULO
    public async Task<bool> ReativarAsync(int veiculoId)
    {
        var veiculo = await _context.Veiculos
            .FirstOrDefaultAsync(v => v.VeiculoId == veiculoId);

        if (veiculo == null)
        {
            throw new KeyNotFoundException(
                "O veículo informado não foi encontrado.");
        }

        if (veiculo.Ativo)
        {
            throw new InvalidOperationException(
                "O veículo já está ativo.");
        }

        veiculo.Ativo = true;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Veículo reativado",
            $"Veículo ID: {veiculoId}. " +
            $"Placa: {veiculo.Placa}."
        );

        return true;
    }

    // LISTAR TODOS OS VEÍCULOS
    public async Task<List<VeiculoRespostaDto>> ListarTodosAsync()
    {
        return await _context.Veiculos
            .AsNoTracking()
            .Select(v => new VeiculoRespostaDto
            {
                VeiculoId = v.VeiculoId,
                MotoristaId = v.MotoristaId,
                Placa = v.Placa,
                TipoVeiculo = v.TipoVeiculo,
                CapacidadeToneladas = v.CapacidadeToneladas,
                Ativo = v.Ativo
            })
            .ToListAsync();
    }

    // LISTAR SOMENTE VEÍCULOS ATIVOS
    public async Task<List<VeiculoRespostaDto>> ListarAtivosAsync()
    {
        return await _context.Veiculos
            .AsNoTracking()
            .Where(v => v.Ativo)
            .Select(v => new VeiculoRespostaDto
            {
                VeiculoId = v.VeiculoId,
                MotoristaId = v.MotoristaId,
                Placa = v.Placa,
                TipoVeiculo = v.TipoVeiculo,
                CapacidadeToneladas = v.CapacidadeToneladas,
                Ativo = v.Ativo
            })
            .ToListAsync();
    }

    // BUSCAR VEÍCULO PELA PLACA
    public async Task<VeiculoRespostaDto?> BuscarPorPlacaAsync(
        string placa)
    {
        placa = FormatacaoDados.NormalizarPlaca(placa);

        return await _context.Veiculos
            .AsNoTracking()
            .Where(v => v.Placa == placa)
            .Select(v => new VeiculoRespostaDto
            {
                VeiculoId = v.VeiculoId,
                MotoristaId = v.MotoristaId,
                Placa = v.Placa,
                TipoVeiculo = v.TipoVeiculo,
                CapacidadeToneladas = v.CapacidadeToneladas,
                Ativo = v.Ativo
            })
            .FirstOrDefaultAsync();
    }

    // FILTRAR VEÍCULOS PELO TIPO
    public async Task<List<VeiculoRespostaDto>> ListarPorTipoAsync(
        string tipoVeiculo)
    {
        tipoVeiculo = tipoVeiculo.Trim().ToLower();

        return await _context.Veiculos
            .AsNoTracking()
            .Where(v =>
                v.TipoVeiculo != null &&
                v.TipoVeiculo.ToLower() == tipoVeiculo)
            .Select(v => new VeiculoRespostaDto
            {
                VeiculoId = v.VeiculoId,
                MotoristaId = v.MotoristaId,
                Placa = v.Placa,
                TipoVeiculo = v.TipoVeiculo,
                CapacidadeToneladas = v.CapacidadeToneladas,
                Ativo = v.Ativo
            })
            .ToListAsync();
    }

    // LISTAR VEÍCULOS DE UM MOTORISTA
    public async Task<List<VeiculoRespostaDto>> ListarPorMotoristaAsync(
        int motoristaId)
    {
        return await _context.Veiculos
            .AsNoTracking()
            .Where(v => v.MotoristaId == motoristaId)
            .Select(v => new VeiculoRespostaDto
            {
                VeiculoId = v.VeiculoId,
                MotoristaId = v.MotoristaId,
                Placa = v.Placa,
                TipoVeiculo = v.TipoVeiculo,
                CapacidadeToneladas = v.CapacidadeToneladas,
                Ativo = v.Ativo
            })
            .ToListAsync();
    }

    // MAPEAR ENTIDADE PARA DTO
    private static VeiculoRespostaDto MapearResposta(
        Veiculo veiculo)
    {
        return new VeiculoRespostaDto
        {
            VeiculoId = veiculo.VeiculoId,
            MotoristaId = veiculo.MotoristaId,
            Placa = veiculo.Placa,
            TipoVeiculo = veiculo.TipoVeiculo,
            CapacidadeToneladas = veiculo.CapacidadeToneladas,
            Ativo = veiculo.Ativo
        };
    }
}