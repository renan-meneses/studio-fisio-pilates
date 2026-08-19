using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Prontuario;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Clinica.UnitTests.Prontuario;

public class ProntuarioTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid PacienteId = Guid.NewGuid();
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    private static TenantDbContext CriarDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(Tenant).Object);
        db.Database.EnsureCreated();

        db.Pacientes.Add(new Paciente { Id = PacienteId, ClinicaId = Tenant, Nome = "Paciente", CPF = "11111111111" });
        db.Profissionais.Add(new Profissional { Id = ProfissionalId, ClinicaId = Tenant, Nome = "Profissional", CPF = "22222222222" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Abrir_CriaProntuarioParaPacienteSemProntuario()
    {
        using var db = CriarDb();
        var handler = new AbrirProntuarioCommandHandler(db);

        var id = await handler.Handle(new AbrirProntuarioCommand(PacienteId), CancellationToken.None);

        id.Should().NotBeEmpty();
        var prontuario = await db.Prontuarios.SingleAsync();
        prontuario.PacienteId.Should().Be(PacienteId);
        prontuario.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Abrir_DuplicadoLancaBusinessRule()
    {
        using var db = CriarDb();
        await db.Prontuarios.AddAsync(new ProntuarioEletronico { ClinicaId = Tenant, PacienteId = PacienteId });
        await db.SaveChangesAsync();

        var handler = new AbrirProntuarioCommandHandler(db);
        var acao = async () => await handler.Handle(new AbrirProntuarioCommand(PacienteId), CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*já possui prontuário ativo*");
    }

    [Fact]
    public async Task AdicionarEvolucao_RegistraEvolucaoEListaNaQuery()
    {
        using var db = CriarDb();
        var prontuario = new ProntuarioEletronico { ClinicaId = Tenant, PacienteId = PacienteId };
        await db.Prontuarios.AddAsync(prontuario);
        await db.SaveChangesAsync();

        var handler = new AdicionarEvolucaoCommandHandler(db);
        var evolucaoId = await handler.Handle(
            new AdicionarEvolucaoCommand(
                prontuario.Id,
                ProfissionalId,
                TipoEvolucao.Evolucao,
                "Dor lombar",
                "Escore de dor 7/10",
                "Pilates clínico 2x/semana",
                null),
            CancellationToken.None);

        evolucaoId.Should().NotBeEmpty();

        var query = new ObterProntuarioPorPacienteQueryHandler(db);
        var resultado = await query.Handle(new ObterProntuarioPorPacienteQuery(PacienteId), CancellationToken.None);

        resultado.TotalEvolucoes.Should().Be(1);
        resultado.Evolucoes[0].Conduta.Should().Be("Pilates clínico 2x/semana");
        resultado.Evolucoes[0].ProfissionalNome.Should().Be("Profissional");
    }

    [Fact]
    public async Task AdicionarEvolucao_ProntuarioInexistenteLancaNotFound()
    {
        using var db = CriarDb();
        var handler = new AdicionarEvolucaoCommandHandler(db);

        var acao = async () => await handler.Handle(
            new AdicionarEvolucaoCommand(Guid.NewGuid(), ProfissionalId, TipoEvolucao.Evolucao, null, null, "Conduta", null),
            CancellationToken.None);

        await acao.Should().ThrowAsync<NotFoundException>();
    }
}