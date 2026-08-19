using System.Linq.Expressions;
using Clinica.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Persistence.QueryFilters;

/// <summary>
/// Aplica Global Query Filters em todas as entidades que implementam
/// <see cref="ITenantEntity"/>, filtrando por <c>ClinicaId == tenant ativo</c>.
///
/// O filtro referencia <see cref="TenantDbContext.CurrentTenantId"/> (propriedade
/// da instância do contexto, alimentada pelo serviço scoped). Assim o modelo é
/// construído UMA vez (model cache compartilhado) e o tenant é reavaliado via
/// parâmetro de query em cada execução — correto para N tenants por processo.
/// </summary>
public static class TenantQueryFilterBuilder
{
    public static void ApplyGlobalFilters(ModelBuilder builder, TenantDbContext context)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(TenantQueryFilterBuilder)
                .GetMethod(nameof(ApplyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, new object[] { builder, context });
        }
    }

    private static void ApplyFilter<TEntity>(ModelBuilder builder, TenantDbContext context)
        where TEntity : class, ITenantEntity
    {
        // Member access sobre a instância do DbContext: o EF Core converte em
        // parâmetro reavaliado por query (padrão documentado para multitenancy).
        Expression<Func<TEntity, bool>> filter = e => e.ClinicaId == context.CurrentTenantId;
        builder.Entity<TEntity>().HasQueryFilter(filter);
    }
}