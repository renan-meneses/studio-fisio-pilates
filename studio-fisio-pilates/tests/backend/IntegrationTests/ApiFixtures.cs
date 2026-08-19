using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using ClinicaEntity = Clinica.Domain.Entities.Clinica;
using Clinica.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Sobe um Postgres real (Testcontainers) e uma instância da API
/// (WebApplicationFactory) apontando para ele. Cada teste limpa o banco
/// via Respawn para garantir isolamento e determinismo.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("clinica_test")
        .WithUsername("clinica_test")
        .WithPassword("clinica_test")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["JWT:Key"] = "chave-de-teste-com-pelo-menos-32-bytes-0123456789",
                        ["JWT:Issuer"] = "clinica-test",
                        ["JWT:Audience"] = "clinica-test",
                    });
                });
            });

        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>Cria tenant (clínica) + usuário admin com senha conhecida.</summary>
    public async Task<SeedData> SeedClinicaAsync(string nome = "Clínica Teste")
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var senha = "Senha@123";

        var clinica = new ClinicaEntity
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            CNPJ = $"{Random.Shared.NextInt64(10000000000000, 99999999999999):D14}",
            Email = $"contato-{Guid.NewGuid():N}@teste.local",
            Plano = PlanoContratacao.Profissional,
        };

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            ClinicaId = clinica.Id,
            Nome = "Admin Teste",
            Email = $"admin-{Guid.NewGuid():N}@teste.local",
            SenhaHash = hasher.Hash(senha),
            Papel = PapelUsuario.Administrador,
        };

        var paciente = new Paciente
        {
            Id = Guid.NewGuid(),
            ClinicaId = clinica.Id,
            Nome = "Paciente Teste",
            CPF = $"{Random.Shared.NextInt64(10000000000, 99999999999):D11}",
            Telefone = "11988887777",
        };

        var profissional = new Profissional
        {
            Id = Guid.NewGuid(),
            ClinicaId = clinica.Id,
            Nome = "Dr. Teste",
            CPF = $"{Random.Shared.NextInt64(10000000000, 99999999999):D11}",
            RegistroProfissional = "CREFITO 00000-F",
            SalarioBase = 5000m,
        };

        await context.Clinicas.AddAsync(clinica);
        await context.Usuarios.AddAsync(usuario);
        await context.Pacientes.AddAsync(paciente);
        await context.Profissionais.AddAsync(profissional);
        await context.SaveChangesAsync();

        return new SeedData(clinica.Id, usuario.Email, senha, paciente.Id, profissional.Id);
    }
}

public sealed record SeedData(
    Guid ClinicaId,
    string Email,
    string Senha,
    Guid PacienteId,
    Guid ProfissionalId)
{
    public string TenantHeaderValue => ClinicaId.ToString();
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;