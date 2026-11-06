using Captacao.Api.DTOs.Documentos;
using Captacao.Api.Services.Auditoria;
using Captacao.Api.Services.Solicitacoes;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Documentos;

public class DocumentoService
{
    private readonly CaptacaoDbContext _context;
    private readonly ArmazenamentoDocumentoService _armazenamento;
    private readonly AuditoriaService _auditoriaService;
    private readonly SolicitacaoRegrasService _regras;

    public DocumentoService(
        CaptacaoDbContext context,
        ArmazenamentoDocumentoService armazenamento,
        AuditoriaService auditoriaService,
        SolicitacaoRegrasService regras)
    {
        _context = context;
        _armazenamento = armazenamento;
        _auditoriaService = auditoriaService;
        _regras = regras;
    }

    // ENVIAR DOCUMENTO
    public async Task<DocumentoRespostaDto> CriarAsync(
        DocumentoCadastroDto dto, int? atendenteId = null)
    {
        var motoristaExiste = await _context.Motoristas
            .AnyAsync(m => m.MotoristaId == dto.MotoristaId);

        if (!motoristaExiste)
            throw new ArgumentException(
                "O motorista informado não foi encontrado.");

        if (dto.VeiculoId.HasValue)
        {
            var veiculoExiste = await _context.Veiculos
                .AnyAsync(v =>
                    v.VeiculoId == dto.VeiculoId.Value &&
                    v.MotoristaId == dto.MotoristaId);

            if (!veiculoExiste)
                throw new ArgumentException(
                    "O veículo não existe ou não pertence ao motorista informado.");
        }

        if (dto.SolicitacaoId.HasValue)
        {
            var solicitacaoExiste = await _context.Solicitacoes
                .AnyAsync(s =>
                    s.SolicitacaoId == dto.SolicitacaoId.Value);

            if (!solicitacaoExiste)
                throw new ArgumentException(
                    "A solicitação informada não foi encontrada.");
        }

        if (dto.Arquivo == null)
            throw new ArgumentException("Selecione um arquivo.");

        // O tipo precisa ser um documento ativo da configuração do formulário.
        var tipo = await ResolverTipoDocumentoAsync(dto.TipoDocumento);

        if (CatalogoFormulario.DocumentoDoVeiculo(tipo) && !dto.VeiculoId.HasValue)
            throw new ArgumentException("Informe o veículo ao enviar o CRLV.");

        var resultado = await _armazenamento.SalvarAsync(dto.Arquivo);

        Documento documento;

        // O arquivo só é removido se o registro no banco falhar. Falhas
        // posteriores (auditoria, recálculo) não devem apagar o arquivo.
        try
        {
            documento = new Documento
            {
                MotoristaId = dto.MotoristaId,
                VeiculoId = dto.VeiculoId,
                SolicitacaoId = dto.SolicitacaoId,
                TipoDocumento = tipo,
                ChaveArquivo = resultado.chave,
                NomeOriginal = Path.GetFileName(dto.Arquivo.FileName),
                TipoConteudo = resultado.tipoConteudo,
                TamanhoBytes = dto.Arquivo.Length,
                StatusValidacao = StatusDocumento.Pendente,
                DataEnvio = DateTime.UtcNow
            };

            _context.Documentos.Add(documento);

            await _context.SaveChangesAsync();
        }
        catch
        {
            _armazenamento.Excluir(resultado.chave);
            throw;
        }

        await _auditoriaService.RegistrarAsync(
            "Documento criado",
            $"Documento ID: {documento.DocumentoId}. " +
            $"Motorista ID: {documento.MotoristaId}. " +
            $"Veículo ID: {documento.VeiculoId?.ToString() ?? "Não informado"}. " +
            $"Solicitação ID: {documento.SolicitacaoId?.ToString() ?? "Não informado"}. " +
            $"Tipo: {documento.TipoDocumento}. " +
            $"Nome do arquivo: {documento.NomeOriginal}.",
            atendenteId
        );

        // Novo arquivo pode resolver pendências (RF07, RF14).
        await RecalcularAsync(documento);

        return MapearResposta(documento);
    }

    // LISTAR TODOS
    public async Task<List<DocumentoRespostaDto>> ListarTodosAsync()
    {
        var documentos = await _context.Documentos
            .AsNoTracking()
            .OrderByDescending(d => d.DataEnvio)
            .ToListAsync();

        return documentos.Select(MapearResposta).ToList();
    }

    // BUSCAR POR ID
    public async Task<DocumentoRespostaDto?> BuscarPorIdAsync(int id)
    {
        var documento = await _context.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentoId == id);

        return documento == null
            ? null
            : MapearResposta(documento);
    }

    // LISTAR DOCUMENTOS DO MOTORISTA
    public async Task<List<DocumentoRespostaDto>>
        ListarPorMotoristaAsync(int motoristaId)
    {
        return await _context.Documentos
            .AsNoTracking()
            .Where(d => d.MotoristaId == motoristaId)
            .OrderByDescending(d => d.DataEnvio)
            .Select(d => new DocumentoRespostaDto
            {
                DocumentoId = d.DocumentoId,
                MotoristaId = d.MotoristaId,
                VeiculoId = d.VeiculoId,
                SolicitacaoId = d.SolicitacaoId,
                TipoDocumento = d.TipoDocumento,
                NomeOriginal = d.NomeOriginal,
                TipoConteudo = d.TipoConteudo,
                TamanhoBytes = d.TamanhoBytes,
                StatusValidacao = d.StatusValidacao,
                DataEnvio = d.DataEnvio,
                DataValidacao = d.DataValidacao
            })
            .ToListAsync();
    }

    // LISTAR DOCUMENTOS DO VEÍCULO
    public async Task<List<DocumentoRespostaDto>>
        ListarPorVeiculoAsync(int veiculoId)
    {
        return await _context.Documentos
            .AsNoTracking()
            .Where(d => d.VeiculoId == veiculoId)
            .OrderByDescending(d => d.DataEnvio)
            .Select(d => new DocumentoRespostaDto
            {
                DocumentoId = d.DocumentoId,
                MotoristaId = d.MotoristaId,
                VeiculoId = d.VeiculoId,
                SolicitacaoId = d.SolicitacaoId,
                TipoDocumento = d.TipoDocumento,
                NomeOriginal = d.NomeOriginal,
                TipoConteudo = d.TipoConteudo,
                TamanhoBytes = d.TamanhoBytes,
                StatusValidacao = d.StatusValidacao,
                DataEnvio = d.DataEnvio,
                DataValidacao = d.DataValidacao
            })
            .ToListAsync();
    }

    // BAIXAR ARQUIVO
    public async Task<(byte[] arquivo, string tipoConteudo,
        string nomeOriginal)?> BaixarAsync(int id)
    {
        var documento = await _context.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentoId == id);

        if (documento == null)
            return null;

        var arquivo = await _armazenamento.LerAsync(
            documento.ChaveArquivo);

        return (
            arquivo,
            documento.TipoConteudo,
            documento.NomeOriginal
        );
    }

    // ATUALIZAR STATUS DE VALIDAÇÃO
    // Recalcula a prioridade e o status da solicitação (RF07, RF14): a
    // reprovação de um documento obrigatório devolve a solicitação para
    // "Com pendências".
    public async Task AtualizarValidacaoAsync(
        int id,
        DocumentoValidacaoDto dto,
        int atendenteId)
    {
        var documento = await _context.Documentos
            .FirstOrDefaultAsync(d => d.DocumentoId == id);

        if (documento == null)
            throw new KeyNotFoundException(
                "Documento não encontrado.");

        var novoStatus = StatusDocumento.Normalizar(dto.StatusValidacao);

        if (novoStatus == null || novoStatus == StatusDocumento.Pendente)
            throw new ArgumentException(
                "O status deve ser Validado ou Reprovado.");

        var statusAnterior = documento.StatusValidacao;

        documento.StatusValidacao = novoStatus;
        documento.DataValidacao = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Documento validado",
            $"Documento ID: {documento.DocumentoId}. " +
            $"Motorista ID: {documento.MotoristaId}. " +
            $"Tipo: {documento.TipoDocumento}. " +
            $"Status anterior: {statusAnterior}. " +
            $"Novo status: {documento.StatusValidacao}.",
            atendenteId);

        await RecalcularAsync(documento);
    }

    // EXCLUIR DOCUMENTO
    public async Task ExcluirAsync(int id, int? atendenteId = null)
    {
        var documento = await _context.Documentos
            .FirstOrDefaultAsync(d => d.DocumentoId == id);

        if (documento == null)
            throw new KeyNotFoundException(
                "Documento não encontrado.");

        var nomeArquivo = documento.NomeOriginal;
        var tipoDocumento = documento.TipoDocumento;
        var motoristaId = documento.MotoristaId;
        var solicitacaoId = documento.SolicitacaoId;

        _context.Documentos.Remove(documento);

        await _context.SaveChangesAsync();

        // Remove o arquivo físico
        _armazenamento.Excluir(documento.ChaveArquivo);

        // Registra a exclusão na auditoria
        await _auditoriaService.RegistrarAsync(
            "Documento excluído",
            $"Documento ID: {id}. " +
            $"Motorista ID: {motoristaId}. " +
            $"Tipo: {tipoDocumento}. " +
            $"Arquivo: {nomeArquivo}.",
            atendenteId
        );

        if (solicitacaoId.HasValue)
            await _regras.RecalcularAsync(solicitacaoId.Value);
        else
            await _regras.RecalcularAbertasDoMotoristaAsync(motoristaId);
    }

    // Documento ligado a uma solicitação recalcula só ela; sem
    // solicitação, recalcula as abertas do motorista.
    private Task RecalcularAsync(Documento documento) =>
        documento.SolicitacaoId.HasValue
            ? _regras.RecalcularAsync(documento.SolicitacaoId.Value)
            : _regras.RecalcularAbertasDoMotoristaAsync(documento.MotoristaId);

    private async Task<string> ResolverTipoDocumentoAsync(string tipoInformado)
    {
        var chave = (tipoInformado ?? string.Empty).Trim().ToLowerInvariant();

        var configurado = await _context.CamposFormulario
            .AsNoTracking()
            .AnyAsync(c =>
                c.Chave == chave &&
                c.Categoria == CategoriaCampoFormulario.Documento &&
                c.Ativo);

        if (!configurado)
            throw new ArgumentException(
                "Tipo de documento inválido ou desativado no formulário.");

        return chave;
    }

    // MAPEAMENTO
    private static DocumentoRespostaDto MapearResposta(
        Documento documento)
    {
        return new DocumentoRespostaDto
        {
            DocumentoId = documento.DocumentoId,
            MotoristaId = documento.MotoristaId,
            VeiculoId = documento.VeiculoId,
            SolicitacaoId = documento.SolicitacaoId,
            TipoDocumento = documento.TipoDocumento,
            NomeOriginal = documento.NomeOriginal,
            TipoConteudo = documento.TipoConteudo,
            TamanhoBytes = documento.TamanhoBytes,
            StatusValidacao = documento.StatusValidacao,
            DataEnvio = documento.DataEnvio,
            DataValidacao = documento.DataValidacao
        };
    }
}