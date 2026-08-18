using Clinica.Application.Common.Interfaces;
using Clinica.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();

        services.AddDbContext<TenantDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                    configuration.GetConnectionString("Default"),
                    npgsql => npgsql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
        });

        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<TenantDbContext>());

        return services;
    }

    /// <summary>
    /// Implementação default: valores definidos pelo middleware da API via
    /// <see cref="ICurrentTenantAccessor"/> no mesmo escopo da requisição.
    /// </summary>
    private sealed class CurrentTenantService : ICurrentTenantService
    {
        public Guid? TenantId { get; internal set; }

        public string? TenantName { get; internal set; }
    }

    private sealed class CurrentTenantAccessor : ICurrentTenantAccessor
    {
        private readonly CurrentTenantService _service;

        public CurrentTenantAccessor(ICurrentTenantService service)
        {
            _service = (CurrentTenantService)service;
        }

        public void Set(Guid tenantId, string tenantName)
        {
            _service.TenantId = tenantId;
            _service.TenantName = tenantName;
        }
    }
}