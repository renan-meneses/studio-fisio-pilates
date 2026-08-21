using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// RBAC: dados clínicos (PEP) exigem papéis autorizados; cadastros
/// administrativos permanecem disponíveis para qualquer usuário autenticado.
/// </summary>
[Collection("api")]
public sealed class RbacIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public RbacIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Atendente")]
    [InlineData("Profissional")]
    public async Task Papeis_autorizados_acessam_pep_por_completo(string papel)
    {
        var seed = await _fixture.SeedClinicaAsync($"Clínica RBAC {papel}");
        var (client, login) = await AutenticarAsync(seed, papel);

        var abrir = await client.PostAsJsonAsync("/api/prontuarios", new { pacienteId = seed.PacienteId });
        abrir.StatusCode.Should().Be(HttpStatusCode.OK, "abertura de prontuário deve permitida para {0}", papel);
        var prontuarioId = (await abrir.Content.ReadFromJsonAsync<JsonElement>()).GetGuid();

        var evolucao = await client.PostAsJsonAsync($"/api/prontuarios/{prontuarioId}/evolucoes", new
        {
            profissionalId = seed.ProfissionalId,
            tipo = 2,
            conduta = "Conduta registrada no teste.",
        });
        evolucao.StatusCode.Should().Be(HttpStatusCode.OK);

        var leitura = await client.GetAsync($"/api/prontuarios/{prontuarioId}/evolucoes");
        leitura.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await leitura.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Financeiro_nao_acessa_pep_mas_acessa_cadastros_e_agenda()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica RBAC Financeiro");
        var (client, _) = await AutenticarAsync(seed, "Financeiro");

        var pep = await client.GetAsync($"/api/prontuarios/paciente/{seed.PacienteId}");
        pep.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var evolucoes = await client.GetAsync($"/api/prontuarios/{Guid.NewGuid()}/evolucoes");
        evolucoes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var prontuarios = await client.PostAsJsonAsync("/api/prontuarios", new { pacienteId = seed.PacienteId });
        prontuarios.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var pacientes = await client.GetAsync("/api/prontuarios/pacientes");
        pacientes.StatusCode.Should().Be(HttpStatusCode.OK, "cadastro de pacientes é administrativo");

        var agenda = await client.GetAsync("/api/agendamentos");
        agenda.StatusCode.Should().Be(HttpStatusCode.OK, "agenda é administrativo");
    }

    [Fact]
    public async Task Financeiro_nao_fabrica_evolucao_clinica()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica RBAC Fabricação");
        var (client, _) = await AutenticarAsync(seed, "Financeiro");

        var resposta = await client.PostAsJsonAsync(
            $"/api/prontuarios/{Guid.NewGuid()}/evolucoes", new
            {
                profissionalId = seed.ProfissionalId,
                tipo = 2,
                conduta = "Tentativa de escrita clínica.",
            });

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(HttpClient Client, SeedUsuario Usuario)> AutenticarAsync(SeedData seed, string papel)
    {
        var usuario = await _fixture.SeedUsuarioAsync(seed.ClinicaId, papel);

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = usuario.Email, senha = usuario.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);

        return (client, usuario);
    }
}