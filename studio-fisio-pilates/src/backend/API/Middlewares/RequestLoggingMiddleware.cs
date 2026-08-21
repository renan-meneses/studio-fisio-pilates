using System.Diagnostics;
using Clinica.API.Telemetry;
using Clinica.CrossCutting.Auth;

namespace Clinica.API.Middlewares;

/// <summary>
/// Log estruturado por requisição: abre scope com tenant e usuário ativos
/// (herdado por todos os logs do ciclo) e registra a linha final com
/// método, path, status, duração e métricas.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApiMetrics metrics)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();
        var stopwatch = Stopwatch.StartNew();

        var tenantId = context.Items[TenantHeaderMiddleware.TenantHeaderName] is Guid tid ? tid : (Guid?)null;
        var userId = context.User.GetUserId();

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TenantId"] = tenantId,
            ["UserId"] = userId,
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            metrics.Record(tenantId, method, statusCode, stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {Duration}ms",
                method, path, statusCode, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}