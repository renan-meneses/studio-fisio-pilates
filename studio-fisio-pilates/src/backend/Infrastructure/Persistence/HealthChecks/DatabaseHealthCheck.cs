using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Clinica.Persistence.HealthChecks;

/// <summary>
/// Readiness: verifica conectividade com o Postgres (SELECT 1).
/// Usado no probe <c>/health/ready</c>; liveness não depende dele.
/// </summary>
public sealed class DatabaseHealthCheck(TenantDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("Postgres acessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres inacessível.", ex);
        }
    }
}
