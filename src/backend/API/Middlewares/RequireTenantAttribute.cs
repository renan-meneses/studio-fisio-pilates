using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clinica.API.Middlewares;

/// <summary>
/// Exige a presença de X-Tenant-Id na requisição (HTTP 400 quando ausente).
/// Aplica-se a controllers de negócio; onboarding (criação de clínica) fica isento.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireTenantAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var hasTenant = context.HttpContext.Items[TenantHeaderMiddleware.TenantHeaderName] is Guid;
        if (!hasTenant)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "Header X-Tenant-Id é obrigatório.",
            });
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}