using Captacao.Api.DTOs.Motoristas;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Constantes;
using Captacao.Domain.Entidades;
using Captacao.Domain.Regras;
using Captacao.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Motoristas;

public class MotoristaService
{
    private readonly CaptacaoDbContext _context;
    private readonly CpfProtecaoService _cpfProtecaoService;
    private readonly AuditoriaService _auditoriaService;

    public MotoristaService(
        CaptacaoDbContext context,
        CpfProtecaoService cpfProtecaoService,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _cpfProtecaoService = cpfProtecaoService;
        _auditoriaService = auditoriaService;
    }

    // RF16: lista a base de motoristas. Cada contato aparece uma única vez
    // (o telefone é único) com os dados da solicitação mais recente. A busca
    // aceita nome, telefone ou placa.
    public Task<List<MotoristaRespostaDto>> ListarTodosAsync(string? busca = null) =>
        ListarAsync(busca, null);

    private async Task<List<MotoristaRespostaDto>> ListarAsync(string? busca, int? motoristaId)
    {
        var consulta = _context.Motoristas.AsNoTracking();

        if (motoristaId.HasValue)
        {
            consulta = consulta.Where(m => m.MotoristaId == motoristaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var digitos = FormatacaoDados.SomenteDigitos(termo);
            var placa = FormatacaoDados.NormalizarPlaca(termo);

            if (digitos.Length > 0)
            {
                consulta = consulta.Where(m =>
                    m.Telefone.Contains(digitos) ||
                    m.Veiculos.Any(v => v.Placa.Contains(placa)));
            }
            else
            {
                consulta = consulta.Where(m =>
                    (m.NomeCompleto != null && m.NomeCompleto.Contains(termo)) ||
                    m.Veiculos.Any(v => v.Placa.Contains(placa)));
            }
        }

        var motoristas = await consulta
            .OrderBy(m => m.NomeCompleto == null)
            .ThenBy(m => m.NomeCompleto)
            .ThenBy(m => m.Telefone)
            .ToListAsync();

        if (motoristas.Count == 0) return new List<MotoristaRespostaDto>();

        var ids = motoristas.Select(m => m.MotoristaId).ToList();

        var veiculos = await _context.Veiculos.AsNoTracking()
            .Where(v => ids.Contains(v.MotoristaId) && v.Ativo)
            .ToListAsync();

        var solicitacoes = await _context.Solicitacoes.AsNoTracking()
            .Where(s => ids.Contains(s.MotoristaId))
            .Select(s => new { s.MotoristaId, s.SolicitacaoId, s.Status, s.DataCriacao })
            .ToListAsync();

        return motoristas.Select(m =>
        {
            var veiculo = veiculos
                .Where(v => v.MotoristaId == m.MotoristaId)
                .OrderByDescending(v => v.VeiculoId)
                .FirstOrDefault();

            var ultima = solicitacoes
                .Where(s => s.MotoristaId == m.MotoristaId)
                .OrderByDescending(s => s.DataCriacao)
                .ThenByDescending(s => s.SolicitacaoId)
                .FirstOrDefault();

            var dto = Mapear(m);
            dto.Placa = veiculo?.Placa;
            dto.CapacidadeToneladas = veiculo?.CapacidadeToneladas;
            dto.UltimaSolicitacaoId = ultima?.SolicitacaoId;
            dto.StatusUltimaSolicitacao = ultima?.Status;
            dto.DataUltimaSolicitacao = ultima?.DataCriacao;
            return dto;
        }).ToList();
    }

    public async Task<MotoristaRespostaDto?> BuscarPorIdAsync(int id) =>
        (await ListarAsync(null, id)).FirstOrDefault();

    public async Task<MotoristaRespostaDto> CriarAsync(MotoristaCadastroDto dto)
    {
        var telefone = FormatacaoDados.NormalizarTelefone(dto.Telefone);

        if (!FormatacaoDados.TelefoneValido(telefone))
            throw new ArgumentException(
                "Informe um telefone válido com DDD (10 a 15 dígitos).");

        var telefoneExiste = await _context.Motoristas
            .AnyAsync(m => m.Telefone == telefone);

        if (telefoneExiste)
            throw new InvalidOperationException(
                "Já existe um motorista cadastrado com esse telefone.");

        string? cpfCriptografado = null;
        string? cpfHash = null;

        // O CPF é opcional, mas precisa ser válido quando informado.
        if (!string.IsNullOrWhiteSpace(dto.Cpf))
        {
            var cpfNormalizado = _cpfProtecaoService.Normalizar(dto.Cpf);

            if (!_cpfProtecaoService.Validar(cpfNormalizado))
                throw new ArgumentException("O CPF informado é inválido.");

            cpfHash = _cpfProtecaoService.GerarHash(cpfNormalizado);

            var cpfExiste = await _context.Motoristas
                .AnyAsync(m => m.CpfHash == cpfHash);

            if (cpfExiste)
                throw new InvalidOperationException(
                    "Já existe um motorista cadastrado com esse CPF.");

            cpfCriptografado = _cpfProtecaoService.Criptografar(cpfNormalizado);
        }

        var motorista = new Motorista
        {
            NomeCompleto = dto.NomeCompleto.Trim(),
            Telefone = telefone,
            Email = dto.Email?.Trim(),
            Rntrc = dto.Rntrc?.Trim(),
            Cpf = cpfCriptografado,
            CpfHash = cpfHash,
            DataCadastro = DateTime.UtcNow
        };

        _context.Motoristas.Add(motorista);

        await _context.SaveChangesAsync();

        // Sem CPF e sem dados desnecessários na auditoria.
        await _auditoriaService.RegistrarAsync(
            "Motorista criado",
            $"Motorista ID: {motorista.MotoristaId}.");

        return Mapear(motorista);
    }

    // Cadastro preliminar no primeiro contato (RF01): só o telefone é
    // conhecido; nome e CPF ficam nulos até o formulário.
    public async Task<(Motorista Motorista, bool Criado)> ObterOuCriarPorTelefoneAsync(
        string telefoneInformado)
    {
        var telefone = FormatacaoDados.NormalizarTelefone(telefoneInformado);

        if (!FormatacaoDados.TelefoneValido(telefone))
            throw new ArgumentException("Telefone inválido.");

        var existente = await _context.Motoristas
            .FirstOrDefaultAsync(m => m.Telefone == telefone);

        if (existente != null) return (existente, false);

        var motorista = new Motorista
        {
            Telefone = telefone,
            DataCadastro = DateTime.UtcNow
        };

        _context.Motoristas.Add(motorista);

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "Cadastro preliminar de motorista criado",
            $"Motorista ID: {motorista.MotoristaId}.");

        return (motorista, true);
    }

    private MotoristaRespostaDto Mapear(Motorista m) => new()
    {
        MotoristaId = m.MotoristaId,
        NomeCompleto = m.NomeCompleto,
        Telefone = m.Telefone,
        CpfMascarado = _cpfProtecaoService.Mascarar(m.Cpf),
        Email = m.Email,
        Rntrc = m.Rntrc,
        DataCadastro = m.DataCadastro
    };
}
