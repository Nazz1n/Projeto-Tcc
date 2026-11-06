using Captacao.Api.DTOs.Documentos;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.Documentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Captacao.Api.Controllers;

// ANTES: este controller não tinha [Authorize], então qualquer pessoa na
// internet podia listar documentos, baixar CNH/CRLV por ID sequencial,
// validar e excluir arquivos.
//
// AGORA: todos os endpoints exigem usuário autenticado da equipe interna.
// A exclusão é restrita ao Administrador, por ser irreversível.
//
// IMPORTANTE: o envio de documentos pelo MOTORISTA (formulário acessado
// pelo Magic Link) não deve reutilizar este endpoint. Ele precisará de um
// endpoint próprio, autenticado pelo token do link, que descubra o
// motorista e a solicitação a partir do token no banco, sem confiar em
// MotoristaId/SolicitacaoId enviados no corpo da requisição.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly DocumentoService _documentoService;

    public DocumentosController(DocumentoService documentoService)
    {
        _documentoService = documentoService;
    }

    // ENVIAR DOCUMENTO
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<DocumentoRespostaDto>> Criar(
        [FromForm] DocumentoCadastroDto dto)
    {
        try
        {
            var documento = await _documentoService.CriarAsync(dto, User.ObterAtendenteId());

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = documento.DocumentoId },
                documento);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    // LISTAR TODOS
    [HttpGet]
    public async Task<ActionResult<List<DocumentoRespostaDto>>> ListarTodos()
    {
        var documentos = await _documentoService.ListarTodosAsync();

        return Ok(documentos);
    }

    // BUSCAR POR ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocumentoRespostaDto>> BuscarPorId(int id)
    {
        var documento = await _documentoService.BuscarPorIdAsync(id);

        if (documento == null)
            return NotFound(new { mensagem = "Documento não encontrado." });

        return Ok(documento);
    }

    // LISTAR DOCUMENTOS DO MOTORISTA
    [HttpGet("motorista/{motoristaId:int}")]
    public async Task<ActionResult<List<DocumentoRespostaDto>>>
        ListarPorMotorista(int motoristaId)
    {
        var documentos =
            await _documentoService.ListarPorMotoristaAsync(motoristaId);

        return Ok(documentos);
    }

    // LISTAR DOCUMENTOS DO VEÍCULO
    [HttpGet("veiculo/{veiculoId:int}")]
    public async Task<ActionResult<List<DocumentoRespostaDto>>>
        ListarPorVeiculo(int veiculoId)
    {
        var documentos =
            await _documentoService.ListarPorVeiculoAsync(veiculoId);

        return Ok(documentos);
    }

    // BAIXAR ARQUIVO
    // Pendência: registrar na auditoria quem baixou cada documento.
    [HttpGet("{id:int}/arquivo")]
    public async Task<IActionResult> Baixar(int id)
    {
        try
        {
            var resultado = await _documentoService.BaixarAsync(id);

            if (resultado == null)
                return NotFound(new { mensagem = "Documento não encontrado." });

            return File(
                resultado.Value.arquivo,
                resultado.Value.tipoConteudo,
                resultado.Value.nomeOriginal);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    // ATUALIZAR VALIDAÇÃO
    [HttpPatch("{id:int}/validacao")]
    public async Task<IActionResult> AtualizarValidacao(
        int id,
        DocumentoValidacaoDto dto)
    {
        var atendenteId = User.ObterAtendenteId();

        if (atendenteId == null)
            return Unauthorized(new { mensagem = "Não foi possível identificar o atendente." });

        try
        {
            await _documentoService.AtualizarValidacaoAsync(id, dto, atendenteId.Value);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    // EXCLUIR DOCUMENTO
    // Exclusão remove também o arquivo físico, por isso só o
    // Administrador pode executá-la.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Excluir(int id)
    {
        try
        {
            await _documentoService.ExcluirAsync(id, User.ObterAtendenteId());

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
