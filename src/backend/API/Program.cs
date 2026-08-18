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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<TenantDbContext>());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Resolve X-Tenant-Id (header) e valida contra o claim tenant_id do JWT.
app.UseMiddleware<TenantHeaderMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;