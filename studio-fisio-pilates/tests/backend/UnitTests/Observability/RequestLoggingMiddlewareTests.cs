using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Clinica.API.Middlewares;
using Clinica.API.Telemetry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Clinica.UnitTests.Observability;

/// <summary>Cria meters reais em memória (sem listener) para os testes.</summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = new();

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in _meters)
            meter.Dispose();
    }
}

public class RequestLoggingMiddlewareTests
{
    private static (DefaultHttpContext Context, CapturingLogger<RequestLoggingMiddleware> Logger, ApiMetrics Metrics)
        CriarCenario()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/agendamentos";
        context.Response.StatusCode = 200;

        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var metrics = new ApiMetrics(new TestMeterFactory());

        return (context, logger, metrics);
    }

    [Fact]
    public async Task Abre_scope_com_tenant_e_usuarios_resolvidos()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (context, logger, metrics) = CriarCenario();

        context.Items[TenantHeaderMiddleware.TenantHeaderName] = tenantId;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
            "teste"));

        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger);

        await middleware.InvokeAsync(context, metrics);

        var scope = logger.Entries
            .Where(e => e.Scope is not null)
            .SelectMany(e => e.Scope!)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        scope["TenantId"].Should().Be(tenantId);
        scope["UserId"].Should().Be(userId);
    }

    [Fact]
    public async Task Registra_linha_final_com_metodo_path_status_e_duracao()
    {
        var (context, logger, metrics) = CriarCenario();

        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger);

        await middleware.InvokeAsync(context, metrics);

        var mensagem = logger.Entries
            .Single(e => e.Level == LogLevel.Information && !string.IsNullOrWhiteSpace(e.Message));

        mensagem.Message.Should().Contain("GET /api/agendamentos responded 200 in");
        mensagem.Message.Should().EndWith("ms");
    }

    [Fact]
    public async Task Completa_o_log_mesmo_quando_o_pipeline_lanca_excecao()
    {
        var (context, logger, metrics) = CriarCenario();
        context.Response.StatusCode = 500;

        var middleware = new RequestLoggingMiddleware(
            _ => throw new InvalidOperationException("falha simulada"),
            logger);

        var ato = async () => await middleware.InvokeAsync(context, metrics);

        await ato.Should().ThrowAsync<InvalidOperationException>();
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("responded 500"));
    }
}