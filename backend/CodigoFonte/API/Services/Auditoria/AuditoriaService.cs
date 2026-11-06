
using Captacao.Api.DTOs.Auditoria;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Auditoria;

public class AuditoriaService
{
    private readonly CaptacaoDbContext _context;

    public AuditoriaService(CaptacaoDbContext context)
    {
        _context = context;
    }

    // Registra uma ação realizada no sistema
    public async Task RegistrarAsync(
        string acao,
        string? detalhes = null,
        int? atendenteId = null)
    {
        if (string.IsNullOrWhiteSpace(acao))
            throw new ArgumentException("A ação é obrigatória.");

        if (acao.Length > 100)
            throw new ArgumentException("A ação deve ter no máximo 100 caracteres.");

        if (atendenteId.HasValue)
        {
            bool atendenteExiste = await _context.Atendentes
                .AnyAsync(a => a.AtendenteId == atendenteId.Value);

            if (!atendenteExiste)
                throw new KeyNotFoundException("Atendente não encontrado.");
        }

        var registro = new RegistroAuditoria
        {
            AtendenteId = atendenteId,
            Acao = acao.Trim(),
            Detalhes = detalhes,
            DataRegistro = DateTime.UtcNow
        };

        _context.RegistrosAuditoria.Add(registro);

        await _context.SaveChangesAsync();
    }

    // Lista todos os registros
    public async Task<List<RegistroAuditoriaRespostaDto>> ListarTodosAsync()
    {
        return await _context.RegistrosAuditoria
            .AsNoTracking()
            .Include(r => r.Atendente)
            .OrderByDescending(r => r.DataRegistro)
            .Select(r => new RegistroAuditoriaRespostaDto
            {
                RegistroAuditoriaId = r.RegistroAuditoriaId,
                AtendenteId = r.AtendenteId,
                NomeAtendente = r.Atendente != null
                    ? r.Atendente.Nome
                    : "Sistema",
                Acao = r.Acao,
                Detalhes = r.Detalhes,
                DataRegistro = r.DataRegistro
            })
            .ToListAsync();
    }

    // Consulta um registro específico
    public async Task<RegistroAuditoriaRespostaDto> ObterPorIdAsync(
        int registroAuditoriaId)
    {
        var registro = await _context.RegistrosAuditoria
            .AsNoTracking()
            .Include(r => r.Atendente)
            .Where(r => r.RegistroAuditoriaId == registroAuditoriaId)
            .Select(r => new RegistroAuditoriaRespostaDto
            {
                RegistroAuditoriaId = r.RegistroAuditoriaId,
                AtendenteId = r.AtendenteId,
                NomeAtendente = r.Atendente != null
                    ? r.Atendente.Nome
                    : "Sistema",
                Acao = r.Acao,
                Detalhes = r.Detalhes,
                DataRegistro = r.DataRegistro
            })
            .FirstOrDefaultAsync();

        if (registro == null)
            throw new KeyNotFoundException("Registro de auditoria não encontrado.");

        return registro;
    }
}
