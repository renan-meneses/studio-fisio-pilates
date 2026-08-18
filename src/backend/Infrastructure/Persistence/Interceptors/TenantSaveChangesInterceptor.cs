using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Clinica.Persistence.Interceptors;

/// <summary>
/// Interceptor de escrita: injeta <see cref="ITenantEntity.ClinicaId"/> em todo
/// insert e gerencia CreatedAt/UpdatedAt de auditoria.
/// </summary>
public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenantService _tenant;

    public TenantSaveChangesInterceptor(ICurrentTenantService tenant)
    {
        _tenant = tenant;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var tenantId = _tenant.TenantId;
        var now = DateTime.UtcNow;
        var user = _tenant.TenantName ?? "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (tenantId.HasValue && entry.Entity is ITenantEntity tenantEntity)
                    {
                        if (tenantEntity.ClinicaId == Guid.Empty)
                            tenantEntity.ClinicaId = tenantId.Value;
                    }

                    if (entry.Entity is BaseEntity baseEntity)
                    {
                        baseEntity.CreatedAt = now;
                        baseEntity.CreatedBy = user;
                    }

                    break;

                case EntityState.Modified:
                    if (entry.Entity is BaseEntity modified)
                    {
                        modified.UpdatedAt = now;
                        modified.UpdatedBy = user;
                        entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    }

                    break;
            }
        }
    }
}