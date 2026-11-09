using System.Net.Mail;
using Captacao.Api.DTOs.Documentos;
using Captacao.Api.DTOs.Formulario;
using Captacao.Api.Services.Auditoria;
using Captacao.Api.Services.Documentos;
using Captacao.Api.Services.LinksAcesso;
using Captacao.Api.Services.Motoristas;
using Captacao.Api.Services.Solicitacoes;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Formulario;

// Formulário público do motorista (UC02). Todo acesso é identificado pelo
// token do link: o motorista e a solicitação são sempre obtidos do banco a
// partir dele, nunca de ids enviados pelo cliente.
public class FormularioService
{
    private const decimal CapacidadeMaxima = 200m;

    private readonly CaptacaoDbContext _context;
    private readonly LinkAcessoService _links;
    private readonly SolicitacaoRegrasService _regras;
    private readonly DocumentoService _documentos;
    private readonly CpfProtecaoService _cpf;
    private readonly AuditoriaService _auditoria;

    public FormularioService(
        CaptacaoDbContext context,
        LinkAcessoService links,
        SolicitacaoRegrasService regras,
        DocumentoService documentos,
        CpfProtecaoService cpf,
        AuditoriaService auditoria)
    {
        _context = context;
        _links = links;
        _regras = regras;
        _documentos = documentos;
        _cpf = cpf;
        _auditoria = auditoria;
    }

    // ---------------------------------------------------------------
    // GET: configuração + dados já informados
    // ---------------------------------------------------------------
    public async Task<FormularioRespostaDto> ObterAsync(string token)
    {
        var solicitacao = await CarregarPorTokenAsync(token);

        // Primeiro acesso ao formulário (RF14).
        if (solicitacao.Status == StatusSolicitacao.NovoContato)
        {
            _regras.AplicarStatus(solicitacao, StatusSolicitacao.AguardandoPreenchimento, null);
            await _regras.RecalcularAsync(solicitacao);
        }

        return await MontarRespostaAsync(solicitacao, token);
    }

    // ---------------------------------------------------------------
    // PUT: dados pessoais e veículo
    // ---------------------------------------------------------------
    public async Task<FormularioRespostaDto> SalvarDadosAsync(string token, FormularioDadosDto dto)
    {
        var solicitacao = await CarregarPorTokenAsync(token);
        var motorista = solicitacao.Motorista;

        if (dto.NomeCompleto != null)
        {
            var nome = dto.NomeCompleto.Trim();
            motorista.NomeCompleto = nome.Length == 0 ? null : nome;
        }

        if (dto.Email != null)
        {
            var email = dto.Email.Trim();

            if (email.Length == 0)
            {
                motorista.Email = null;
            }
            else
            {
                if (!MailAddress.TryCreate(email, out var endereco) || endereco.Address != email)
                    throw new ArgumentException("E-mail inválido.");

                motorista.Email = email;
            }
        }

        // O CPF devolvido pela API vem mascarado; só é regravado quando o
        // motorista digita um valor novo.
        if (!string.IsNullOrWhiteSpace(dto.Cpf) && !dto.Cpf.Contains('*'))
        {
            var cpf = _cpf.Normalizar(dto.Cpf);

            if (!_cpf.Validar(cpf))
                throw new ArgumentException("CPF inválido.");

            var hash = _cpf.GerarHash(cpf);

            var emUso = await _context.Motoristas.AnyAsync(m =>
                m.CpfHash == hash && m.MotoristaId != motorista.MotoristaId);

            if (emUso)
                throw new InvalidOperationException(
                    "Não foi possível usar este CPF. Procure o atendente.");

            motorista.Cpf = _cpf.Criptografar(cpf);
            motorista.CpfHash = hash;
        }

        await AplicarVeiculoAsync(solicitacao, dto);

        motorista.DataAtualizacao = DateTime.UtcNow;
        solicitacao.DataAtualizacao = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Sem auditoria dos valores: apenas o fato, sem dado pessoal.
        await _auditoria.RegistrarAsync(
            "Dados do formulário atualizados",
            $"Solicitação ID: {solicitacao.SolicitacaoId}. Motorista ID: {motorista.MotoristaId}.");

        await _regras.RecalcularAsync(solicitacao);

        return await MontarRespostaAsync(solicitacao, token);
    }

    private async Task AplicarVeiculoAsync(Solicitacao solicitacao, FormularioDadosDto dto)
    {
        var motoristaId = solicitacao.MotoristaId;

        if (dto.CapacidadeToneladas is { } c && (c <= 0 || c > CapacidadeMaxima))
            throw new ArgumentException(
                $"A capacidade deve ser maior que zero e no máximo {CapacidadeMaxima:0} toneladas.");

        if (dto.VeiculoId.HasValue)
        {
            var veiculo = await _context.Veiculos.FirstOrDefaultAsync(v =>
                v.VeiculoId == dto.VeiculoId.Value &&
                v.MotoristaId == motoristaId &&
                v.Ativo)
                ?? throw new ArgumentException("Veículo não encontrado.");

            if (dto.CapacidadeToneladas.HasValue)
                veiculo.CapacidadeToneladas = dto.CapacidadeToneladas.Value;

            solicitacao.VeiculoId = veiculo.VeiculoId;
            solicitacao.Veiculo = veiculo;
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.Placa))
        {
            // Só capacidade: ajusta o veículo já selecionado.
            if (dto.CapacidadeToneladas.HasValue && solicitacao.Veiculo != null)
                solicitacao.Veiculo.CapacidadeToneladas = dto.CapacidadeToneladas.Value;

            return;
        }

        var placa = FormatacaoDados.NormalizarPlaca(dto.Placa);

        if (!FormatacaoDados.PlacaValida(placa))
            throw new ArgumentException("Placa inválida. Use o formato ABC1234 ou ABC1D23.");

        var existente = await _context.Veiculos.FirstOrDefaultAsync(v => v.Placa == placa);

        if (existente != null && existente.MotoristaId != motoristaId)
            throw new InvalidOperationException(
                "Não foi possível usar esta placa. Procure o atendente.");

        if (existente != null)
        {
            existente.Ativo = true;

            if (dto.CapacidadeToneladas.HasValue)
                existente.CapacidadeToneladas = dto.CapacidadeToneladas.Value;

            solicitacao.VeiculoId = existente.VeiculoId;
            solicitacao.Veiculo = existente;
            return;
        }

        if (!dto.CapacidadeToneladas.HasValue)
            throw new ArgumentException("Informe a capacidade em toneladas do veículo.");

        var novo = new Veiculo
        {
            MotoristaId = motoristaId,
            Placa = placa,
            CapacidadeToneladas = dto.CapacidadeToneladas.Value,
            Ativo = true
        };

        _context.Veiculos.Add(novo);
        solicitacao.Veiculo = novo; // o EF preenche VeiculoId ao salvar
    }

    // ---------------------------------------------------------------
    // POST: documento
    // ---------------------------------------------------------------
    public async Task<FormularioRespostaDto> EnviarDocumentoAsync(
        string token, FormularioDocumentoEnvioDto dto)
    {
        var solicitacao = await CarregarPorTokenAsync(token);

        var tipo = (dto.Tipo ?? string.Empty).Trim().ToLowerInvariant();
        int? veiculoId = null;

        if (CatalogoFormulario.DocumentoDoVeiculo(tipo))
        {
            if (!solicitacao.VeiculoId.HasValue)
                throw new ArgumentException(
                    "Informe os dados do veículo antes de enviar o CRLV.");

            veiculoId = solicitacao.VeiculoId;
        }

        await _documentos.CriarAsync(new DocumentoCadastroDto
        {
            MotoristaId = solicitacao.MotoristaId,
            VeiculoId = veiculoId,
            SolicitacaoId = solicitacao.SolicitacaoId,
            TipoDocumento = tipo,
            Arquivo = dto.Arquivo
        });

        var atualizada = await _regras.CarregarAsync(solicitacao.SolicitacaoId) ?? solicitacao;

        return await MontarRespostaAsync(atualizada, token);
    }

    // ---------------------------------------------------------------
    // POST: envio final
    // ---------------------------------------------------------------
    public async Task<FormularioEnvioRespostaDto> EnviarAsync(string token)
    {
        var solicitacao = await CarregarPorTokenAsync(token);
        var avaliacao = await _regras.AvaliarAsync(solicitacao);

        var documentosPendentes = avaliacao.Documentos
            .Where(d => d.Obrigatorio && d.Estado == EstadoDocumento.Ausente)
            .Select(d => d.Chave)
            .ToList();

        if (avaliacao.CamposPendentes.Count > 0 || documentosPendentes.Count > 0)
        {
            return new FormularioEnvioRespostaDto
            {
                Status = solicitacao.Status,
                CamposPendentes = avaliacao.CamposPendentes.ToList(),
                DocumentosPendentes = documentosPendentes,
                Mensagem = "Preencha os itens obrigatórios antes de enviar."
            };
        }

        // Consome o link (uso único). Falha com UnauthorizedAccessException
        // se já foi usado, inclusive em envios simultâneos.
        await _links.ConsumirAsync(token);

        if (RegrasStatus.EnvioAlteraStatus(solicitacao.Status))
        {
            var novo = RegrasStatus.StatusAposEnvio(avaliacao);
            _regras.AplicarStatus(solicitacao, novo, null);
        }

        await _regras.RecalcularAsync(solicitacao);

        await _auditoria.RegistrarAsync(
            "Formulário enviado pelo motorista",
            $"Solicitação ID: {solicitacao.SolicitacaoId}. Status: {solicitacao.Status}.");

        return new FormularioEnvioRespostaDto
        {
            Status = solicitacao.Status,
            Mensagem = "Cadastro enviado. Nossa equipe fará a análise e entrará em contato."
        };
    }

    // ---------------------------------------------------------------
    private async Task<Solicitacao> CarregarPorTokenAsync(string token)
    {
        var id = await _links.ObterSolicitacaoIdValidaAsync(token);

        return await _regras.CarregarAsync(id)
            ?? throw new KeyNotFoundException("Solicitação não encontrada.");
    }

    private async Task<FormularioRespostaDto> MontarRespostaAsync(Solicitacao solicitacao, string token)
    {
        var validade = await _links.ValidarAsync(token);
        var configuracao = await _regras.ObterConfiguracaoAsync();
        var avaliacao = await _regras.AvaliarAsync(solicitacao);

        var veiculos = await _context.Veiculos
            .AsNoTracking()
            .Where(v => v.MotoristaId == solicitacao.MotoristaId && v.Ativo)
            .OrderBy(v => v.Placa)
            .Select(v => new FormularioVeiculoDto
            {
                VeiculoId = v.VeiculoId,
                Placa = v.Placa,
                CapacidadeToneladas = v.CapacidadeToneladas
            })
            .ToListAsync();

        return new FormularioRespostaDto
        {
            SolicitacaoId = solicitacao.SolicitacaoId,
            Status = solicitacao.Status,
            DataExpiracao = validade.DataExpiracao,
            Campos = configuracao
                .Where(c => c.Ativo && c.Categoria == CategoriaCampoFormulario.Campo)
                .OrderBy(c => c.CampoFormularioId)
                .Select(c => new FormularioCampoDto
                {
                    Chave = c.Chave,
                    Rotulo = c.Rotulo,
                    TipoCampo = c.TipoCampo,
                    Obrigatorio = c.Obrigatorio
                })
                .ToList(),
            Documentos = avaliacao.Documentos
                .Select(d => new FormularioDocumentoStatusDto
                {
                    Chave = d.Chave,
                    Rotulo = d.Rotulo,
                    Obrigatorio = d.Obrigatorio,
                    Estado = d.Estado.ToString(),
                    NomeOriginal = d.NomeOriginal,
                    DataEnvio = d.DataEnvio,
                    Reutilizado = d.Reutilizado,
                    DoVeiculo = CatalogoFormulario.DocumentoDoVeiculo(d.Chave)
                })
                .ToList(),
            Dados = new FormularioDadosAtuaisDto
            {
                Telefone = solicitacao.Motorista.Telefone,
                NomeCompleto = solicitacao.Motorista.NomeCompleto,
                Email = solicitacao.Motorista.Email,
                CpfMascarado = _cpf.Mascarar(solicitacao.Motorista.Cpf),
                VeiculoSelecionadoId = solicitacao.VeiculoId,
                Veiculos = veiculos
            }
        };
    }
}
