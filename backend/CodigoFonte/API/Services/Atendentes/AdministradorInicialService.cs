using Captacao.Api.Seguranca;
using Captacao.Api.Services.Auditoria;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Captacao.Api.Services.Atendentes;

// Cria o primeiro Administrador do sistema.
//
// Problema que resolve: o AtendenteService.CriarAsync sempre cria usuários
// com perfil "Atendente", e o cadastro de usuários exige um Administrador.
// Sem este serviço, não existe forma de obter o primeiro administrador
// pela API.
//
// Como funciona:
//  - roda uma vez na inicialização (chamado no Program.cs);
//  - se já existe um Administrador ATIVO, não faz nada;
//  - caso contrário, lê Administrador:Nome, Administrador:Email e
//    Administrador:Senha da configuração e cria o usuário.
//
// As credenciais devem vir de user-secrets (desenvolvimento) ou de
// variáveis de ambiente (produção), nunca do appsettings.json:
//
//   dotnet user-secrets set "Administrador:Nome"  "Seu Nome"
//   dotnet user-secrets set "Administrador:Email" "admin@empresa.com"
//   dotnet user-secrets set "Administrador:Senha" "uma-senha-longa-aqui"
//
// Em produção, as variáveis equivalentes são Administrador__Email etc.
// Depois do primeiro login, as três entradas podem ser removidas.
public class AdministradorInicialService
{
    private readonly CaptacaoDbContext _context;
    private readonly IPasswordHasher<Atendente> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly AuditoriaService _auditoriaService;
    private readonly ILogger<AdministradorInicialService> _logger;

    public AdministradorInicialService(
        CaptacaoDbContext context,
        IPasswordHasher<Atendente> passwordHasher,
        IConfiguration configuration,
        AuditoriaService auditoriaService,
        ILogger<AdministradorInicialService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    public async Task GarantirAsync()
    {
        try
        {
            // Já existe um administrador ativo: nada a fazer.
            // Verificar "ativo" evita ficar sem acesso caso o único
            // administrador tenha sido inativado.
            var existeAdministradorAtivo = await _context.Atendentes
                .AnyAsync(a =>
                    a.Perfil == Perfis.Administrador &&
                    a.Ativo);

            if (existeAdministradorAtivo)
            {
                return;
            }

            var nome = _configuration["Administrador:Nome"]?.Trim();
            var email = _configuration["Administrador:Email"]
                ?.Trim()
                .ToLowerInvariant();
            var senha = _configuration["Administrador:Senha"];

            if (string.IsNullOrWhiteSpace(nome) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(senha))
            {
                _logger.LogWarning(
                    "Não existe administrador ativo e Administrador:Nome, " +
                    "Administrador:Email e Administrador:Senha não estão " +
                    "configurados. O cadastro de usuários ficará " +
                    "indisponível até que um administrador seja criado.");

                return;
            }

            // Mesma regra de tamanho usada em AtendenteCadastroDto.
            if (senha.Length < 10 || senha.Length > 128)
            {
                _logger.LogError(
                    "Administrador:Senha deve possuir entre 10 e 128 " +
                    "caracteres. O administrador inicial não foi criado.");

                return;
            }

            // Se o e-mail já pertence a outro atendente, NÃO promovemos
            // essa conta automaticamente: transformar um usuário comum em
            // administrador por causa de uma configuração seria uma
            // elevação de privilégio silenciosa.
            var emailEmUso = await _context.Atendentes
                .AnyAsync(a => a.Email == email);

            if (emailEmUso)
            {
                _logger.LogError(
                    "O e-mail configurado em Administrador:Email já está " +
                    "em uso por um atendente que não é administrador ativo. " +
                    "Use outro e-mail ou ajuste o usuário manualmente.");

                return;
            }

            var administrador = new Atendente
            {
                Nome = nome,
                Email = email,
                Perfil = Perfis.Administrador,
                Ativo = true
            };

            // O hash usa o mesmo PasswordHasher do login, então o
            // AutenticacaoService consegue verificar esta senha normalmente.
            administrador.SenhaHash = _passwordHasher.HashPassword(
                administrador,
                senha);

            _context.Atendentes.Add(administrador);

            await _context.SaveChangesAsync();

            // A senha nunca é registrada, nem no log nem na auditoria.
            await _auditoriaService.RegistrarAsync(
                "Administrador inicial criado",
                $"Atendente ID: {administrador.AtendenteId}. " +
                $"E-mail: {administrador.Email}.");

            _logger.LogInformation(
                "Administrador inicial criado (ID {Id}).",
                administrador.AtendenteId);
        }
        catch (Exception ex)
        {
            // Falha aqui (por exemplo, o script do banco ainda não foi executado)
            // não deve impedir a API de subir. O erro fica no log.
            _logger.LogError(
                ex,
                "Não foi possível verificar ou criar o administrador inicial.");
        }
    }
}
