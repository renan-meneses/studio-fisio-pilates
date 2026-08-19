namespace Clinica.Application.Common.Interfaces;

/// <summary>Emissão de tokens JWT (implementado em CrossCutting.Auth).</summary>
public interface ITokenService
{
    string CreateToken(Guid userId, string email, Guid clinicaId, string clinicaNome);
}