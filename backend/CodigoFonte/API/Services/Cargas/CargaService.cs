using Captacao.Api.DTOs.Cargas;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Cargas;

public class CargaService
{
    private readonly CaptacaoDbContext _context;
    private readonly AuditoriaService _auditoriaService;

    public CargaService(
        CaptacaoDbContext context,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
    }

    // =========================================================
    // CRIAR
    // =========================================================

    public async Task<CargaRespostaDto> CriarAsync(
        CargaCadastroDto dto,
        int atendenteId)
    {
        if (dto == null)
            throw new ArgumentException("Os dados da carga são obrigatórios.");

        if (string.IsNullOrWhiteSpace(dto.Origem))
            throw new ArgumentException("A origem é obrigatória.");

        if (string.IsNullOrWhiteSpace(dto.Destino))
            throw new ArgumentException("O destino é obrigatório.");

        if (dto.CapacidadeMinimaToneladas.HasValue &&
            dto.CapacidadeMinimaToneladas.Value <= 0)
        {
            throw new ArgumentException(
                "A capacidade mínima deve ser maior que zero.");
        }

        var atendenteExiste = await _context.Atendentes
            .AnyAsync(a =>
                a.AtendenteId == atendenteId &&
                a.Ativo);

        if (!atendenteExiste)
            throw new ArgumentException(
                "O atendente informado não existe ou está inativo.");

        var carga = new Carga
        {
            AtendenteId = atendenteId,
            Origem = dto.Origem.Trim(),
            Destino = dto.Destino.Trim(),
            CapacidadeMinimaToneladas =
                dto.CapacidadeMinimaToneladas,
            Status = StatusCarga.Disponivel,
            DataCadastro = DateTime.UtcNow
        };

        _context.Cargas.Add(carga);

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Carga criada",
            $"Carga {carga.CargaId} criada. " +
            $"Origem: {carga.Origem}. " +
            $"Destino: {carga.Destino}. " +
            $"Capacidade mínima: {carga.CapacidadeMinimaToneladas?.ToString() ?? "Não informada"} toneladas.",
            atendenteId);

        return MapearResposta(carga);
    }

    // =========================================================
    // LISTAR TODAS
    // =========================================================

    public async Task<List<CargaRespostaDto>> ListarTodosAsync()
    {
        var cargas = await _context.Cargas
            .AsNoTracking()
            .OrderByDescending(c => c.DataCadastro)
            .ToListAsync();

        return cargas
            .Select(MapearResposta)
            .ToList();
    }

    // =========================================================
    // LISTAR DISPONÍVEIS
    // =========================================================

    public async Task<List<CargaRespostaDto>> ListarDisponiveisAsync()
    {
        var cargas = await _context.Cargas
            .AsNoTracking()
            .Where(c => c.Status == StatusCarga.Disponivel)
            .OrderByDescending(c => c.DataCadastro)
            .ToListAsync();

        return cargas
            .Select(MapearResposta)
            .ToList();
    }

    // =========================================================
    // LISTAR POR ATENDENTE
    // =========================================================

    public async Task<List<CargaRespostaDto>> ListarPorAtendenteAsync(
        int atendenteId)
    {
        var atendenteExiste = await _context.Atendentes
            .AnyAsync(a => a.AtendenteId == atendenteId);

        if (!atendenteExiste)
            throw new ArgumentException(
                "Atendente não encontrado.");

        var cargas = await _context.Cargas
            .AsNoTracking()
            .Where(c => c.AtendenteId == atendenteId)
            .OrderByDescending(c => c.DataCadastro)
            .ToListAsync();

        return cargas
            .Select(MapearResposta)
            .ToList();
    }

    // =========================================================
    // BUSCAR POR ID
    // =========================================================

    public async Task<CargaRespostaDto?> BuscarPorIdAsync(
        int cargaId)
    {
        var carga = await _context.Cargas
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.CargaId == cargaId);

        if (carga == null)
            return null;

        return MapearResposta(carga);
    }

    // =========================================================
    // ATUALIZAR
    // =========================================================

    public async Task<CargaRespostaDto> AtualizarAsync(
        int cargaId,
        CargaAtualizacaoDto dto,
        int? executorId = null)
    {
        if (dto == null)
            throw new ArgumentException(
                "Os dados da atualização são obrigatórios.");

        if (string.IsNullOrWhiteSpace(dto.Origem))
            throw new ArgumentException(
                "A origem é obrigatória.");

        if (string.IsNullOrWhiteSpace(dto.Destino))
            throw new ArgumentException(
                "O destino é obrigatório.");

        if (dto.CapacidadeMinimaToneladas.HasValue &&
            dto.CapacidadeMinimaToneladas.Value <= 0)
        {
            throw new ArgumentException(
                "A capacidade mínima deve ser maior que zero.");
        }

        var carga = await _context.Cargas
            .FirstOrDefaultAsync(c =>
                c.CargaId == cargaId);

        if (carga == null)
            throw new KeyNotFoundException(
                "Carga não encontrada.");

        var origemAnterior = carga.Origem;
        var destinoAnterior = carga.Destino;
        var capacidadeAnterior =
            carga.CapacidadeMinimaToneladas;

        carga.Origem = dto.Origem.Trim();
        carga.Destino = dto.Destino.Trim();
        carga.CapacidadeMinimaToneladas =
            dto.CapacidadeMinimaToneladas;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Carga atualizada",
            $"Carga {carga.CargaId} atualizada. " +
            $"Origem: {origemAnterior} -> {carga.Origem}. " +
            $"Destino: {destinoAnterior} -> {carga.Destino}. " +
            $"Capacidade mínima: {capacidadeAnterior?.ToString() ?? "Não informada"} -> " +
            $"{carga.CapacidadeMinimaToneladas?.ToString() ?? "Não informada"} toneladas.",
            executorId ?? carga.AtendenteId);

        return MapearResposta(carga);
    }

    // =========================================================
    // ATUALIZAR STATUS
    // =========================================================

    public async Task<CargaRespostaDto> AtualizarStatusAsync(
        int cargaId,
        string novoStatus,
        int? executorId = null)
    {
        if (string.IsNullOrWhiteSpace(novoStatus))
            throw new ArgumentException(
                "O novo status é obrigatório.");

        novoStatus = novoStatus.Trim();

        var statusValido = StatusCarga.Normalizar(novoStatus);

        if (statusValido == null)
        {
            throw new ArgumentException(
                "Status inválido. Utilize: Disponivel ou Contratada.");
        }

        var carga = await _context.Cargas
            .FirstOrDefaultAsync(c =>
                c.CargaId == cargaId);

        if (carga == null)
            throw new KeyNotFoundException(
                "Carga não encontrada.");

        var statusAnterior = carga.Status;

        if (statusAnterior.Equals(
                statusValido,
                StringComparison.OrdinalIgnoreCase))
        {
            return MapearResposta(carga);
        }

        carga.Status = statusValido;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Status da carga atualizado",
            $"Carga {carga.CargaId}: " +
            $"status alterado de '{statusAnterior}' " +
            $"para '{carga.Status}'.",
            executorId ?? carga.AtendenteId);

        return MapearResposta(carga);
    }

    // =========================================================
    // MAPEAMENTO
    // =========================================================

    private static CargaRespostaDto MapearResposta(
        Carga carga)
    {
        return new CargaRespostaDto
        {
            CargaId = carga.CargaId,
            AtendenteId = carga.AtendenteId,
            Origem = carga.Origem,
            Destino = carga.Destino,
            CapacidadeMinimaToneladas =
                carga.CapacidadeMinimaToneladas,
            Status = carga.Status,
            DataCadastro = carga.DataCadastro
        };
    }
}