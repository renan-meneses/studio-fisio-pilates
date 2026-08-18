using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Agendamento;
using Clinica.Application.Common.Exceptions;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Clinica.UnitTests.Agenda;

/// <summary>Regras de negócio da agenda: sobreposição, janela e estados.</summary>
public class AgendamentoRulesTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid PacienteId = Guid.NewGuid();
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    private static TenantDbContext CriarDb(params Domain.Entities.Agendamento[] registros)
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
        db.Agendamentos.AddRange(registros);
        db.SaveChanges();
        return db;
    }

    private static CriarAgendamentoCommand Comando(DateTime inicio, DateTime fim) =>
        new(PacienteId, ProfissionalId, inicio, fim, TipoSessao.Fisioterapia);

    [Fact]
    public async Task Criar_FalhaQuandoJanelaInvalida()
    {
        var handler = new CriarAgendamentoCommandHandler(CriarDb());

        var acao = async () => await handler.Handle(Comando(new DateTime(2026, 8, 20, 10, 0, 0), new DateTime(2026, 8, 20, 9, 0, 0)), CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*janela de horário é inválida*");
    }

    [Fact]
    public async Task Criar_FalhaQuandoProfissionalJaTemSessaoNoPeriodo()
    {
        var existente = new Domain.Entities.Agendamento
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            ProfissionalId = ProfissionalId,
            DataHoraInicio = new DateTime(2026, 8, 20, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 8, 20, 11, 0, 0),
            Status = StatusAgendamento.Confirmado,
        };

        var handler = new CriarAgendamentoCommandHandler(CriarDb(existente));

        var acao = async () => await handler.Handle(
            Comando(new DateTime(2026, 8, 20, 10, 30, 0), new DateTime(2026, 8, 20, 11, 30, 0)),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Profissional já possui sessão no período informado.");
    }

    [Fact]
    public async Task Criar_PermiteSessaoSequencialSemSobreposicao()
    {
        var existente = new Domain.Entities.Agendamento
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            ProfissionalId = ProfissionalId,
            DataHoraInicio = new DateTime(2026, 8, 20, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 8, 20, 11, 0, 0),
        };

        using var db = (TenantDbContext)CriarDb(existente);
        var handler = new CriarAgendamentoCommandHandler(db);

        var id = await handler.Handle(
            Comando(new DateTime(2026, 8, 20, 11, 0, 0), new DateTime(2026, 8, 20, 12, 0, 0)),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        db.Agendamentos.Count().Should().Be(2);
    }

    [Fact]
    public async Task Criar_IgnoraSessaoCanceladaNaChecagemDeSobreposicao()
    {
        var cancelado = new Domain.Entities.Agendamento
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            ProfissionalId = ProfissionalId,
            DataHoraInicio = new DateTime(2026, 8, 20, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 8, 20, 11, 0, 0),
            Status = StatusAgendamento.Cancelado,
        };

        var handler = new CriarAgendamentoCommandHandler(CriarDb(cancelado));

        var id = await handler.Handle(
            Comando(new DateTime(2026, 8, 20, 10, 30, 0), new DateTime(2026, 8, 20, 11, 30, 0)),
            CancellationToken.None);

        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Cancelar_LancaQuandoSessaoJaRealizada()
    {
        using var db = (TenantDbContext)CriarDb(new Domain.Entities.Agendamento
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            ProfissionalId = ProfissionalId,
            DataHoraInicio = new DateTime(2026, 8, 20, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 8, 20, 11, 0, 0),
            Status = StatusAgendamento.Realizado,
        });

        var handler = new CancelarAgendamentoCommandHandler(db);

        var agendamento = db.Agendamentos.Single();
        var acao = async () => await handler.Handle(
            new CancelarAgendamentoCommand(agendamento.Id, "Teste"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pode ser cancelado*");
    }

    [Fact]
    public async Task RegistrarPresenca_MarcaPresenteEEvoluiAgendamento()
    {
        using var db = (TenantDbContext)CriarDb(new Domain.Entities.Agendamento
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            ProfissionalId = ProfissionalId,
            DataHoraInicio = new DateTime(2026, 8, 20, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 8, 20, 11, 0, 0),
            Status = StatusAgendamento.Confirmado,
        });

        var handler = new RegistrarPresencaCommandHandler(db);
        var agendamento = db.Agendamentos.Single();

        var presencaId = await handler.Handle(
            new RegistrarPresencaCommand(agendamento.Id, StatusAgendamento.Realizado),
            CancellationToken.None);

        presencaId.Should().NotBeEmpty();
        var atualizado = db.Agendamentos.Include(a => a.Presenca).Single();
        atualizado.Status.Should().Be(StatusAgendamento.Realizado);
        atualizado.Presenca!.Status.Should().Be(StatusPresenca.Presente);
    }
}