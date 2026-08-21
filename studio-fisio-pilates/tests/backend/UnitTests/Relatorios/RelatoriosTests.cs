using Clinica.Application.Features.Relatorios;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.UnitTests.Relatorios;

public class RelatoriosTests
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
        db.Profissionais.Add(new Profissional
        {
            Id = ProfissionalId,
            ClinicaId = Tenant,
            Nome = "Dr. Teste",
            CPF = "22222222222",
            RegistroProfissional = "CREFITO 1",
            SalarioBase = 4000m,
        });
        db.SaveChanges();
        return db;
    }

    private static Agendamento NovoAgendamento(DateTime inicio, StatusAgendamento status, decimal valor = 100m) =>
        new()
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            ProfissionalId = ProfissionalId,
            DataHoraInicio = inicio,
            DataHoraFim = inicio.AddMinutes(50),
            TipoSessao = TipoSessao.PilatesSolo,
            TipoAula = TipoAula.Plano,
            Status = status,
            ValorSessao = valor,
        };

    private static Mensalidade NovaMensalidade(
        string competencia, decimal valor, DateTime vencimento,
        StatusMensalidade status, DateTime? pagamento = null) =>
        new()
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            Competencia = competencia,
            Valor = valor,
            DataVencimento = vencimento,
            Status = status,
            DataPagamento = pagamento,
        };

    [Fact]
    public async Task Resumo_ConsolidaCartoesDoDashboard()
    {
        using var db = CriarDb();
        var hoje = DateTime.UtcNow.Date;

        db.Agendamentos.Add(NovoAgendamento(hoje.AddHours(9), StatusAgendamento.Confirmado));
        db.Agendamentos.Add(NovoAgendamento(hoje.AddHours(10), StatusAgendamento.Cancelado));
        db.Agendamentos.Add(NovoAgendamento(hoje.AddDays(-5), StatusAgendamento.Realizado));

        var mesAtual = hoje.ToString("yyyy-MM");
        db.Mensalidades.Add(NovaMensalidade(mesAtual, 300m, hoje.AddDays(-10), StatusMensalidade.Paga, hoje.AddDays(-3)));
        db.Mensalidades.Add(NovaMensalidade("2020-01", 250m, new DateTime(2020, 1, 10), StatusMensalidade.Atrasada));
        await db.SaveChangesAsync();

        var resposta = await new ResumoDashboardQueryHandler(db).Handle(new ResumoDashboardQuery(), CancellationToken.None);

        resposta.AgendamentosHoje.Should().Be(1, "cancelados não contam como agenda do dia");
        resposta.ReceitaMes.Should().Be(300m);
        resposta.Inadimplencia.Should().Be(250m);
        resposta.PacientesAtivos.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Faturamento_RetornaCompetenciasOrdenadasSemCanceladas()
    {
        using var db = CriarDb();
        var hoje = DateTime.UtcNow.Date;

        db.Mensalidades.Add(NovaMensalidade(hoje.AddMonths(-1).ToString("yyyy-MM"), 300m, hoje.AddMonths(-1), StatusMensalidade.Paga, hoje));
        db.Mensalidades.Add(NovaMensalidade(hoje.ToString("yyyy-MM"), 320m, hoje, StatusMensalidade.Pendente));
        db.Mensalidades.Add(NovaMensalidade(hoje.AddMonths(-2).ToString("yyyy-MM"), 999m, hoje.AddMonths(-2), StatusMensalidade.Cancelada));
        await db.SaveChangesAsync();

        var itens = await new FaturamentoQueryHandler(db).Handle(new FaturamentoQuery(3), CancellationToken.None);

        itens.Should().HaveCount(3);
        itens.Select(i => i.Competencia).Should().BeInAscendingOrder();
        var doisMesesAtras = itens[0];
        doisMesesAtras.Receita.Should().Be(0m, "a única mensalidade daquele mês está cancelada");
        doisMesesAtras.Previsto.Should().Be(0m);
        var mesAnterior = itens[1];
        mesAnterior.Receita.Should().Be(300m);
        mesAnterior.Previsto.Should().Be(300m);
        var mesAtual = itens[2];
        mesAtual.Receita.Should().Be(0m);
        mesAtual.Previsto.Should().Be(320m);
        itens.Sum(i => i.Previsto).Should().Be(620m, "canceladas ficam de fora do previsto");
    }

    [Fact]
    public async Task Ocupacao_AgrupaPorDiaIgnorandoCancelados()
    {
        using var db = CriarDb();
        var hoje = DateTime.UtcNow.Date;

        db.Agendamentos.Add(NovoAgendamento(hoje.AddHours(8), StatusAgendamento.Realizado));
        db.Agendamentos.Add(NovoAgendamento(hoje.AddHours(14), StatusAgendamento.Faltou));
        db.Agendamentos.Add(NovoAgendamento(hoje.AddHours(15), StatusAgendamento.Cancelado));
        db.Agendamentos.Add(NovoAgendamento(hoje.AddDays(-1).AddHours(9), StatusAgendamento.Agendado));
        await db.SaveChangesAsync();

        var dias = await new OcupacaoQueryHandler(db).Handle(new OcupacaoQuery(7), CancellationToken.None);

        dias.Should().HaveCount(7);
        var hoje_ = dias.Single(d => d.Data == hoje);
        hoje_.Total.Should().Be(2, "cancelados não contam no total");
        hoje_.Realizados.Should().Be(1);
        hoje_.Faltas.Should().Be(1);
        var ontem = dias.Single(d => d.Data == hoje.AddDays(-1));
        ontem.Total.Should().Be(1);
    }

    [Fact]
    public async Task TopSessoes_RanqueiaPorReceitaSomenteRealizadas()
    {
        using var db = CriarDb();
        var ontem = DateTime.UtcNow.Date.AddDays(-1);

        var fisio = NovoAgendamento(ontem.AddHours(8), StatusAgendamento.Realizado, 200m);
        fisio.TipoSessao = TipoSessao.Fisioterapia;
        var pilates = NovoAgendamento(ontem.AddHours(9), StatusAgendamento.Realizado, 150m);
        pilates.TipoSessao = TipoSessao.PilatesSolo;
        var cancelada = NovoAgendamento(ontem.AddHours(10), StatusAgendamento.Cancelado, 500m);
        cancelada.TipoSessao = TipoSessao.Domiciliar;
        db.Agendamentos.AddRange(fisio, pilates, cancelada);
        await db.SaveChangesAsync();

        var top = await new TopSessoesQueryHandler(db).Handle(new TopSessoesQuery(5), CancellationToken.None);

        top.Should().HaveCount(2);
        top[0].TipoSessao.Should().Be(nameof(TipoSessao.Fisioterapia));
        top[0].Receita.Should().Be(200m);
        top[1].TipoSessao.Should().Be(nameof(TipoSessao.PilatesSolo));
        top.Sum(t => t.Quantidade).Should().Be(2);
    }
}
