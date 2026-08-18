using Clinica.Domain.Entities;
using Clinica.Persistence;
using Clinica.Persistence.Interceptors;
using Clinica.Persistence.QueryFilters;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Clinica.UnitTests.Tenancy;

/// <summary>
/// Testes de isolamento multitenant:
///  1. Global Query Filter filtra leitura por ClinicaId;
///  2. TenantSaveChangesInterceptor injeta ClinicaId em inserts;
///  3. Alterações não travam dados de outros tenants.
/// </summary>
public class TenantIsolationTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    /// <summary>
    /// Conexão SQLite in-memory própria por contexto: options distintas por
    /// tenant evitam o model cache compartilhado do EF Core (que fixaria o
    /// filtro do primeiro tenant em todos os contextos iguais).
    /// </summary>
    private static TenantDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite($"DataSource=tenant-{tenantId:N};Mode=Memory;Cache=Shared")
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(tenantId).Object);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task SeedPacientesAsync(TenantDbContext db, Guid tenantId, int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
        {
            await db.AddAsync(new Paciente
            {
                ClinicaId = tenantId,
                Nome = $"Paciente {tenantId:N} #{i}",
                CPF = $"0000000000{i}",
            });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GlobalQueryFilter_RetornaApenasRegistrosDoTenant()
    {
        using var dbA = CreateContext(TenantA);
        using var dbB = CreateContext(TenantB);
        await SeedPacientesAsync(dbA, TenantA, 3);
        await SeedPacientesAsync(dbB, TenantB, 2);

        var doTenantA = await dbA.Pacientes.ToListAsync();
        var doTenantB = await dbB.Pacientes.ToListAsync();

        doTenantA.Should().HaveCount(3).And.OnlyContain(p => p.ClinicaId == TenantA);
        doTenantB.Should().HaveCount(2).And.OnlyContain(p => p.ClinicaId == TenantB);
    }

    [Fact]
    public async Task Interceptor_InjetaClinicaIdEmEntidadesNovas()
    {
        using var db = CreateContext(TenantA);

        await db.AddAsync(new Paciente { Nome = "Sem Tenant Setado", CPF = "11111111111" });
        await db.SaveChangesAsync();

        var salvo = await db.Pacientes.SingleAsync();
        salvo.ClinicaId.Should().Be(TenantA);
    }

    [Fact]
    public async Task InsertsDeUmTenantNaoAparecemParaOutro()
    {
        using var dbA = CreateContext(TenantA);
        using var dbB = CreateContext(TenantB);

        await dbA.AddAsync(new Paciente { Nome = "Privado", CPF = "22222222222" });
        await dbA.SaveChangesAsync();

        dbB.Pacientes.Count().Should().Be(0);
    }

    [Fact]
    public async Task QueryFilter_NaoVazaViaIncludeDeAgendamento()
    {
        using var dbA = CreateContext(TenantA);
        using var dbB = CreateContext(TenantB);

        var pacienteA = new Paciente { ClinicaId = TenantA, Nome = "A", CPF = "33333333333" };
        var pacienteB = new Paciente { ClinicaId = TenantB, Nome = "B", CPF = "44444444444" };
        var profissionalA = new Profissional { ClinicaId = TenantA, Nome = "Prof A", CPF = "55555555555" };
        var profissionalB = new Profissional { ClinicaId = TenantB, Nome = "Prof B", CPF = "66666666666" };
        await dbA.AddAsync(pacienteA);
        await dbA.AddAsync(pacienteB);
        await dbA.AddAsync(profissionalA);
        await dbA.AddAsync(profissionalB);
        await dbA.SaveChangesAsync();

        await AdicionarAgendamentoAsync(dbA, pacienteA, profissionalA);
        await AdicionarAgendamentoAsync(dbA, pacienteB, profissionalB);
        await dbA.SaveChangesAsync();

        var agendaDoTenantA = await dbA.Agendamentos.Include(a => a.Paciente).ToListAsync();
        agendaDoTenantA.Should().HaveCount(1);
        agendaDoTenantA[0].Paciente!.Nome.Should().Be("A");
    }

    private static async Task AdicionarAgendamentoAsync(
        TenantDbContext db,
        Paciente paciente,
        Profissional profissional)
    {
        await db.Agendamentos.AddAsync(new Agendamento
        {
            ClinicaId = paciente.ClinicaId,
            PacienteId = paciente.Id,
            ProfissionalId = profissional.Id,
            DataHoraInicio = DateTime.UtcNow.AddHours(1),
            DataHoraFim = DateTime.UtcNow.AddHours(2),
        });
    }
}