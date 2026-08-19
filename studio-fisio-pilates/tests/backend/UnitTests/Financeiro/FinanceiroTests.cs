using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Financeiro;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.UnitTests.Financeiro;

public class FinanceiroTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid PacienteId = Guid.NewGuid();

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
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GerarMensalidade_CriaComVencimentoNoDia10DaCompetencia()
    {
        using var db = CriarDb();
        var handler = new GerarMensalidadeCommandHandler(db);

        var id = await handler.Handle(new GerarMensalidadeCommand(PacienteId, "2026-08", 320m), CancellationToken.None);

        id.Should().NotBeEmpty();
        var mensalidade = await db.Mensalidades.SingleAsync();
        mensalidade.DataVencimento.Should().Be(new DateTime(2026, 8, 10));
        mensalidade.Status.Should().Be(StatusMensalidade.Pendente);
    }

    [Fact]
    public async Task GerarMensalidade_CompetenciaDuplicadaLancaBusinessRule()
    {
        using var db = CriarDb();
        await db.Mensalidades.AddAsync(new Mensalidade
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            Competencia = "2026-08",
            Valor = 320m,
            DataVencimento = new DateTime(2026, 8, 10),
        });
        await db.SaveChangesAsync();

        var handler = new GerarMensalidadeCommandHandler(db);
        var acao = async () => await handler.Handle(new GerarMensalidadeCommand(PacienteId, "2026-08", 320m), CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*já gerada*");
    }

    [Fact]
    public async Task RegistrarPagamento_AtualizaStatusParaPaga()
    {
        using var db = CriarDb();
        var mensalidade = new Mensalidade
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            Competencia = "2026-08",
            Valor = 320m,
            DataVencimento = new DateTime(2026, 8, 10),
        };
        await db.Mensalidades.AddAsync(mensalidade);
        await db.SaveChangesAsync();

        var handler = new RegistrarPagamentoMensalidadeCommandHandler(db);
        await handler.Handle(new RegistrarPagamentoMensalidadeCommand(mensalidade.Id), CancellationToken.None);

        var atualizado = await db.Mensalidades.SingleAsync();
        atualizado.Status.Should().Be(StatusMensalidade.Paga);
        atualizado.DataPagamento.Should().NotBeNull();
    }

    [Fact]
    public async Task RegistrarPagamento_DuploLancaBusinessRule()
    {
        using var db = CriarDb();
        var mensalidade = new Mensalidade
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            Competencia = "2026-08",
            Valor = 320m,
            DataVencimento = new DateTime(2026, 8, 10),
            Status = StatusMensalidade.Paga,
        };
        await db.Mensalidades.AddAsync(mensalidade);
        await db.SaveChangesAsync();

        var handler = new RegistrarPagamentoMensalidadeCommandHandler(db);
        var acao = async () => await handler.Handle(new RegistrarPagamentoMensalidadeCommand(mensalidade.Id), CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*já paga*");
    }

    [Fact]
    public async Task Dashboard_CalculaReceitaDespesaEResultadoDaCompetencia()
    {
        using var db = CriarDb();

        var paciente2 = Guid.NewGuid();
        var paciente3 = Guid.NewGuid();
        await db.Pacientes.AddRangeAsync(
            new Paciente { Id = paciente2, ClinicaId = Tenant, Nome = "P2", CPF = "22222222222" },
            new Paciente { Id = paciente3, ClinicaId = Tenant, Nome = "P3", CPF = "33333333333" });
        await db.SaveChangesAsync();

        await db.Mensalidades.AddRangeAsync(
            new Mensalidade { ClinicaId = Tenant, PacienteId = PacienteId, Competencia = "2026-08", Valor = 320m, DataVencimento = new DateTime(2026, 8, 10), Status = StatusMensalidade.Paga, DataPagamento = DateTime.UtcNow },
            new Mensalidade { ClinicaId = Tenant, PacienteId = paciente2, Competencia = "2026-08", Valor = 500m, DataVencimento = new DateTime(2026, 8, 10) },
            new Mensalidade { ClinicaId = Tenant, PacienteId = paciente3, Competencia = "2026-08", Valor = 400m, DataVencimento = new DateTime(2026, 8, 10), Status = StatusMensalidade.Atrasada });
        await db.ContasPagar.AddAsync(new ContaPagar
        {
            ClinicaId = Tenant,
            Fornecedor = "Aluguel",
            Descricao = "Aluguel agosto",
            Valor = 600m,
            DataVencimento = new DateTime(2026, 8, 5),
        });
        await db.SaveChangesAsync();

        var handler = new ObterDashboardQueryHandler(db);
        var dashboard = await handler.Handle(new ObterDashboardQuery("2026-08"), CancellationToken.None);

        dashboard.ReceitaMensal.Should().Be(1220m);
        dashboard.ReceitaRecebida.Should().Be(320m);
        dashboard.DespesaMensal.Should().Be(600m);
        dashboard.Resultado.Should().Be(-280m);
        dashboard.MensalidadesAtrasadas.Should().Be(1);
        dashboard.ContasAVencer.Should().HaveCount(1);
    }
}