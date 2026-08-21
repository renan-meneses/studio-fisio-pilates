using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clinica.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Clinica.CrossCutting.Auth;

/// <summary>
/// Emissão e validação de tokens JWT.
/// Claims embarcadas: sub (usuário), email, role (papel do usuário),
/// tenant_id e clinica (nome).
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenResult CreateToken(Guid userId, string email, Guid clinicaId, string clinicaNome, string papel)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, papel),
            new Claim(CustomClaimTypes.TenantId, clinicaId.ToString()),
            new Claim(CustomClaimTypes.TenantName, clinicaNome),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        // A expiração retornada é a claim exp codificada no JWT (fonte única da
        // verdade) — elimina o drift entre o valor exposto na API e o valido.
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(token.Payload.Expiration!.Value).UtcDateTime;

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public static class CustomClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string TenantName = "tenant_name";
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Lê o sub com fallback: o handler do ASP.NET Core pode remapear
    /// "sub" para o URI longo (nameidentifier) quando MapInboundClaims não é desabilitado.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static Guid? GetTenantId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(CustomClaimTypes.TenantId), out var id) ? id : null;

    public static string? GetPapel(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? principal.FindFirstValue(ClaimTypes.Email);
}