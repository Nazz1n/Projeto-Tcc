
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Captacao.Api.DTOs.Autenticacao;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Captacao.Api.Services.Autenticacao;

public class AutenticacaoService
{
    private readonly CaptacaoDbContext _context;
    private readonly IPasswordHasher<Atendente> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AutenticacaoService(
        CaptacaoDbContext context,
        IPasswordHasher<Atendente> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    // Realiza o login e gera o token JWT
    public async Task<TokenRespostaDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // Busca o atendente pelo e-mail
        var atendente = await _context.Atendentes
            .FirstOrDefaultAsync(a => a.Email == email);

        // Não permite login de usuários inexistentes ou inativos
        if (atendente == null || !atendente.Ativo)
        {
            throw new UnauthorizedAccessException(
                "E-mail ou senha inválidos.");
        }

        // Verifica a senha utilizando o hash armazenado
        var resultado = _passwordHasher.VerifyHashedPassword(
            atendente,
            atendente.SenhaHash,
            dto.Senha);

        if (resultado == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "E-mail ou senha inválidos.");
        }

        // Atualiza o hash caso o algoritmo recomende sua renovação
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            atendente.SenhaHash = _passwordHasher.HashPassword(
                atendente,
                dto.Senha);

            await _context.SaveChangesAsync();
        }

        return GerarToken(atendente);
    }

    // Gera o token JWT
    private TokenRespostaDto GerarToken(Atendente atendente)
    {
        var chaveConfigurada = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chave JWT não configurada.");

        var emissor = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Emissor JWT não configurado.");

        var audiencia = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Audiência JWT não configurada.");

        if (!int.TryParse(
            _configuration["Jwt:ExpirationMinutes"],
            out var minutos) || minutos <= 0)
        {
            throw new InvalidOperationException(
                "Validade do token JWT inválida.");
        }

        byte[] chaveBytes;

        try
        {
            chaveBytes = Convert.FromBase64String(chaveConfigurada);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "A chave JWT deve estar em formato Base64.");
        }

        var chave = new SymmetricSecurityKey(chaveBytes);

        var credenciais = new SigningCredentials(
            chave,
            SecurityAlgorithms.HmacSha256);

        var dataCriacao = DateTime.UtcNow;
        var dataExpiracao = dataCriacao.AddMinutes(minutos);

        // Informações que serão incluídas no token
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                atendente.AtendenteId.ToString()),

            new Claim(ClaimTypes.Name, atendente.Nome),

            new Claim(ClaimTypes.Email, atendente.Email),

            new Claim(ClaimTypes.Role, atendente.Perfil)
        };

        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: audiencia,
            claims: claims,
            notBefore: dataCriacao,
            expires: dataExpiracao,
            signingCredentials: credenciais);

        var tokenGerado = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new TokenRespostaDto
        {
            Token = tokenGerado,
            DataExpiracao = dataExpiracao,
            AtendenteId = atendente.AtendenteId,
            Nome = atendente.Nome,
            Email = atendente.Email,
            Perfil = atendente.Perfil
        };
    }
}
