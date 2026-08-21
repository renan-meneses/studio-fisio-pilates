using Clinica.Application.Common.Interfaces;
using Clinica.CrossCutting.Auth;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using Clinica.Persistence.Initialization;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.UnitTests.Persistence;

/// <summary>
/// Bootstrap de administrador: criação idempotente fora do pipeline de
/// requisições (ignora filtro de tenant) e seed demo com login padrão.
/// </summary>
public class DatabaseInitializerTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly IPasswordHasher Hasher = new PasswordHasher();

    private static TenantDbContext CriarDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(Tenant).Object);
        db.Database.EnsureCreated();

        db.Clinicas.Add(new Domain.Entities.Clinica
        {
            Id = Tenant,
            Nome = "Clínica Teste",
            CNPJ = "00000000000100",
            Email = "teste@clinica.com",
        });
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task EnsureAdminUser_cria_administrador_com_senha_hasheada()
    {
        using var db = CriarDb();

        await DatabaseInitializer.EnsureAdminUserAsync(db, Hasher, new AdminBootstrapOptions
        {
            Nome = "Admin Teste",
            Email = "admin@clinica.com",
            Senha = "Senha@Forte123",
        });

        var admin = await db.Usuarios.IgnoreQueryFilters().SingleAsync();
        admin.Papel.Should().Be(PapelUsuario.Administrador);
        admin.Ativo.Should().BeTrue();
        admin.ClinicaId.Should().Be(Tenant);
        admin.SenhaHash.Should().NotBe("Senha@Forte123");
        Hasher.Verify("Senha@Forte123", admin.SenhaHash).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAdminUser_e_idempotente()
    {
        using var db = CriarDb();

        var options = new AdminBootstrapOptions { Email = "admin@clinica.com", Senha = "Senha@Forte123" };

        await DatabaseInitializer.EnsureAdminUserAsync(db, Hasher, options);
        await DatabaseInitializer.EnsureAdminUserAsync(db, Hasher, options);

        db.Usuarios.IgnoreQueryFilters().Count(u => u.Email == "admin@clinica.com")
            .Should().Be(1);
    }

    [Fact]
    public async Task EnsureAdminUser_sem_configuracao_nao_cria_nada()
    {
        using var db = CriarDb();

        await DatabaseInitializer.EnsureAdminUserAsync(db, Hasher, new AdminBootstrapOptions());

        db.Usuarios.IgnoreQueryFilters().Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureAdminUser_usa_clinica_especificada()
    {
        using var db = CriarDb();
        var outraClinica = Guid.NewGuid();

        await DatabaseInitializer.EnsureAdminUserAsync(db, Hasher, new AdminBootstrapOptions
        {
            Email = "admin@clinica.com",
            Senha = "Senha@Forte123",
            ClinicaId = outraClinica,
        });

        var admin = await db.Usuarios.IgnoreQueryFilters().SingleAsync();
        admin.ClinicaId.Should().Be(outraClinica);
    }

    [Fact]
    public async Task SeedDemo_cria_admin_padrao_da_clinica_demo()
    {
        using var db = CriarDb();

        // Mesmas credenciais do bloco de seed demo (dev): o par
        // email/senha constante precisa resultar em um login válido.
        await DatabaseInitializer.EnsureAdminUserAsync(db, Hasher, new AdminBootstrapOptions
        {
            Nome = "Administrador Demo",
            Email = DatabaseInitializer.DemoAdminEmail,
            Senha = DatabaseInitializer.DemoAdminSenha,
        });

        var admin = await db.Usuarios.IgnoreQueryFilters()
            .SingleAsync(u => u.Email == DatabaseInitializer.DemoAdminEmail);

        admin.Papel.Should().Be(PapelUsuario.Administrador);
        Hasher.Verify(DatabaseInitializer.DemoAdminSenha, admin.SenhaHash).Should().BeTrue();
    }
}
