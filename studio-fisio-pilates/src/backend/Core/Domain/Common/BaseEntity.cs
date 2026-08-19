namespace Clinica.Domain.Common;

/// <summary>
/// Entidade base para todas as entidades de negócio.
/// Identidade por Guid, linha do tempo de auditoria e multitenancy.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = "system";

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}