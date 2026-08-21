using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clinica.CrossCutting.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Clinica.UnitTests.Auth;

/// <summary>
/// Contrato do token: o papel do usuário deve sobreviver ao ciclo completo
/// emissão → assinatura → validação, resolvível por IsInRole/policy.
/// </summary>
public class JwtTokenServiceTests
{
    private const string Secret = "chave-de-teste-com-pelo-menos-32-bytes-0123456789";

    private static JwtOptions Opcoes() => new()
    {
        Issuer = "clinica-test",
        Audience = "clinica-test",
        SecretKey = Secret,
        ExpirationMinutes = 60,
    };

    private static ClaimsPrincipal Validar(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "clinica-test",
            ValidAudience = "clinica-test",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ClockSkew = TimeSpan.Zero,
        };

        return handler.ValidateToken(token, parametros, out _);
    }

    [Fact]
    public void Token_embarque_a_claim_de_papel_como_role()
    {
        var service = new JwtTokenService(Options.Create(Opcoes()));

        var emissao = service.CreateToken(
            Guid.NewGuid(), "user@teste.local", Guid.NewGuid(), "Clínica Teste", "Atendente");

        var payload = new JwtSecurityToken(emissao.Token).Payload;
        payload.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Atendente");
    }

    [Fact]
    public void Papel_do_token_e_resoluvel_como_IsInRole_apos_validacao()
    {
        var service = new JwtTokenService(Options.Create(Opcoes()));

        var emissao = service.CreateToken(
            Guid.NewGuid(), "user@teste.local", Guid.NewGuid(), "Clínica Teste", "Financeiro");

        var principal = Validar(emissao.Token);

        principal.IsInRole("Financeiro").Should().BeTrue();
        principal.IsInRole("Profissional").Should().BeFalse();
        principal.GetPapel().Should().Be("Financeiro");
    }

    [Fact]
    public void Tenant_e_identidade_continuam_resolvidos_apos_validacao()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new JwtTokenService(Options.Create(Opcoes()));

        var emissao = service.CreateToken(userId, "user@teste.local", tenantId, "Clínica Teste", "Administrador");

        var principal = Validar(emissao.Token);

        principal.GetUserId().Should().Be(userId);
        principal.GetTenantId().Should().Be(tenantId);
        principal.GetEmail().Should().Be("user@teste.local");
    }

    [Fact]
    public void ExpiresAt_retornado_equivale_ao_exp_codificado_no_jwt()
    {
        var service = new JwtTokenService(Options.Create(Opcoes()));

        var emissao = service.CreateToken(
            Guid.NewGuid(), "user@teste.local", Guid.NewGuid(), "Clínica Teste", "Administrador");

        var payload = new JwtSecurityToken(emissao.Token).Payload;
        var expCodificado = DateTimeOffset.FromUnixTimeSeconds(payload.Expiration!.Value).UtcDateTime;

        emissao.ExpiresAt.Should().Be(expCodificado);
        emissao.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }
}