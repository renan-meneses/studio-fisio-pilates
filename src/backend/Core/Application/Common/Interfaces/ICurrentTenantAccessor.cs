namespace Clinica.Application.Common.Interfaces;

/// <summary>
/// Permite que o middleware da API alimente o tenant ativo no escopo da
/// requisição antes de qualquer trabalho de persistência.
/// </summary>
public interface ICurrentTenantAccessor
{
    void Set(Guid tenantId, string tenantName);
}