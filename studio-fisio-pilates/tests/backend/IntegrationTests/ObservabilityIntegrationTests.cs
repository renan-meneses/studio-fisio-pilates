using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Clinica.API.Middlewares;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Observabilidade no pipeline HTTP real: o X-Correlation-Id do cliente
/// deve atravessar o ciclo e aparecer na resposta; sem header, o servidor
/// gera e ecoa um novo.
/// </summary>
[Collection("api")]
public sealed class ObservabilityIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public ObservabilityIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Correlation_id_do_cliente_e_eco_na_resposta()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Observabilidade");
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.CorrelationHeaderName, "e2e-correlation-abc");

        var resposta = await LoginEConsultarMeAsync(client, seed);

        resposta.Headers.GetValues(CorrelationIdMiddleware.CorrelationHeaderName)
            .Should().Contain("e2e-correlation-abc");
    }

    [Fact]
    public async Task Quando_ausente_servidor_gera_e_eco_um_novo_correlation_id()
    {
        var resposta = await _fixture.CreateClient().GetAsync("/health");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var correlationId = resposta.Headers.GetValues(CorrelationIdMiddleware.CorrelationHeaderName).First();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<HttpResponseMessage> LoginEConsultarMeAsync(HttpClient client, SeedData seed)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = corpo.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/api/auth/me");
    }
}