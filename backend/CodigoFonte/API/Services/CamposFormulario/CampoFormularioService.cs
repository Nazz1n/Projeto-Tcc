using Captacao.Api.DTOs.CamposFormulario;
using Captacao.Api.Services.Auditoria;
using Captacao.Api.Services.Solicitacoes;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.CamposFormulario;

public class CampoFormularioService
{
    private readonly CaptacaoDbContext _context;
    private readonly AuditoriaService _auditoriaService;
    private readonly SolicitacaoRegrasService _regras;

    public CampoFormularioService(
        CaptacaoDbContext context,
        AuditoriaService auditoriaService,
        SolicitacaoRegrasService regras)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _regras = regras;
    }

    // Cria os campos e documentos predefinidos que ainda não existem.
    // Chamado na inicialização. Nunca altera o que o administrador já
    // configurou.
    public async Task GarantirPadraoAsync()
    {
        var existentes = await _context.CamposFormulario
            .Select(c => c.Chave)
            .ToListAsync();

        var novos = CatalogoFormulario.Padrao
            .Where(p => !existentes.Contains(p.Chave))
            .Select(p => new CampoFormulario
            {
                Chave = p.Chave,
                Rotulo = p.Rotulo,
                Categoria = p.Categoria,
                TipoCampo = p.TipoCampo,
                Obrigatorio = p.ObrigatorioPadrao,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            })
            .ToList();

        if (novos.Count == 0) return;

        _context.CamposFormulario.AddRange(novos);
        await _context.SaveChangesAsync();
    }

    public async Task<List<CampoFormularioRespostaDto>> ListarTodosAsync()
    {
        var campos = await _context.CamposFormulario
            .AsNoTracking()
            .OrderBy(c => c.Categoria)
            .ThenBy(c => c.CampoFormularioId)
            .ToListAsync();

        return campos.Select(MapearResposta).ToList();
    }

    public async Task<List<CampoFormularioRespostaDto>> ListarAtivosAsync()
    {
        var campos = await _context.CamposFormulario
            .AsNoTracking()
            .Where(c => c.Ativo)
            .OrderBy(c => c.Categoria)
            .ThenBy(c => c.CampoFormularioId)
            .ToListAsync();

        return campos.Select(MapearResposta).ToList();
    }

    public async Task<CampoFormularioRespostaDto> ObterPorIdAsync(int campoFormularioId)
    {
        var campo = await _context.CamposFormulario
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CampoFormularioId == campoFormularioId)
            ?? throw new KeyNotFoundException("Campo do formulário não encontrado.");

        return MapearResposta(campo);
    }

    public async Task<CampoFormularioRespostaDto> AtualizarAsync(
        int campoFormularioId,
        CampoFormularioAtualizacaoDto dto,
        int? atendenteId = null)
    {
        var campo = await _context.CamposFormulario
            .FirstOrDefaultAsync(c => c.CampoFormularioId == campoFormularioId)
            ?? throw new KeyNotFoundException("Campo do formulário não encontrado.");

        if (string.IsNullOrWhiteSpace(dto.Rotulo))
            throw new InvalidOperationException("O rótulo do campo é obrigatório.");

        // Um documento inativo não pode ser obrigatório (RF05).
        if (dto.Obrigatorio && !campo.Ativo)
            throw new InvalidOperationException(
                "Um item inativo não pode ser obrigatório. Ative-o primeiro.");

        var rotuloAnterior = campo.Rotulo;
        var obrigatorioAnterior = campo.Obrigatorio;

        campo.Rotulo = dto.Rotulo.Trim();
        campo.Obrigatorio = dto.Obrigatorio;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Campo de formulário atualizado",
            $"Campo ID: {campo.CampoFormularioId}. Chave: {campo.Chave}. " +
            $"Rótulo anterior: {rotuloAnterior}; atual: {campo.Rotulo}. " +
            $"Obrigatório anterior: {obrigatorioAnterior}; atual: {campo.Obrigatorio}.",
            atendenteId);

        // As solicitações abertas passam a ser avaliadas pela nova regra.
        if (obrigatorioAnterior != campo.Obrigatorio)
            await _regras.RecalcularTodasAbertasAsync();

        return MapearResposta(campo);
    }

    // Só documentos podem ser ativados/desativados; os campos de dados
    // fazem parte do cadastro mínimo. Desativar um documento remove sua
    // obrigatoriedade (Figura 13).
    public async Task<CampoFormularioRespostaDto> AlterarAtivoAsync(
        int campoFormularioId,
        bool ativo,
        int? atendenteId = null)
    {
        var campo = await _context.CamposFormulario
            .FirstOrDefaultAsync(c => c.CampoFormularioId == campoFormularioId)
            ?? throw new KeyNotFoundException("Campo do formulário não encontrado.");

        if (campo.Categoria != CategoriaCampoFormulario.Documento)
            throw new InvalidOperationException(
                "Somente documentos podem ser ativados ou desativados.");

        var ativoAnterior = campo.Ativo;
        var obrigatorioAnterior = campo.Obrigatorio;

        campo.Ativo = ativo;

        if (!ativo) campo.Obrigatorio = false;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            ativo ? "Campo de formulário ativado" : "Campo de formulário desativado",
            $"Campo ID: {campo.CampoFormularioId}. Chave: {campo.Chave}. " +
            $"Ativo anterior: {ativoAnterior}; atual: {campo.Ativo}. " +
            $"Obrigatório anterior: {obrigatorioAnterior}; atual: {campo.Obrigatorio}.",
            atendenteId);

        if (ativoAnterior != campo.Ativo || obrigatorioAnterior != campo.Obrigatorio)
            await _regras.RecalcularTodasAbertasAsync();

        return MapearResposta(campo);
    }

    private static CampoFormularioRespostaDto MapearResposta(CampoFormulario campo) => new()
    {
        CampoFormularioId = campo.CampoFormularioId,
        Chave = campo.Chave,
        Rotulo = campo.Rotulo,
        Categoria = campo.Categoria,
        TipoCampo = campo.TipoCampo,
        Obrigatorio = campo.Obrigatorio,
        Ativo = campo.Ativo,
        DataCriacao = campo.DataCriacao
    };
}
