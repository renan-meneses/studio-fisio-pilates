namespace Clinica.Application.Common.Interfaces;

/// <summary>
/// Contrato de hash de senha (implementado em CrossCutting.Auth).
/// A Application depende da abstração, mantendo a direção das dependências.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string storedHash);
}