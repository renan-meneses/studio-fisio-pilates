using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Rh;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.UnitTests.Rh;

public class RhTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
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

        db.Profissionais.Add(new Profissional
        {
            Id = ProfissionalId,
            ClinicaId = Tenant,
            Nome = "Profissional",
            CPF = "22222222222",
            SalarioBase = 4500m,
        });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task RegistrarPonto_SalvaPontoComHorasTrabalhadas()
    {
        using var db = CriarDb();
        var handler = new RegistrarPontoCommandHandler(db);

        var id = await handler.Handle(
            new RegistrarPontoCommand(
                ProfissionalId,
                new DateTime(2026, 8, 17),
                new TimeSpan(8, 0, 0),
                new TimeSpan(17, 0, 0),
                new TimeSpan(12, 0, 0),
                new TimeSpan(13, 0, 0),
                null),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        var ponto = await db.Pontos.SingleAsync();
        ponto.HorasTrabalhadas().Should().Be(new TimeSpan(8, 0, 0));
    }

    [Fact]
    public async Task RegistrarPonto_HorarioInvalidoLancaBusinessRule()
    {
        using var db = CriarDb();
        var handler = new RegistrarPontoCommandHandler(db);

        var acao = async () => await handler.Handle(
            new RegistrarPontoCommand(
                ProfissionalId,
                new DateTime(2026, 8, 17),
                new TimeSpan(17, 0, 0),
                new TimeSpan(8, 0, 0),
                null,
                null,
                null),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*posterior à entrada*");
    }

    [Fact]
    public async Task CalcularFolha_ProcessaValorBrutoComDescontos()
    {
        using var db = CriarDb();
        await db.Pontos.AddAsync(new Ponto
        {
            ClinicaId = Tenant,
            ProfissionalId = ProfissionalId,
            Data = new DateTime(2026, 8, 3),
            Entrada = new TimeSpan(8, 0, 0),
            Saida = new TimeSpan(17, 0, 0),
        });
        await db.Pontos.AddAsync(new Ponto
        {
            ClinicaId = Tenant,
            ProfissionalId = ProfissionalId,
            Data = new DateTime(2026, 8, 4),
            Entrada = new TimeSpan(8, 0, 0),
            Saida = new TimeSpan(17, 0, 0),
        });
        await db.SaveChangesAsync();

        var handler = new CalcularFolhaCommandHandler(db);
        var id = await handler.Handle(
            new CalcularFolhaCommand(ProfissionalId, "2026-08", 500m),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        var folha = await db.FolhasSalariais.SingleAsync();
        folha.ValorBruto.Should().Be(4500m);
        folha.Descontos.Should().Be(500m);
        folha.ValorLiquido.Should().Be(4000m);
        folha.DiasTrabalhados.Should().Be(2);
        folha.Status.Should().Be(StatusFolha.Processada);
    }

    [Fact]
    public async Task CalcularFolha_FolhaDuplicadaLancaBusinessRule()
    {
        using var db = CriarDb();
        await db.FolhasSalariais.AddAsync(new FolhaSalarial
        {
            ClinicaId = Tenant,
            ProfissionalId = ProfissionalId,
            Competencia = "2026-08",
            ValorBruto = 4500m,
            Descontos = 0,
            Status = StatusFolha.Rascunho,
        });
        await db.SaveChangesAsync();

        var handler = new CalcularFolhaCommandHandler(db);
        var acao = async () => await handler.Handle(
            new CalcularFolhaCommand(ProfissionalId, "2026-08", 0m),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*já calculada*");
    }

    [Fact]
    public async Task CalcularFolha_DescontoAcimaDoBrutoLancaBusinessRule()
    {
        using var db = CriarDb();
        var handler = new CalcularFolhaCommandHandler(db);

        var acao = async () => await handler.Handle(
            new CalcularFolhaCommand(ProfissionalId, "2026-08", 9000m),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Descontos inválidos*");
    }
}