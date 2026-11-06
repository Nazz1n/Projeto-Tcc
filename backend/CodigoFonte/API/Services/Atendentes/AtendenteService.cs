
using Captacao.Api.DTOs.Atendentes;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Atendentes;

public class AtendenteService
{
    private readonly CaptacaoDbContext _context;
    private readonly IPasswordHasher<Atendente> _passwordHasher;
    private readonly AuditoriaService _auditoriaService;

    public AtendenteService(
        CaptacaoDbContext context,
        IPasswordHasher<Atendente> passwordHasher,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditoriaService = auditoriaService;
    }

    // Cadastra um novo atendente
    public async Task<AtendenteRespostaDto> CriarAsync(
        AtendenteCadastroDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var emailExiste = await _context.Atendentes
            .AnyAsync(a => a.Email == email);

        if (emailExiste)
        {
            throw new InvalidOperationException(
                "Já existe um atendente cadastrado com este e-mail.");
        }

        var atendente = new Atendente
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Perfil = "Atendente",
            Ativo = true
        };

        atendente.SenhaHash = _passwordHasher.HashPassword(
            atendente,
            dto.Senha);

        _context.Atendentes.Add(atendente);

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Atendente criado",
            $"Atendente ID: {atendente.AtendenteId}. " +
            $"Nome: {atendente.Nome}. " +
            $"E-mail: {atendente.Email}."
        );

        return MapearResposta(atendente);
    }

    // Lista todos os atendentes
    public async Task<List<AtendenteRespostaDto>> ListarTodosAsync()
    {
        var atendentes = await _context.Atendentes
            .AsNoTracking()
            .OrderBy(a => a.Nome)
            .ToListAsync();

        return atendentes.Select(MapearResposta).ToList();
    }

    // Lista somente os atendentes ativos
    public async Task<List<AtendenteRespostaDto>> ListarAtivosAsync()
    {
        var atendentes = await _context.Atendentes
            .AsNoTracking()
            .Where(a => a.Ativo)
            .OrderBy(a => a.Nome)
            .ToListAsync();

        return atendentes.Select(MapearResposta).ToList();
    }

    // Busca um atendente pelo ID
    public async Task<AtendenteRespostaDto?> BuscarPorIdAsync(int id)
    {
        var atendente = await _context.Atendentes
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AtendenteId == id);

        if (atendente == null)
        {
            return null;
        }

        return MapearResposta(atendente);
    }

    // Atualiza nome e e-mail
    public async Task<AtendenteRespostaDto> AtualizarAsync(
        int id,
        AtendenteAtualizacaoDto dto)
    {
        var atendente = await _context.Atendentes
            .FirstOrDefaultAsync(a => a.AtendenteId == id);

        if (atendente == null)
        {
            throw new KeyNotFoundException(
                "Atendente não encontrado.");
        }

        var nomeAnterior = atendente.Nome;
        var emailAnterior = atendente.Email;

        var email = dto.Email.Trim().ToLowerInvariant();

        var emailExiste = await _context.Atendentes
            .AnyAsync(a =>
                a.Email == email &&
                a.AtendenteId != id);

        if (emailExiste)
        {
            throw new InvalidOperationException(
                "Este e-mail já está sendo utilizado por outro atendente.");
        }

        atendente.Nome = dto.Nome.Trim();
        atendente.Email = email;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Atendente atualizado",
            $"Atendente ID: {id}. " +
            $"Nome anterior: {nomeAnterior}. " +
            $"Nome atual: {atendente.Nome}. " +
            $"E-mail anterior: {emailAnterior}. " +
            $"E-mail atual: {atendente.Email}."
        );

        return MapearResposta(atendente);
    }

    // Inativa um atendente
    public async Task InativarAsync(int id)
    {
        var atendente = await _context.Atendentes
            .FirstOrDefaultAsync(a => a.AtendenteId == id);

        if (atendente == null)
        {
            throw new KeyNotFoundException(
                "Atendente não encontrado.");
        }

        if (!atendente.Ativo)
        {
            return;
        }

        atendente.Ativo = false;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Atendente inativado",
            $"Atendente ID: {id}. Nome: {atendente.Nome}."
        );
    }

    // Reativa um atendente
    public async Task ReativarAsync(int id)
    {
        var atendente = await _context.Atendentes
            .FirstOrDefaultAsync(a => a.AtendenteId == id);

        if (atendente == null)
        {
            throw new KeyNotFoundException(
                "Atendente não encontrado.");
        }

        if (atendente.Ativo)
        {
            return;
        }

        atendente.Ativo = true;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Atendente reativado",
            $"Atendente ID: {id}. Nome: {atendente.Nome}."
        );
    }

    // Converte a entidade em DTO de resposta
    private static AtendenteRespostaDto MapearResposta(
        Atendente atendente)
    {
        return new AtendenteRespostaDto
        {
            AtendenteId = atendente.AtendenteId,
            Nome = atendente.Nome,
            Email = atendente.Email,
            Perfil = atendente.Perfil,
            Ativo = atendente.Ativo
        };
    }
}
