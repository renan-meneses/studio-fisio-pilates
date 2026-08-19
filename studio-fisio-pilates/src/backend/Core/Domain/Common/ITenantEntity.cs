namespace Clinica.Domain.Common;

/// <summary>
/// Contrato de multitenancy: toda entidade de negócio pertence a uma clínica.
/// O EF Core usa esta interface para aplicar Global Query Filters (leitura)
/// e o TenantSaveChangesInterceptor (escrita).
/// </summary>
public interface ITenantEntity
{
    Guid ClinicaId { get; set; }
}