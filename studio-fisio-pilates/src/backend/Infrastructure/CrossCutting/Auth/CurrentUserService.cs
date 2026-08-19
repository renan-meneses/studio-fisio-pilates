using System.Security.Claims;
using Clinica.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Clinica.CrossCutting.Auth;

/// <summary>
/// Implementação de ICurrentUserService a partir dos claims do JWT autenticado.
/// Registrada em escopo por requisição.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var identity = _httpContextAccessor.HttpContext?.User;
            return identity is { Identity.IsAuthenticated: true } ? identity.GetUserId() : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.GetEmail();

    public Guid? ClinicaId => _httpContextAccessor.HttpContext?.User.GetTenantId();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}