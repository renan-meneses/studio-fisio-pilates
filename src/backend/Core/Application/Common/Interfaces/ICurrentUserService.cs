namespace Clinica.Application.Common.Interfaces;

/// <summary>Identidade do usuário autenticado (claims do JWT).</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    Guid? ClinicaId { get; }

    bool IsAuthenticated { get; }
}