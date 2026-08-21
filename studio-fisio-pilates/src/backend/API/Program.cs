using System.Text.Json;
using Clinica.Application;
using Clinica.API.Controllers;
using Clinica.API.Middlewares;
using Clinica.API.Telemetry;
using Clinica.CrossCutting;
using Clinica.Persistence;
using Clinica.Persistence.Initialization;
using Clinica.Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddCrossCutting(builder.Configuration);

builder.Services.Configure<WebhookOptions>(
    builder.Configuration.GetSection(WebhookOptions.SectionName));

builder.Services.AddSingleton<ApiMetrics>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                allowIntegerValues: true));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClinicaWeb", policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "Authorization", "X-Tenant-Id")
            .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Roda em dev (seed demo) ou quando AdminBootstrap está configurado
// (criação idempotente de admin em qualquer ambiente, inclusive prod).
var adminBootstrap = builder.Configuration
    .GetSection(AdminBootstrapOptions.SectionName)
    .Get<AdminBootstrapOptions>() ?? new();

if (app.Environment.IsDevelopment() || adminBootstrap.Configurado)
{
    using var scope = app.Services.CreateScope();
    await DatabaseInitializer.InitializeAsync(
        scope.ServiceProvider.GetRequiredService<TenantDbContext>(),
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
        adminBootstrap);
}

// Correlation ID distribuído: primeiro de tudo (cobre inclusive erros).
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("ClinicaWeb");

// Resolve X-Tenant-Id (header) e valida contra o claim tenant_id do JWT.
// Executado APÓS UseAuthentication para que o principal autenticado
// esteja disponível na validação de divergência de tenant.
app.UseMiddleware<TenantHeaderMiddleware>();

// Idempotência genérica de POST via header Idempotency-Key (se presente).
// Após o tenant (chave de escopo por tenant) e antes do log do request,
// para que replays também sejam registrados.
app.UseMiddleware<IdempotencyMiddleware>();

// Log estruturado da requisição com scope de tenant/usuário (após a
// resolução do tenant, para que o escopo carregue o tenant ativo).
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;