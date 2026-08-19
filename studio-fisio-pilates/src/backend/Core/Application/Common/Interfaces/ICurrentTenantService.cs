namespace Clinica.Application.Common.Interfaces;

/// <summary>
/// Resolve o tenant ativo no escopo da requisição.
/// Alimentado pelo middleware (header X-Tenant-Id) e validado contra o
/// claim tenant_id do token JWT.
/// </summary>
public interface ICurrentTenantService
{
    Guid? TenantId { get; }

    string? TenantName { get; }
}