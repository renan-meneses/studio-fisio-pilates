using Clinica.API.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clinica.UnitTests.Observability;

public class CorrelationIdMiddlewareTests
{
    private static async Task<DefaultHttpContext> ExecutarAsync(Action<DefaultHttpContext>? configurar = null)
    {
        var context = new DefaultHttpContext();
        configurar?.Invoke(context);

        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new CorrelationIdMiddleware(next, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task Aceita_correlation_id_do_cliente_e_eco_na_resposta()
    {
        var context = await ExecutarAsync(ctx =>
            ctx.Request.Headers[CorrelationIdMiddleware.CorrelationHeaderName] = "cli-correlation-123");

        context.Response.Headers[CorrelationIdMiddleware.CorrelationHeaderName].ToString()
            .Should().Be("cli-correlation-123");
        context.TraceIdentifier.Should().Be("cli-correlation-123");
    }

    [Fact]
    public async Task Gera_correlation_id_quando_ausente()
    {
        var context = await ExecutarAsync();

        var correlationId = context.Response.Headers[CorrelationIdMiddleware.CorrelationHeaderName].ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        context.TraceIdentifier.Should().Be(correlationId);
    }

    [Fact]
    public async Task Sanitiza_correlation_id_longo()
    {
        var excessivo = new string('a', 500);

        var context = await ExecutarAsync(ctx =>
            ctx.Request.Headers[CorrelationIdMiddleware.CorrelationHeaderName] = excessivo);

        context.Response.Headers[CorrelationIdMiddleware.CorrelationHeaderName].ToString()
            .Length.Should().Be(100);
    }
}