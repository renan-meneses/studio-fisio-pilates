namespace Clinica.Application.Common.Interfaces;

/// <summary>Resultado da emissão: token + expiração real codificada no JWT.</summary>
public sealed record TokenResult(string Token, DateTime ExpiresAt);

/// <summary>Emissão de tokens JWT (implementado em CrossCutting.Auth).</summary>
public interface ITokenService
{
    TokenResult CreateToken(Guid userId, string email, Guid clinicaId, string clinicaNome, string papel);
}