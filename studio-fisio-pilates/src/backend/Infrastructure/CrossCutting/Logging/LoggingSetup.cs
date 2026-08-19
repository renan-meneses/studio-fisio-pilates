using Microsoft.Extensions.DependencyInjection;

namespace Clinica.CrossCutting.Logging;

/// <summary>
/// Extensão de conveniência: em produção troque por Serilog/OpenTelemetry
/// sem tocar nas demais camadas (o contrato usado é ILogger&lt;T&gt;).
/// </summary>
public static class LoggingSetup
{
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services) => services;

    // TODO(prod): AddSerilog((ctx, cfg) => cfg.WriteTo.Console()
    //     .WriteTo.Seq("http://seq:5341"));
}