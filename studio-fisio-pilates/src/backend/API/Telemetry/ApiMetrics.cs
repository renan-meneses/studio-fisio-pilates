using System.Diagnostics.Metrics;

namespace Clinica.API.Telemetry;

/// <summary>
/// Métricas por requisição (latência, volume, taxa de erro por tenant).
/// Baseadas em <see cref="Meter"/> nativo do .NET — sem dependências externas;
/// os dados fluem automaticamente quando um exporter OTLP for configurado.
/// </summary>
public sealed class ApiMetrics
{
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _requestsTotal;

    public ApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Clinica.Api");

        _requestDuration = meter.CreateHistogram<double>(
            name: "api.request.duration",
            unit: "ms",
            description: "Duração das requisições HTTP.");

        _requestsTotal = meter.CreateCounter<long>(
            name: "api.request.total",
            unit: "1",
            description: "Total de requisições HTTP, classificadas por status.");
    }

    public void Record(Guid? tenantId, string method, int statusCode, double durationMs)
    {
        var statusClass = statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx",
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "other",
        };

        _requestDuration.Record(durationMs,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", statusClass),
            new KeyValuePair<string, object?>("tenant_id", tenantId?.ToString() ?? "none"));

        _requestsTotal.Add(1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", statusClass),
            new KeyValuePair<string, object?>("tenant_id", tenantId?.ToString() ?? "none"));
    }
}