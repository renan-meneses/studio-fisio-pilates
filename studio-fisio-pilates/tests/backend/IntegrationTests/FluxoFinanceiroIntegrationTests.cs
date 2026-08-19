using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Fluxo financeiro completo via HTTP: cobrança de mensalidade,
/// registro de pagamento e reflexo no dashboard.
/// </summary>
[Collection("api")]
public sealed class FluxoFinanceiroIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public FluxoFinanceiroIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Cobrar_receber_e_dashboard_refletem_o_movimento()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Financeiro");
        var client = _fixture.CreateClient();
        await AutenticarAsync(client, seed);

        var competencia = DateTime.Now.ToString("yyyy-MM");

        var cobranca = await client.PostAsJsonAsync("/api/mensalidades", new
        {
            pacienteId = seed.PacienteId,
            competencia,
            valor = 350.00m,
        });
        cobranca.StatusCode.Should().Be(HttpStatusCode.OK);
        var mensalidadeId = (await cobranca.Content.ReadAsStringAsync()).Trim('"');

        var lista = await client.GetAsync($"/api/mensalidades?competencia={competencia}");
        lista.StatusCode.Should().Be(HttpStatusCode.OK);
        var mensalidades = await lista.Content.ReadFromJsonAsync<JsonElement>();
        mensalidades.GetArrayLength().Should().Be(1);
        mensalidades[0].GetProperty("id").GetString().Should().Be(mensalidadeId);
        mensalidades[0].GetProperty("status").GetString().Should().Be("Pendente");

        var pagamento = await client.PostAsync($"/api/mensalidades/{mensalidadeId}/pagar", null);
        pagamento.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var dashboard = await client.GetAsync($"/api/financeiro/dashboard?competencia={competencia}");
        dashboard.StatusCode.Should().Be(HttpStatusCode.OK);
        var resumo = await dashboard.Content.ReadFromJsonAsync<JsonElement>();
        resumo.GetProperty("receitaRecebida").GetDecimal().Should().Be(350.00m);
        resumo.GetProperty("mensalidadesAtrasadas").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Contas_a_pagar_sao_registradas_e_baixadas()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Contas");
        var client = _fixture.CreateClient();
        await AutenticarAsync(client, seed);

        var criacao = await client.PostAsJsonAsync("/api/contas-pagar", new
        {
            fornecedor = "Imobiliária Center",
            descricao = "Aluguel do espaço",
            valor = 1800.00m,
            dataVencimento = DateTime.Today.AddDays(10),
            tipoCusto = 1,
        });
        criacao.StatusCode.Should().Be(HttpStatusCode.OK);
        var contaId = (await criacao.Content.ReadAsStringAsync()).Trim('"');

        var baixa = await client.PostAsync($"/api/contas-pagar/{contaId}/baixar", null);
        baixa.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lista = await client.GetAsync("/api/contas-pagar");
        lista.StatusCode.Should().Be(HttpStatusCode.OK);
        var contas = await lista.Content.ReadFromJsonAsync<JsonElement>();
        contas.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    private static async Task AutenticarAsync(HttpClient client, SeedData seed)
    {
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);
    }
}