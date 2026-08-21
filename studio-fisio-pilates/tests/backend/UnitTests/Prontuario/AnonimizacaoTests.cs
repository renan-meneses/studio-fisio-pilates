using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Prontuario;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.UnitTests.Prontuario;

/// <summary>
/// LGPD: anonimização preserva integridade referencial (histórico continua
/// existindo) e é idempotente.
/// </summary>
public class AnonimizacaoTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static TenantDbContext CriarDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(Tenant).Object);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<Paciente> CriarPacienteComHistoricoAsync(TenantDbContext db)
    {
        var paciente = new Paciente
        {
            ClinicaId = Tenant,
            Nome = "Fulana de Tal",
            CPF = "11122233344",
            DataNascimento = new DateTime(1988, 7, 20),
            Telefone = "11977776666",
            Email = "fulana@email.com",
            Endereco = "Rua das Flores, 123",
        };
        await db.Pacientes.AddAsync(paciente);
        await db.SaveChangesAsync();
        return paciente;
    }

    [Fact]
    public async Task Anonimizar_remove_dados_pessoais_e_mantem_historico()
    {
        using var db = CriarDb();
        var paciente = await CriarPacienteComHistoricoAsync(db);

        // Histórico financeiro que DEVE sobreviver à anonimização.
        var mensalidade = new Mensalidade
        {
            ClinicaId = Tenant,
            PacienteId = paciente.Id,
            Competencia = "2026-08",
            Valor = 300m,
            DataVencimento = new DateTime(2026, 8, 10),
        };
        await db.Mensalidades.AddAsync(mensalidade);
        await db.SaveChangesAsync();

        await new AnonimizarPacienteCommandHandler(db).Handle(
            new AnonimizarPacienteCommand(paciente.Id), CancellationToken.None);

        var anonimo = await db.Pacientes.SingleAsync(p => p.Id == paciente.Id);
        anonimo.Nome.Should().Contain("LGPD");
        anonimo.CPF.Should().BeNull();
        anonimo.Telefone.Should().BeNull();
        anonimo.Email.Should().BeNull();
        anonimo.Endereco.Should().BeNull();
        anonimo.Status.Should().Be(StatusPaciente.Anonimizado);

        // Integridade referencial preservada.
        db.Mensalidades.Count(m => m.PacienteId == paciente.Id).Should().Be(1);
    }

    [Fact]
    public async Task Anonimizar_e_idempotente()
    {
        using var db = CriarDb();
        var paciente = await CriarPacienteComHistoricoAsync(db);
        var handler = new AnonimizarPacienteCommandHandler(db);

        await handler.Handle(new AnonimizarPacienteCommand(paciente.Id), CancellationToken.None);
        var segundaChamada = async () => await handler.Handle(
            new AnonimizarPacienteCommand(paciente.Id), CancellationToken.None);

        await segundaChamada.Should().NotThrowAsync();
        db.Pacientes.Count(p => p.Id == paciente.Id).Should().Be(1);
    }

    [Fact]
    public async Task Anonimizar_paciente_inexistente_not_found()
    {
        using var db = CriarDb();
        var handler = new AnonimizarPacienteCommandHandler(db);

        var ato = async () => await handler.Handle(
            new AnonimizarPacienteCommand(Guid.NewGuid()), CancellationToken.None);

        await ato.Should().ThrowAsync<NotFoundException>();
    }
}
