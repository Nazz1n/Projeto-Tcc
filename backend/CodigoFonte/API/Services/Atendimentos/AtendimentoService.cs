using Captacao.Api.DTOs.Atendimentos;
using Captacao.Api.Services.LinksAcesso;
using Captacao.Api.Services.Motoristas;
using Captacao.Api.Services.Solicitacoes;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Atendimentos;

// RF01 com envio manual: o motorista escreve para o WhatsApp Business, e o
// atendente informa o telefone aqui. Em uma só chamada o sistema acha ou
// cria o motorista, acha ou cria a solicitação aberta e gera o link único
// do formulário, que o atendente envia pelo próprio WhatsApp.
public class AtendimentoService
{
    private readonly CaptacaoDbContext _context;
    private readonly MotoristaService _motoristas;
    private readonly SolicitacaoService _solicitacoes;
    private readonly LinkAcessoService _links;

    public AtendimentoService(
        CaptacaoDbContext context,
        MotoristaService motoristas,
        SolicitacaoService solicitacoes,
        LinkAcessoService links)
    {
        _context = context;
        _motoristas = motoristas;
        _solicitacoes = solicitacoes;
        _links = links;
    }

    public async Task<IniciarAtendimentoRespostaDto> IniciarAsync(
        IniciarAtendimentoDto dto, int? atendenteId)
    {
        var digitos = FormatacaoDados.NormalizarTelefone(dto.Telefone);

        if (!FormatacaoDados.TelefoneValido(digitos))
            throw new ArgumentException("Telefone inválido. Informe DDD e número.");

        // Um mesmo motorista pode ter sido cadastrado com ou sem o 55:
        // procura pelas duas formas antes de criar outro.
        var candidatos = CandidatosDeTelefone(digitos);

        var existente = await _context.Motoristas
            .FirstOrDefaultAsync(m => candidatos.Contains(m.Telefone));

        var motorista = existente;
        var motoristaCriado = false;

        if (motorista == null)
        {
            // Novos cadastros ficam sempre com o DDI (formato do wa.me).
            (motorista, motoristaCriado) =
                await _motoristas.ObterOuCriarPorTelefoneAsync(candidatos[0]);
        }

        var (solicitacao, solicitacaoCriada) =
            await _solicitacoes.ObterOuCriarAbertaAsync(motorista.MotoristaId, atendenteId);

        var link = await _links.GerarAsync(solicitacao.SolicitacaoId);

        return new IniciarAtendimentoRespostaDto
        {
            MotoristaId = motorista.MotoristaId,
            NomeMotorista = motorista.NomeCompleto,
            MotoristaCriado = motoristaCriado,
            SolicitacaoId = solicitacao.SolicitacaoId,
            SolicitacaoCriada = solicitacaoCriada,
            StatusSolicitacao = solicitacao.Status,
            Link = link
        };
    }

    // [0] é a forma canônica (com 55); as demais são variações aceitas.
    internal static List<string> CandidatosDeTelefone(string digitos)
    {
        var comDdi = FormatacaoDados.AdicionarDdiBrasilSeNecessario(digitos);

        if (comDdi != digitos)
            return new List<string> { comDdi, digitos };

        if (digitos.StartsWith("55") && digitos.Length is 12 or 13)
            return new List<string> { digitos, digitos[2..] };

        return new List<string> { digitos };
    }
}
