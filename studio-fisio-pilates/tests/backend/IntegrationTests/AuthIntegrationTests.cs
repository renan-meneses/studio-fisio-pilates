using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

[Collection("api")]
public sealed class AuthIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public AuthIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_com_senha_incorreta_retorna_401()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Login");
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var resposta = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = "senha-errada" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_corretamente_retorna_token_e_me_resolve_claims()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Login OK");
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var resposta = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var token = corpo.GetProperty("accessToken").GetString()!;
        token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await me.Content.ReadFromJsonAsync<JsonElement>();
        claims.GetProperty("tenantId").GetString().Should().Be(seed.TenantHeaderValue);
        claims.GetProperty("email").GetString().Should().Be(seed.Email);
        claims.GetProperty("authenticated").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Login_sem_header_descobre_tenant_da_clinica()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Tenant Discovery");
        var client = _fixture.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("tenantId").GetString().Should().Be(seed.TenantHeaderValue);
        corpo.GetProperty("tenantNome").GetString().Should().Be("Clínica Tenant Discovery");

        var token = corpo.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await me.Content.ReadFromJsonAsync<JsonElement>();
        claims.GetProperty("tenantId").GetString().Should().Be(seed.TenantHeaderValue);
    }
}