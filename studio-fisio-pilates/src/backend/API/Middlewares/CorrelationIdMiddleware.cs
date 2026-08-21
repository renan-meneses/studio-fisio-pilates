namespace Clinica.API.Middlewares;

/// <summary>
/// Correlation ID distribuído: aceita o header <c>X-Correlation-Id</c> do
/// cliente (ou gera um), propaga em <see cref="HttpContext.TraceIdentifier"/>
/// e ecoa na resposta para rastreio ponta a ponta de logs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string CorrelationHeaderName = "X-Correlation-Id";

    private const int MaxCorrelationIdLength = 100;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }
        else
        {
            correlationId = correlationId.Trim();
            if (correlationId.Length > MaxCorrelationIdLength)
                correlationId = correlationId[..MaxCorrelationIdLength];
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeaderName] = correlationId;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
        });

        await _next(context);
    }
}