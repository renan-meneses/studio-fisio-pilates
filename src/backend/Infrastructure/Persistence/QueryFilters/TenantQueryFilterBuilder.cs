using System.Linq.Expressions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Persistence.QueryFilters;

/// <summary>
/// Aplica Global Query Filters em todas as entidades que implementam
/// <see cref="ITenantEntity"/>, filtrando por <c>ClinicaId == tenant ativo</c>.
/// O valor é reavaliado por query (o tenant service é scoped por requisição),
/// garantindo isolamento mesmo com o mesmo modelo compartilhado.
/// </summary>
public static class TenantQueryFilterBuilder
{
    public static void ApplyGlobalFilters(ModelBuilder builder, ICurrentTenantService tenant)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(TenantQueryFilterBuilder)
                .GetMethod(nameof(ApplyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, new object[] { builder, tenant });
        }
    }

    private static void ApplyFilter<TEntity>(ModelBuilder builder, ICurrentTenantService tenant)
        where TEntity : class, ITenantEntity
    {
        // O EF Core converte tenant.TenantId numa comparação parametrizada,
        // reavaliando o valor do serviço scoped a cada execução de query.
        builder.Entity<TEntity>().HasQueryFilter(e => e.ClinicaId == tenant.TenantId);
    }
}