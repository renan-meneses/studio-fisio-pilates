using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Agendamento;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TurmaEntity = Clinica.Domain.Entities.Turma;

namespace Clinica.UnitTests.Agenda;

/// <summary>
/// Promoção automática da fila de espera ao cancelar um agendamento de
/// turma: primeiro da fila entra na vaga; falha de regra preserva a fila.
/// </summary>
public class WaitlistPromocaoTests
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

    private static async Task<Paciente> CriarPacienteAsync(TenantDbContext db, string nome, string cpf, string email)
    {
        var paciente = new Paciente
        {
            ClinicaId = Tenant,
            Nome = nome,
            CPF = cpf,
            DataNascimento = new DateTime(1990, 1, 1),
            Email = email,
        };
        await db.Pacientes.AddAsync(paciente);
        await db.SaveChangesAsync();
        return paciente;
    }

    private static async Task<Profissional> CriarProfissionalAsync(TenantDbContext db, string cpf)
    {
        var profissional = new Profissional
        {
            ClinicaId = Tenant,
            Nome = "Dr. Fila",
            CPF = cpf,
            RegistroProfissional = "CREFITO 88888-F",
        };
        await db.Profissionais.AddAsync(profissional);
        await db.SaveChangesAsync();
        return profissional;
    }

    private static async Task<TurmaEntity> CriarTurmaAsync(TenantDbContext db, int capacidade)
    {
        var turma = new TurmaEntity
        {
            ClinicaId = Tenant,
            Nome = $"Turma fila {capacidade}",
            TipoSessao = TipoSessao.PilatesSolo,
            Capacidade = capacidade,
            Ativo = true,
        };
        await db.Turmas.AddAsync(turma);
        await db.SaveChangesAsync();
        return turma;
    }

    private static Agendamento AgendamentoNaJanela(
        Guid pacienteId, Guid profissionalId, Guid? turmaId,
        DateTime inicio, DateTime fim, StatusAgendamento status) => new()
        {
            ClinicaId = Tenant,
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            DataHoraInicio = inicio,
            DataHoraFim = fim,
            TipoSessao = TipoSessao.PilatesSolo,
            TipoAula = TipoAula.Plano,
            TurmaId = turmaId,
            Status = status,
            ValorSessao = 90m,
        };

    private static async Task<WaitlistEntry> EntrarNaFilaAsync(
        TenantDbContext db, Guid turmaId, Guid pacienteId)
    {
        var entrada = new WaitlistEntry
        {
            ClinicaId = Tenant,
            TurmaId = turmaId,
            PacienteId = pacienteId,
            Ativo = true,
        };
        await db.WaitlistEntries.AddAsync(entrada);
        await db.SaveChangesAsync();
        return entrada;
    }

    private static (DateTime inicio, DateTime fim) JanelaFutura() =>
        (DateTime.UtcNow.Date.AddDays(2).AddHours(9), DateTime.UtcNow.Date.AddDays(2).AddHours(10));

    [Fact]
    public async Task Cancelamento_promove_primeiro_da_fila_e_notifica()
    {
        using var db = CriarDb();
        var (inicio, fim) = JanelaFutura();
        var titular = await CriarPacienteAsync(db, "Titular", "11000000001", "titular@teste.local");
        var daFila = await CriarPacienteAsync(db, "Da Fila", "11000000002", "fila@teste.local");
        var profissional = await CriarProfissionalAsync(db, "99000000001");
        var turma = await CriarTurmaAsync(db, capacidade: 1);

        var agendamento = AgendamentoNaJanela(titular.Id, profissional.Id, turma.Id, inicio, fim, StatusAgendamento.Confirmado);
        await db.Agendamentos.AddAsync(agendamento);
        await db.SaveChangesAsync();
        var entrada = await EntrarNaFilaAsync(db, turma.Id, daFila.Id);

        var spy = new NotificacaoSpy();
        await new CancelarAgendamentoCommandHandler(db, spy)
            .Handle(new CancelarAgendamentoCommand(agendamento.Id, "Imprevisto do aluno"), CancellationToken.None);

        agendamento.Status.Should().Be(StatusAgendamento.Cancelado);

        var promovido = await db.Agendamentos
            .SingleAsync(a => a.PacienteId == daFila.Id);
        promovido.Status.Should().Be(StatusAgendamento.Agendado);
        promovido.DataHoraInicio.Should().Be(inicio);
        promovido.DataHoraFim.Should().Be(fim);
        promovido.TurmaId.Should().Be(turma.Id);
        promovido.Observacoes.Should().Contain("lista de espera");

        (await db.WaitlistEntries.SingleAsync(w => w.Id == entrada.Id)).Ativo.Should().BeFalse();

        spy.Mensagens.Should().HaveCount(1);
        spy.Mensagens[0].DestinatarioEmail.Should().Be("fila@teste.local");
        spy.Mensagens[0].Assunto.Should().Contain("Vaga");
    }

    [Fact]
    public async Task Conflito_do_profissional_preserva_fila_intacta()
    {
        using var db = CriarDb();
        var (inicio, fim) = JanelaFutura();
        var titular = await CriarPacienteAsync(db, "Titular", "12000000001", "t2@teste.local");
        var daFila = await CriarPacienteAsync(db, "Bloqueada", "12000000002", "b2@teste.local");
        var outroAluno = await CriarPacienteAsync(db, "Outro", "12000000003", "o2@teste.local");
        var profissional = await CriarProfissionalAsync(db, "99000000002");
        var turma = await CriarTurmaAsync(db, capacidade: 1);

        // Sessão individual do MESMO profissional sobreposta à janela:
        // a promoção do aluno da fila violaria a regra de sobreposição.
        var bloqueio = AgendamentoNaJanela(outroAluno.Id, profissional.Id, null, inicio.AddMinutes(30), fim.AddMinutes(30), StatusAgendamento.Confirmado);
        bloqueio.TipoAula = TipoAula.Individual;

        var agendamento = AgendamentoNaJanela(titular.Id, profissional.Id, turma.Id, inicio, fim, StatusAgendamento.Confirmado);
        await db.Agendamentos.AddRangeAsync(bloqueio, agendamento);
        await db.SaveChangesAsync();
        var entrada = await EntrarNaFilaAsync(db, turma.Id, daFila.Id);

        var spy = new NotificacaoSpy();
        await new CancelarAgendamentoCommandHandler(db, spy)
            .Handle(new CancelarAgendamentoCommand(agendamento.Id, "Imprevisto"), CancellationToken.None);

        agendamento.Status.Should().Be(StatusAgendamento.Cancelado);
        db.Agendamentos.Count(a => a.PacienteId == daFila.Id).Should().Be(0);
        (await db.WaitlistEntries.SingleAsync(w => w.Id == entrada.Id)).Ativo.Should().BeTrue();
        spy.Mensagens.Should().BeEmpty();
    }

    [Fact]
    public async Task Sem_fila_de_espera_cancela_sem_promover()
    {
        using var db = CriarDb();
        var (inicio, fim) = JanelaFutura();
        var titular = await CriarPacienteAsync(db, "Solitário", "13000000001", "s3@teste.local");
        var profissional = await CriarProfissionalAsync(db, "99000000003");
        var turma = await CriarTurmaAsync(db, capacidade: 3);

        var agendamento = AgendamentoNaJanela(titular.Id, profissional.Id, turma.Id, inicio, fim, StatusAgendamento.Agendado);
        await db.Agendamentos.AddAsync(agendamento);
        await db.SaveChangesAsync();

        var spy = new NotificacaoSpy();
        await new CancelarAgendamentoCommandHandler(db, spy)
            .Handle(new CancelarAgendamentoCommand(agendamento.Id, "Desistiu"), CancellationToken.None);

        agendamento.Status.Should().Be(StatusAgendamento.Cancelado);
        db.Agendamentos.Count(a => a.Id != agendamento.Id).Should().Be(0);
        spy.Mensagens.Should().BeEmpty();
    }

    [Fact]
    public async Task Promove_apenas_o_primeiro_mantendo_restantes_na_fila()
    {
        using var db = CriarDb();
        var (inicio, fim) = JanelaFutura();
        var titular = await CriarPacienteAsync(db, "Primeira Titular", "14000000001", "p4@teste.local");
        var segundo = await CriarPacienteAsync(db, "Segundo", "14000000002", "s4@teste.local");
        var terceiro = await CriarPacienteAsync(db, "Terceiro", "14000000003", "t4@teste.local");
        var profissional = await CriarProfissionalAsync(db, "99000000004");
        var turma = await CriarTurmaAsync(db, capacidade: 1);

        var agendamento = AgendamentoNaJanela(titular.Id, profissional.Id, turma.Id, inicio, fim, StatusAgendamento.Confirmado);
        await db.Agendamentos.AddAsync(agendamento);
        await db.SaveChangesAsync();
        var entradaSegunda = await EntrarNaFilaAsync(db, turma.Id, segundo.Id);
        var entradaTerceira = await EntrarNaFilaAsync(db, turma.Id, terceiro.Id);

        var spy = new NotificacaoSpy();
        await new CancelarAgendamentoCommandHandler(db, spy)
            .Handle(new CancelarAgendamentoCommand(agendamento.Id, "Cancelou"), CancellationToken.None);

        (await db.Agendamentos.SingleAsync(a => a.PacienteId == segundo.Id)).Status
            .Should().Be(StatusAgendamento.Agendado);
        db.Agendamentos.Count(a => a.PacienteId == terceiro.Id).Should().Be(0);
        (await db.WaitlistEntries.SingleAsync(w => w.Id == entradaSegunda.Id)).Ativo.Should().BeFalse();
        (await db.WaitlistEntries.SingleAsync(w => w.Id == entradaTerceira.Id)).Ativo.Should().BeTrue();
        spy.Mensagens.Select(m => m.DestinatarioEmail).Should().ContainSingle().Which.Should().Be("s4@teste.local");
    }
}
