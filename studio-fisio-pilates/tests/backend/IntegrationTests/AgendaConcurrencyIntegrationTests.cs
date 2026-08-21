using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Concorrência de agenda no Postgres real: requisições paralelas para o
/// mesmo slot do profissional — apenas UMA passa (lock advisory por
/// transação); turma cheia vira 409 e a fila de espera registra o aluno.
/// </summary>
[Collection("api")]
public sealed class AgendaConcurrencyIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public AgendaConcurrencyIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Posts_paralelos_no_mesmo_slot_aceitam_apenas_um()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Corrida");
        var clients = Enumerable.Range(0, 5).Select(_ =>
        {
            var c = _fixture.CreateClient();
            return AutenticarAsync(c, seed);
        }).ToList();

        var inicio = new DateTime(2026, 10, 1, 18, 0, 0);
        var comando = new
        {
            pacienteId = seed.PacienteId,
            profissionalId = seed.ProfissionalId,
            dataHoraInicio = inicio,
            dataHoraFim = inicio.AddHours(1),
            tipoSessao = 1, // PilatesSolo
        };

        var respostas = await Task.WhenAll(clients.Select(async tarefa =>
        {
            var client = await tarefa;
            return await client.PostAsJsonAsync("/api/agendamentos", comando);
        }));

        respostas.Count(r => r.StatusCode == HttpStatusCode.Created)
            .Should().Be(1, "apenas um agendamento pode ocupar o slot");
        respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict)
            .Should().Be(clients.Count - 1);
    }

    [Fact]
    public async Task Turma_cheia_rejeita_e_waitlist_registra_aluno()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Turma Cheia");
        var client = _fixture.CreateClient();
        await AutenticarAsync(client, seed);

        // Turma com capacidade 2 no mesmo horário semanal.
        var criarTurma = await client.PostAsJsonAsync("/api/turmas", new
        {
            nome = "Turma Lotada",
            tipoSessao = 1,
            capacidade = 2,
        });
        criarTurma.StatusCode.Should().Be(HttpStatusCode.Created);
        var turmaId = (await criarTurma.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Dois alunos ocupam as vagas.
        for (var i = 0; i < 2; i++)
        {
            var pacienteId = await CriarPacienteAsync(client);
            var resposta = await client.PostAsJsonAsync("/api/agendamentos", new
            {
                pacienteId,
                profissionalId = seed.ProfissionalId,
                dataHoraInicio = new DateTime(2026, 10, 5, 9, 0, 0),
                dataHoraFim = new DateTime(2026, 10, 5, 10, 0, 0),
                tipoSessao = 1,
                tipoAula = 2,
                turmaId,
            });
            resposta.StatusCode.Should().Be(HttpStatusCode.Created,
                $"aluno {i + 1} deve conseguir vaga");
        }

        // Terceiro aluno: turma cheia → 409.
        var terceiro = await CriarPacienteAsync(client);
        var lotado = await client.PostAsJsonAsync("/api/agendamentos", new
        {
            pacienteId = terceiro,
            profissionalId = seed.ProfissionalId,
            dataHoraInicio = new DateTime(2026, 10, 5, 9, 0, 0),
            dataHoraFim = new DateTime(2026, 10, 5, 10, 0, 0),
            tipoSessao = 1,
            tipoAula = 2,
            turmaId,
        });
        lotado.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Fila de espera: entrada idempotente e listagem ordenada.
        var entrar = await client.PostAsJsonAsync(
            $"/api/turmas/{turmaId}/waitlist", new { pacienteId = terceiro });
        entrar.StatusCode.Should().Be(HttpStatusCode.OK);

        var reentrar = await client.PostAsJsonAsync(
            $"/api/turmas/{turmaId}/waitlist", new { pacienteId = terceiro });
        var primeiraEntrada = (await entrar.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        (await reentrar.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid().Should().Be(primeiraEntrada);

        var fila = await client.GetAsync($"/api/turmas/{turmaId}/waitlist");
        fila.StatusCode.Should().Be(HttpStatusCode.OK);
        var conteudo = await fila.Content.ReadFromJsonAsync<JsonElement>();
        conteudo.GetArrayLength().Should().Be(1);
        conteudo[0].GetProperty("pacienteId").GetString()
            .Should().Be(terceiro.ToString());
    }

    private async Task<Guid> CriarPacienteAsync(HttpClient client)
    {
        var cpf = Random.Shared.NextInt64(10000000000, 99999999999).ToString();
        var resposta = await client.PostAsJsonAsync("/api/prontuarios/pacientes", new
        {
            nome = $"Aluno {cpf[^4..]}",
            cpf,
            dataNascimento = "1995-03-10T00:00:00",
            telefone = "11988887777",
        });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
    }

    private static async Task<HttpClient> AutenticarAsync(HttpClient client, SeedData seed)
    {
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.EnsureSuccessStatusCode();
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);
        return client;
    }
}
