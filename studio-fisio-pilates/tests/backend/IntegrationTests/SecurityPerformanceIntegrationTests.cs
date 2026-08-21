using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Segurança e performance: headers de segurança em todas as respostas,
/// paginação das listagens (teto de resultados).
/// </summary>
[Collection("api")]
public sealed class SecurityPerformanceIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public SecurityPerformanceIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Respostas_carregam_headers_de_seguranca()
    {
        var resposta = await _fixture.CreateClient().GetAsync("/health");
        resposta.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);

        resposta.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options");
        resposta.Headers.Should().Contain(h => h.Key == "X-Frame-Options");
        resposta.Headers.Should().Contain(h => h.Key == "Referrer-Policy");
    }

    [Fact]
    public async Task Health_live_e_ready_respondem_ok()
    {
        var client = _fixture.CreateClient();

        var live = await client.GetAsync("/health/live");
        live.StatusCode.Should().Be(HttpStatusCode.OK);

        var ready = await client.GetAsync("/health/ready");
        ready.StatusCode.Should().Be(HttpStatusCode.OK,
            "readiness valida conectividade com o Postgres do testcontainer");

        var health = await client.GetAsync("/health");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listagem_de_pacientes_respeita_limite()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Paginação");
        var client = _fixture.CreateClient();
        await AutenticarAsync(client, seed);

        for (var i = 0; i < 3; i++)
        {
            var cpf = Random.Shared.NextInt64(10000000000, 99999999999).ToString();
            await client.PostAsJsonAsync("/api/prontuarios/pacientes", new
            {
                nome = $"Paciente Pag {i}",
                cpf,
                dataNascimento = "1990-01-01T00:00:00",
                telefone = "11900000000",
            });
        }

        var resposta = await client.GetAsync("/api/prontuarios/pacientes?limite=2");
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetArrayLength().Should().Be(2);

        // Sem limite explícito: default 200, devolve tudo (3).
        var semLimite = await client.GetAsync("/api/prontuarios/pacientes");
        var todos = await semLimite.Content.ReadFromJsonAsync<JsonElement>();
        todos.GetArrayLength().Should().BeGreaterThanOrEqualTo(3);
    }

    private static async Task AutenticarAsync(HttpClient client, SeedData seed)
    {
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.EnsureSuccessStatusCode();
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);
    }
}
