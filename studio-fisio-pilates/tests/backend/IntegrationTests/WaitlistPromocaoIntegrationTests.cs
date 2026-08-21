using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TurmaEntity = Clinica.Domain.Entities.Turma;

namespace Clinica.IntegrationTests;

/// <summary>
/// Ponta a ponta: cancelamento via API promove o primeiro da fila de espera
/// da turma para o horário liberado e desativa a entrada da fila.
/// </summary>
[Collection("api")]
public sealed class WaitlistPromocaoIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public WaitlistPromocaoIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Cancelamento_via_api_promove_aluno_da_fila()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var (turmaId, agendamentoId, pacienteFilaId) = await SemearCenarioAsync(seed);
        var client = await AutenticarAsync(seed);

        var resposta = await client.PatchAsJsonAsync(
            $"/api/agendamentos/{agendamentoId}/cancelar",
            new { motivo = "Lesão do aluno titular" });
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Clinica.Persistence.TenantDbContext>();

        // Fora do pipeline HTTP não há tenant no contexto: ignorar o filtro
        // global (as entidades têm ClinicaId explícito do seed).
        var promovido = await db.Agendamentos
            .IgnoreQueryFilters()
            .SingleAsync(a => a.PacienteId == pacienteFilaId);
        promovido.Status.Should().Be(StatusAgendamento.Agendado);
        promovido.TurmaId.Should().Be(turmaId);

        var entrada = await db.WaitlistEntries
            .IgnoreQueryFilters()
            .SingleAsync(w => w.PacienteId == pacienteFilaId);
        entrada.Ativo.Should().BeFalse();

        // Listagem da agenda reflete os dois estados.
        var agenda = await client.GetFromJsonAsync<JsonElement>("/api/agendamentos");
        agenda.GetArrayLength().Should().Be(2);
    }

    private async Task<(Guid TurmaId, Guid AgendamentoId, Guid PacienteFilaId)> SemearCenarioAsync(SeedData seed)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Clinica.Persistence.TenantDbContext>();

        var alunoFila = new Paciente
        {
            ClinicaId = seed.ClinicaId,
            Nome = "Aluno da Fila",
            CPF = $"{Random.Shared.NextInt64(10000000000, 99999999999):D11}",
            Email = "fila@integracao.local",
        };
        await db.Pacientes.AddAsync(alunoFila);

        var turma = new TurmaEntity
        {
            ClinicaId = seed.ClinicaId,
            Nome = "Turma Promoção",
            TipoSessao = TipoSessao.Fisioterapia,
            Capacidade = 1,
            Ativo = true,
        };
        await db.Turmas.AddAsync(turma);
        await db.SaveChangesAsync();

        var inicio = DateTime.UtcNow.Date.AddDays(3).AddHours(15);
        var agendamento = new Agendamento
        {
            ClinicaId = seed.ClinicaId,
            PacienteId = seed.PacienteId,
            ProfissionalId = seed.ProfissionalId,
            DataHoraInicio = inicio,
            DataHoraFim = inicio.AddHours(1),
            TipoSessao = TipoSessao.Fisioterapia,
            TipoAula = TipoAula.Plano,
            TurmaId = turma.Id,
            Status = StatusAgendamento.Confirmado,
        };
        await db.Agendamentos.AddAsync(agendamento);
        await db.SaveChangesAsync();

        await db.WaitlistEntries.AddAsync(new WaitlistEntry
        {
            ClinicaId = seed.ClinicaId,
            TurmaId = turma.Id,
            PacienteId = alunoFila.Id,
            Ativo = true,
        });
        await db.SaveChangesAsync();

        return (turma.Id, agendamento.Id, alunoFila.Id);
    }

    private async Task<HttpClient> AutenticarAsync(SeedData seed)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);

        return client;
    }
}
