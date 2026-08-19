using System.Text.Json;
using Clinica.Application;
using Clinica.API.Middlewares;
using Clinica.CrossCutting;
using Clinica.Persistence;
using Clinica.Persistence.Initialization;
using Clinica.Application.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddCrossCutting(builder.Configuration);

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

    using var scope = app.Services.CreateScope();
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<TenantDbContext>());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("ClinicaWeb");

// Resolve X-Tenant-Id (header) e valida contra o claim tenant_id do JWT.
// Executado APÓS UseAuthentication para que o principal autenticado
// esteja disponível na validação de divergência de tenant.
app.UseMiddleware<TenantHeaderMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;