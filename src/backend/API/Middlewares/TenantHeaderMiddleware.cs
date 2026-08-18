using Clinica.Application.Common.Interfaces;
using Clinica.CrossCutting.Auth;

namespace Clinica.API.Middlewares;

/// <summary>
/// Resolve o tenant ativo por header X-Tenant-Id e alimenta o
/// ICurrentTenantAccessor scoped antes de qualquer handler rodar.
/// Endpoints protegidos usam [RequireTenant] para validar presença.
/// </summary>
public sealed class TenantHeaderMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantIdHeader = context.Request.Headers[TenantHeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(tenantIdHeader))
        {
            if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "X-Tenant-Id deve ser um GUID válido." });
                return;
            }

            context.Items[TenantHeaderName] = tenantId;

            var accessor = context.RequestServices.GetService<ICurrentTenantAccessor>();
            var tenantService = context.RequestServices.GetService<ICurrentTenantService>();
            if (accessor is not null)
                accessor.Set(tenantId, tenantService?.TenantName ?? tenantId.ToString());

            var authTenant = context.User.GetTenantId();
            if (context.User.Identity?.IsAuthenticated == true && authTenant.HasValue && authTenant.Value != tenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    new { error = "Tenant do token não corresponde ao X-Tenant-Id informado." });
                return;
            }
        }

        await _next(context);
    }
}