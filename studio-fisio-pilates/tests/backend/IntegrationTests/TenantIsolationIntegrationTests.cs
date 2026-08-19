using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Isolamento multitenant verificado por HTTP: dados de uma clínica
/// nunca aparecem no contexto de outra, e token com tenant divergente
/// do header recebe 403.
/// </summary>
[Collection("api")]
public sealed class TenantIsolationIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public TenantIsolationIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Dados_de_uma_clinica_nao_vazam_para_outra()
    {
        var clinicaA = await _fixture.SeedClinicaAsync("Clínica A Isolamento");
        var clinicaB = await _fixture.SeedClinicaAsync("Clínica B Isolamento");

        var tokenA = await LoginAsync(clinicaA);
        var tokenB = await LoginAsync(clinicaB);

        var clienteA = _fixture.CreateClient();
        clienteA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        clienteA.DefaultRequestHeaders.Add("X-Tenant-Id", clinicaA.TenantHeaderValue);

        var clienteB = _fixture.CreateClient();
        clienteB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        clienteB.DefaultRequestHeaders.Add("X-Tenant-Id", clinicaB.TenantHeaderValue);

        var criacao = await clienteA.PostAsJsonAsync("/api/agendamentos", new
        {
            pacienteId = clinicaA.PacienteId,
            profissionalId = clinicaA.ProfissionalId,
            dataHoraInicio = new DateTime(2026, 8, 25, 9, 0, 0),
            dataHoraFim = new DateTime(2026, 8, 25, 10, 0, 0),
            tipoSessao = 2,
            valorSessao = 120m,
            observacoes = "Sessão da clínica A",
        });
        criacao.StatusCode.Should().Be(HttpStatusCode.Created);

        var listaB = await clienteB.GetAsync("/api/agendamentos");
        listaB.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpoB = await listaB.Content.ReadFromJsonAsync<JsonElement>();

        if (corpoB.ValueKind == JsonValueKind.Array)
        {
            corpoB.GetArrayLength().Should().Be(0, "a clínica B não possui agendamentos");
        }
        else
        {
            // Endpoint protegido sem dados de A: pode retornar objeto vazio de lista.
            corpoB.ToString().Should().NotContain("Clínica A");
        }
    }

    [Fact]
    public async Task Token_de_outro_tenant_com_header_divergente_recebe_403()
    {
        var clinicaA = await _fixture.SeedClinicaAsync("Clínica A Divergência");
        var clinicaB = await _fixture.SeedClinicaAsync("Clínica B Divergência");

        var tokenA = await LoginAsync(clinicaA);

        var cliente = _fixture.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        cliente.DefaultRequestHeaders.Add("X-Tenant-Id", clinicaB.TenantHeaderValue);

        var resposta = await cliente.GetAsync("/api/agendamentos");
        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> LoginAsync(SeedData seed)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        var resposta = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("accessToken").GetString()!;
    }
}