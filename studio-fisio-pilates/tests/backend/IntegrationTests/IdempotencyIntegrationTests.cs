using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Idempotência ponta a ponta: replays de POST com a mesma Idempotency-Key
/// devolvem o mesmo recurso sem duplicar dados no banco.
/// </summary>
[Collection("api")]
public sealed class IdempotencyIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public IdempotencyIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Mesma_chave_retorna_mesmo_recurso_e_nao_duplica_paciente()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Idempotência");
        var (client, _) = await AutenticarAsync(seed);

        var corpo = new { nome = "Paciente Idempotente", cpf = "12345678901" };

        var primeiro = await EnviarComChaveAsync(client, corpo);
        var replay1 = await EnviarComChaveAsync(client, corpo);
        var replay2 = await EnviarComChaveAsync(client, corpo);

        primeiro.Should().NotBe(Guid.Empty);
        replay1.Should().Be(primeiro, "replay devolve o mesmo recurso da primeira execução");
        replay2.Should().Be(primeiro);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var pacientes = await db.Pacientes.IgnoreQueryFilters().Where(p => p.CPF == "12345678901").ToListAsync();
        pacientes.Should().ContainSingle("a mesma chave não pode duplicar o paciente");
    }

    private static async Task<Guid> EnviarComChaveAsync(HttpClient client, object corpo)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/prontuarios/pacientes")
        {
            Content = JsonContent.Create(corpo),
        };
        request.Headers.Add("Idempotency-Key", "chave-e2e-1");

        var resposta = await client.SendAsync(request);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<(HttpClient Client, SeedUsuario Usuario)> AutenticarAsync(SeedData seed)
    {
        var usuario = await _fixture.SeedUsuarioAsync(seed.ClinicaId, "Atendente");

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