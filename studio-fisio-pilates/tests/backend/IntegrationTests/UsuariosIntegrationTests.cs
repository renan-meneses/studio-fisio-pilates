using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Gestão de usuários: apenas Administrador gerencia acessos; fluxo completo
/// de criação → login → desativação → redefinição de senha.
/// </summary>
[Collection("api")]
public sealed class UsuariosIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public UsuariosIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Admin_cria_usuario_e_novo_usuario_faz_login()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = await AutenticarAdminAsync(seed);

        var criar = await client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Bruna Atendente",
            email = "bruna@teste.local",
            senha = "Senha@1234",
            papel = "Atendente",
        });
        criar.StatusCode.Should().Be(HttpStatusCode.Created);
        var novoId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var listagem = await client.GetAsync("/api/usuarios");
        listagem.StatusCode.Should().Be(HttpStatusCode.OK);
        var usuarios = await listagem.Content.ReadFromJsonAsync<JsonElement>();
        usuarios.GetArrayLength().Should().Be(2);

        var anonimo = _fixture.CreateClient();
        var login = await anonimo.PostAsJsonAsync("/api/auth/login",
            new { email = "bruna@teste.local", senha = "Senha@1234" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("papel").GetString().Should().Be("Atendente");
        corpo.GetProperty("tenantId").GetGuid().Should().Be(seed.ClinicaId);
    }

    [Fact]
    public async Task Nao_admin_recebe_403_na_gestao_de_usuarios()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var usuario = await _fixture.SeedUsuarioAsync(seed.ClinicaId, "Atendente");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = usuario.Email, senha = usuario.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);

        (await client.GetAsync("/api/usuarios")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var criar = await client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Intruso",
            email = "intruso@teste.local",
            senha = "Senha@1234",
            papel = "Administrador",
        });
        criar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Email_duplicado_retorna_conflito()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = await AutenticarAdminAsync(seed);

        await client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Primeiro",
            email = "dup@teste.local",
            senha = "Senha@1234",
            papel = "Financeiro",
        });

        var duplicado = await client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Segundo",
            email = "dup@teste.local",
            senha = "Senha@1234",
            papel = "Financeiro",
        });
        duplicado.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Admin_nao_desativa_proprio_usuario()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = await AutenticarAdminAsync(seed);

        var adminId = await BuscarProprioIdAsync(client, seed.Email);

        var resposta = await client.PatchAsJsonAsync($"/api/usuarios/{adminId}/status", new { ativo = false });
        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Usuario_desativado_nao_consegue_logar()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = await AutenticarAdminAsync(seed);

        var criar = await client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Temporário",
            email = "temp@teste.local",
            senha = "Senha@1234",
            papel = "Profissional",
        });
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var desativar = await client.PatchAsJsonAsync($"/api/usuarios/{id}/status", new { ativo = false });
        desativar.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonimo = _fixture.CreateClient();
        var login = await anonimo.PostAsJsonAsync("/api/auth/login",
            new { email = "temp@teste.local", senha = "Senha@1234" });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Redefinir_senha_administrativa_e_login_com_nova_senha()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = await AutenticarAdminAsync(seed);

        var criar = await client.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Resetável",
            email = "reset@teste.local",
            senha = "SenhaAntiga@1",
            papel = "Atendente",
        });
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var redefinir = await client.PatchAsJsonAsync($"/api/usuarios/{id}/senha", new { novaSenha = "SenhaNova@2" });
        redefinir.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonimo = _fixture.CreateClient();

        var antiga = await anonimo.PostAsJsonAsync("/api/auth/login",
            new { email = "reset@teste.local", senha = "SenhaAntiga@1" });
        antiga.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var nova = await anonimo.PostAsJsonAsync("/api/auth/login",
            new { email = "reset@teste.local", senha = "SenhaNova@2" });
        nova.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> AutenticarAdminAsync(SeedData seed)
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

    private static async Task<Guid> BuscarProprioIdAsync(HttpClient client, string email)
    {
        var listagem = await client.GetAsync("/api/usuarios");
        listagem.EnsureSuccessStatusCode();
        var usuarios = await listagem.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var u in usuarios.EnumerateArray())
        {
            if (u.GetProperty("email").GetString() == email)
                return u.GetProperty("id").GetGuid();
        }

        throw new InvalidOperationException($"Usuário {email} não encontrado na listagem.");
    }
}
