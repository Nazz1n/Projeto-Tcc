using System.Text;
using Captacao.Api.Infra;
using Captacao.Api.Seguranca;
using Captacao.Api.Services.Atendentes;
using Captacao.Api.Services.Atendimentos;
using Captacao.Api.Services.Auditoria;
using Captacao.Api.Services.Autenticacao;
using Captacao.Api.Services.CamposFormulario;
using Captacao.Api.Services.Cargas;
using Captacao.Api.Services.Documentos;
using Captacao.Api.Services.Formulario;
using Captacao.Api.Services.LinksAcesso;
using Captacao.Api.Services.Motoristas;
using Captacao.Api.Services.Relatorios;
using Captacao.Api.Services.Solicitacoes;
using Captacao.Api.Services.Veiculos;
using Captacao.Domain.Entidades;
using Captacao.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// JWT
// =========================================================
var chaveJwt = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chave JWT não configurada.");

var emissorJwt = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Emissor JWT não configurado.");

var audienciaJwt = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Audiência JWT não configurada.");

var chaveBytes = Convert.FromBase64String(chaveJwt);

if (chaveBytes.Length < 32)
{
    throw new InvalidOperationException(
        "A chave JWT precisa possuir pelo menos 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = emissorJwt,

            ValidateAudience = true,
            ValidAudience = audienciaJwt,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(chaveBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // WebSocket do navegador não envia o header Authorization: o
        // SignalR manda o JWT em "?access_token=". Só é aceito nas rotas
        // do hub.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = contexto =>
            {
                var token = contexto.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(token) &&
                    contexto.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    contexto.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

// =========================================================
// AUTORIZAÇÃO
// =========================================================
// FallbackPolicy: política aplicada a QUALQUER endpoint que não tenha
// [Authorize] nem [AllowAnonymous]. Com ela, esquecer o atributo em um
// controller novo deixa de significar "endpoint público": o padrão passa
// a ser "exige usuário autenticado".
//
// Endpoints que precisam ser públicos devem declarar [AllowAnonymous]
// explicitamente (hoje: login, validação de link de acesso e a lista de
// campos ativos do formulário).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// =========================================================
// CORS
// =========================================================
// Sem CORS, o navegador bloqueia as chamadas do front-end (que roda em
// outra origem, ex.: https://localhost:5173) para esta API.
// As origens permitidas vêm da configuração, para poder variar entre
// desenvolvimento e produção. Autenticação é feita por header
// "Authorization: Bearer", portanto NÃO é necessário AllowCredentials.
var origensPermitidas = builder.Configuration
    .GetSection("Cors:OrigensPermitidas")
    .Get<string[]>() ?? Array.Empty<string>();

if (origensPermitidas.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:OrigensPermitidas não configurado.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", politica =>
    {
        politica
            .WithOrigins(origensPermitidas)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // O cliente SignalR (JS) negocia a conexão com credenciais;
            // é permitido porque as origens são uma lista explícita.
            .AllowCredentials();
    });
});

// =========================================================
// TRATAMENTO GLOBAL DE EXCEÇÕES
// =========================================================
builder.Services.AddExceptionHandler<ExcecaoGlobalHandler>();
builder.Services.AddProblemDetails();

// =========================================================
// PROTEÇÃO DE DADOS (chaves usadas para cifrar o CPF)
// =========================================================
// A pasta pode ser definida em "Armazenamento:PastaChaves". Sem
// configuração, mantém o caminho relativo anterior (para os CPFs já
// cifrados continuarem legíveis). Faça backup dessa pasta: sem as chaves,
// os CPFs cifrados não podem ser lidos.
var pastaChavesConfigurada = builder.Configuration["Armazenamento:PastaChaves"];

var pastaChaves = !string.IsNullOrWhiteSpace(pastaChavesConfigurada)
    ? Path.GetFullPath(pastaChavesConfigurada)
    : Path.GetFullPath(Path.Combine(
        builder.Environment.ContentRootPath,
        "..", "..", "..", "..",
        "dados-seguranca",
        "chaves-protecao"));

Directory.CreateDirectory(pastaChaves);

builder.Services
    .AddDataProtection()
    .SetApplicationName("CaptacaoCaminhoneiros")
    .PersistKeysToFileSystem(new DirectoryInfo(pastaChaves));

// =========================================================
// BANCO DE DADOS
// =========================================================
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A string de conexão DefaultConnection não foi configurada.");

builder.Services.AddDbContext<CaptacaoDbContext>(options =>
    options.UseMySQL(connectionString));

// =========================================================
// SERVIÇOS DA APLICAÇÃO
// =========================================================
builder.Services.AddScoped<MotoristaService>();
builder.Services.AddScoped<CpfProtecaoService>();
builder.Services.AddScoped<VeiculoService>();
builder.Services.AddScoped<ArmazenamentoDocumentoService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddScoped<SolicitacaoService>();
builder.Services.AddScoped<AtendenteService>();
builder.Services.AddScoped<AdministradorInicialService>();
builder.Services.AddScoped<
    IPasswordHasher<Atendente>,
    PasswordHasher<Atendente>>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<LinkAcessoService>();
builder.Services.AddScoped<CampoFormularioService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<CargaService>();

builder.Services.AddScoped<SolicitacaoRegrasService>();
builder.Services.AddScoped<FormularioService>();
builder.Services.AddScoped<RelatorioService>();

builder.Services.AddScoped<AtendimentoService>();

// Painel em tempo real (RF09/RNF05) via SignalR.
builder.Services.AddSignalR();
builder.Services.AddSingleton<IPainelNotificador, PainelNotificadorSignalR>();

// =========================================================
// LIMITE DE REQUISIÇÕES (endpoints públicos)
// =========================================================
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opcoes.AddPolicy("login", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    opcoes.AddPolicy("formulario", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// =========================================================
// API E DOCUMENTAÇÃO
// =========================================================
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Informe o token JWT recebido no login."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)]
                = new List<string>()
        });
});

var app = builder.Build();

// =========================================================
// ADMINISTRADOR INICIAL
// =========================================================
// Garante que exista ao menos um Administrador ativo. Só age quando não
// há nenhum e as credenciais estão configuradas (ver comentários em
// AdministradorInicialService). Erros aqui são registrados em log e não
// impedem a API de iniciar.
using (var escopo = app.Services.CreateScope())
{
    var administradorInicial = escopo.ServiceProvider
        .GetRequiredService<AdministradorInicialService>();

    await administradorInicial.GarantirAsync();

    // Campos e documentos predefinidos do formulário (RF05).
    try
    {
        await escopo.ServiceProvider
            .GetRequiredService<CampoFormularioService>()
            .GarantirPadraoAsync();
    }
    catch (Exception ex)
    {
        escopo.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Não foi possível garantir a configuração padrão do formulário.");
    }
}

// =========================================================
// PIPELINE HTTP (a ordem dos middlewares importa)
// =========================================================

// Deve ser o primeiro: captura exceções lançadas por tudo que vem depois.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Com a FallbackPolicy ativa, o endpoint do OpenAPI também exigiria
    // login. Em desenvolvimento ele é liberado explicitamente.
    // O Swagger UI e o swagger.json do Swashbuckle são middlewares, e não
    // endpoints, então não são afetados pela FallbackPolicy.
    app.MapOpenApi().AllowAnonymous();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS antes da autenticação: as requisições de "preflight" (OPTIONS) que
// o navegador envia não carregam token e precisam ser respondidas aqui,
// antes de a autenticação rejeitá-las.
app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PainelHub>("/hubs/painel");

// Verificação da conexão com o banco.
// Antes era anônima e expunha o nome do banco. Agora exige perfil
// Administrador. Se um balanceador ou serviço de monitoramento precisar
// chamá-la sem token, troque por .AllowAnonymous() e remova o campo
// "banco" da resposta.
app.MapGet("/api/health/database", async (CaptacaoDbContext db) =>
{
    var conectado = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        banco = "captacao_caminhoneiros",
        status = conectado ? "Conectado" : "Não conectado"
    });
})
.RequireAuthorization(politica =>
    politica.RequireRole(Perfis.Administrador));

app.Run();
