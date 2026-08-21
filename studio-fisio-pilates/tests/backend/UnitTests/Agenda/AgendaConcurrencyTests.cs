using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Agendamento;
using Clinica.Application.Features.Turma;
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
/// Regras de agenda com concorrência: capacidade de turma por horário
/// (ocupante existente não conta contra si) e fila de espera idempotente.
/// O lock advisory é no-op em SQLite — coberto nos testes de integração.
/// </summary>
public class AgendaConcurrencyTests
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

    private static async Task<TurmaEntity> CriarTurmaAsync(
        TenantDbContext db, int capacidade)
    {
        var turma = new TurmaEntity
        {
            ClinicaId = Tenant,
            Nome = $"Turma {capacidade} vagas",
            TipoSessao = TipoSessao.PilatesSolo,
            Capacidade = capacidade,
            Ativo = true,
        };
        await db.Turmas.AddAsync(turma);
        await db.SaveChangesAsync();
        return turma;
    }

    private static async Task<Paciente> CriarPacienteAsync(
        TenantDbContext db, string nome, string cpf)
    {
        var paciente = new Paciente
        {
            ClinicaId = Tenant,
            Nome = nome,
            CPF = cpf,
            DataNascimento = new DateTime(1990, 1, 1),
        };
        await db.Pacientes.AddAsync(paciente);
        await db.SaveChangesAsync();
        return paciente;
    }

    private static async Task<Profissional> CriarProfissionalAsync(
        TenantDbContext db)
    {
        var profissional = new Profissional
        {
            ClinicaId = Tenant,
            Nome = "Dr. Agenda",
            CPF = "99000000001",
            RegistroProfissional = "CREFITO 99999-F",
        };
        await db.Profissionais.AddAsync(profissional);
        await db.SaveChangesAsync();
        return profissional;
    }

    private static Agendamento AgendamentoAtivo(
        Guid pacienteId, Guid profissionalId, Guid turmaId,
        DateTime inicio, DateTime fim) => new()
    {
        ClinicaId = Tenant,
        PacienteId = pacienteId,
        ProfissionalId = profissionalId,
        DataHoraInicio = inicio,
        DataHoraFim = fim,
        TipoSessao = TipoSessao.PilatesSolo,
        TipoAula = TipoAula.Plano,
        TurmaId = turmaId,
        Status = StatusAgendamento.Confirmado,
    };

    [Fact]
    public async Task Turma_cheia_rejeita_novo_aluno_no_horario()
    {
        using var db = CriarDb();
        var turma = await CriarTurmaAsync(db, capacidade: 2);
        var profissional = await CriarProfissionalAsync(db);
        var profissionalId = profissional.Id;
        var inicio = new DateTime(2026, 9, 1, 18, 0, 0);
        var fim = inicio.AddHours(1);

        for (var i = 0; i < 2; i++)
        {
            var p = await CriarPacienteAsync(db, $"Ocupante {i}", $"1000000000{i}");
            await db.Agendamentos.AddAsync(
                AgendamentoAtivo(p.Id, profissionalId, turma.Id, inicio, fim));
        }
        await db.SaveChangesAsync();

        var terceiro = await CriarPacienteAsync(db, "Terceiro", "30000000003");
        var handler = new CriarAgendamentoCommandHandler(db);

        var ato = async () => await handler.Handle(new CriarAgendamentoCommand(
            terceiro.Id, profissionalId, inicio, fim, TipoSessao.PilatesSolo,
            TipoAula.Plano, turma.Id), CancellationToken.None);

        var excecao = await ato.Should().ThrowAsync<BusinessRuleException>();
        excecao.Which.Message.Should().Contain("cheia");
    }

    [Fact]
    public async Task Ocupante_existente_pode_atualizar_o_proprio_horario_na_turma()
    {
        using var db = CriarDb();
        var turma = await CriarTurmaAsync(db, capacidade: 1);
        var profissional = await CriarProfissionalAsync(db);
        var profissionalId = profissional.Id;
        var inicio = new DateTime(2026, 9, 2, 7, 0, 0);

        var ocupante = await CriarPacienteAsync(db, "Ocupante Único", "41000000000");
        var agendamento = AgendamentoAtivo(
            ocupante.Id, profissionalId, turma.Id, inicio, inicio.AddHours(1));
        await db.Agendamentos.AddAsync(agendamento);
        await db.SaveChangesAsync();

        // Remarca o próprio agendamento para outro dia: não deve esbarrar
        // na capacidade dele mesmo.
        var handler = new AtualizarAgendamentoCommandHandler(db);
        var novaJanela = inicio.AddDays(7);

        var id = await handler.Handle(new AtualizarAgendamentoCommand(
            agendamento.Id, ocupante.Id, profissionalId, novaJanela, novaJanela.AddHours(1),
            TipoSessao.PilatesSolo, TipoAula.Plano, turma.Id), CancellationToken.None);

        id.Should().Be(agendamento.Id);
    }

    [Fact]
    public async Task Waitlist_entrada_idempotente_e_ordenada_por_chegada()
    {
        using var db = CriarDb();
        var turma = await CriarTurmaAsync(db, capacidade: 2);
        var entrar = new EntrarWaitlistCommandHandler(db);
        var listar = new ListarWaitlistQueryHandler(db);

        var p1 = await CriarPacienteAsync(db, "Primeiro da Fila", "51000000000");
        var p2 = await CriarPacienteAsync(db, "Segundo da Fila", "52000000000");

        var entrada1 = await entrar.Handle(
            new EntrarWaitlistCommand(turma.Id, p1.Id), CancellationToken.None);

        // Reentrada do mesmo paciente: devolve a entrada existente.
        var reentrada = await entrar.Handle(
            new EntrarWaitlistCommand(turma.Id, p1.Id), CancellationToken.None);
        reentrada.Should().Be(entrada1);

        _ = await entrar.Handle(
            new EntrarWaitlistCommand(turma.Id, p2.Id), CancellationToken.None);

        var fila = await listar.Handle(
            new ListarWaitlistQuery(turma.Id), CancellationToken.None);
        fila.Should().HaveCount(2);
        fila[0].PacienteId.Should().Be(p1.Id);
        fila[1].PacienteId.Should().Be(p2.Id);

        // Saída remove da fila; reentrada cria posição ao final.
        await new SairWaitlistCommandHandler(db).Handle(
            new SairWaitlistCommand(entrada1), CancellationToken.None);
        var aposSaida = await listar.Handle(
            new ListarWaitlistQuery(turma.Id), CancellationToken.None);
        aposSaida.Select(e => e.PacienteId).Should().NotContain(p1.Id);

        var volta = await entrar.Handle(
            new EntrarWaitlistCommand(turma.Id, p1.Id), CancellationToken.None);
        volta.Should().NotBe(entrada1);
    }
}
