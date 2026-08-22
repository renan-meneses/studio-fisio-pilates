using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Clinica.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Fluxo "esqueci minha senha" via HTTP: solicitação anti-enumeração,
/// redefinição com token de uso único e login com a nova senha.
/// </summary>
[Collection("api")]
public sealed class RecuperacaoSenhaIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public RecuperacaoSenhaIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Solicitacao_responde_204_independente_da_existencia_do_email()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        (await client.PostAsJsonAsync("/api/auth/recuperar-senha", new { email = seed.Email }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync("/api/auth/recuperar-senha", new { email = "ninguem@teste.local" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Redefinicao_com_token_valido_permite_login_e_invalida_a_senha_antiga()
    {
        var seed = await _fixture.SeedClinicaAsync();
        const string tokenBruto = "token-conhecido-de-teste";
        await SemearTokenAsync(seed, tokenBruto, expirado: false);

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var resposta = await client.PostAsJsonAsync("/api/auth/redefinir-senha",
            new { email = seed.Email, token = tokenBruto, novaSenha = "SenhaNova@9" });
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Senha antiga rejeitada; nova aceita.
        (await client.PostAsJsonAsync("/api/auth/login", new { email = seed.Email, senha = seed.Senha }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/login", new { email = seed.Email, senha = "SenhaNova@9" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Uso único: mesma combinação não funciona de novo.
        (await client.PostAsJsonAsync("/api/auth/redefinir-senha",
                new { email = seed.Email, token = tokenBruto, novaSenha = "SenhaNova@10" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Token_expirado_ou_desconhecido_retorna_409()
    {
        var seed = await _fixture.SeedClinicaAsync();
        await SemearTokenAsync(seed, "token-expirado-http", expirado: true);

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        (await client.PostAsJsonAsync("/api/auth/redefinir-senha",
                new { email = seed.Email, token = "token-expirado-http", novaSenha = "SenhaNova@9" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await client.PostAsJsonAsync("/api/auth/redefinir-senha",
                new { email = seed.Email, token = "totalmente-desconhecido", novaSenha = "SenhaNova@9" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>Semeia o token diretamente no banco simulando o e-mail recebido.</summary>
    private async Task SemearTokenAsync(SeedData seed, string tokenBruto, bool expirado)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Clinica.Persistence.TenantDbContext>();

        var usuarioId = (await db.Usuarios.IgnoreQueryFilters()
            .SingleAsync(u => u.Email == seed.Email)).Id;

        db.TokensRedefinicaoSenha.Add(new TokenRedefinicaoSenha
        {
            ClinicaId = seed.ClinicaId,
            UsuarioId = usuarioId,
            TokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(tokenBruto))),
            ExpiraEm = expirado ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();
    }
}
