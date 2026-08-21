using System.Text;
using Clinica.API.Middlewares;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clinica.UnitTests.Observability;

/// <summary>Idempotência: replays de POST com a mesma chave não duplicam efeitos.</summary>
public class IdempotencyMiddlewareTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static async Task<TenantDbContext> CriarDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(Tenant).Object);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static DefaultHttpContext CriarContexto(string? chave)
    {
        var context = new DefaultHttpContext();
        context.Items[TenantHeaderMiddleware.TenantHeaderName] = Tenant;
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/prontuarios/pacientes";
        context.Response.StatusCode = 200;
        context.Response.Body = new MemoryStream();
        if (chave is not null)
            context.Request.Headers[IdempotencyMiddleware.HeaderName] = chave;
        return context;
    }

    [Fact]
    public async Task Sem_header_o_pipeline_executa_normalmente()
    {
        var executou = false;
        var db = await CriarDbAsync();
        var middleware = new IdempotencyMiddleware(_ =>
        {
            executou = true;
            return Task.CompletedTask;
        }, NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(CriarContexto(null), db);

        executou.Should().BeTrue();
        (await db.IdempotencyRecords.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Primeira_execucao_persiste_e_segunda_nao_repete_o_pipeline()
    {
        var execucoes = 0;
        var db = await CriarDbAsync();
        var middleware = new IdempotencyMiddleware(_ =>
        {
            execucoes++;
            var corpo = Encoding.UTF8.GetBytes("{\"id\":\"abc\"}");
            _.Response.Body.Write(corpo, 0, corpo.Length);
            return Task.CompletedTask;
        }, NullLogger<IdempotencyMiddleware>.Instance);

        var primeiro = CriarContexto("chave-1");
        await middleware.InvokeAsync(primeiro, db);

        var segundo = CriarContexto("chave-1");
        await middleware.InvokeAsync(segundo, db);

        execucoes.Should().Be(1, "a segunda chamada é replay do cache");
        LerCorpo(primeiro).Should().Contain("\"id\":\"abc\"");
        LerCorpo(segundo).Should().Contain("\"id\":\"abc\"");
        (await db.IdempotencyRecords.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Chaves_diferentes_executam_independentes()
    {
        var execucoes = 0;
        var db = await CriarDbAsync();
        var middleware = new IdempotencyMiddleware(_ =>
        {
            execucoes++;
            return Task.CompletedTask;
        }, NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(CriarContexto("chave-a"), db);
        await middleware.InvokeAsync(CriarContexto("chave-b"), db);

        execucoes.Should().Be(2);
        (await db.IdempotencyRecords.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Metodo_nao_post_ignora_a_idempotencia()
    {
        var db = await CriarDbAsync();
        var middleware = new IdempotencyMiddleware(_ => Task.CompletedTask, NullLogger<IdempotencyMiddleware>.Instance);

        var context = CriarContexto("chave-get");
        context.Request.Method = HttpMethods.Get;
        await middleware.InvokeAsync(context, db);

        (await db.IdempotencyRecords.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Resposta_de_erro_nao_e_cacheada()
    {
        var db = await CriarDbAsync();
        var middleware = new IdempotencyMiddleware(_ =>
        {
            _.Response.StatusCode = 500;
            return Task.CompletedTask;
        }, NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(CriarContexto("chave-500"), db);

        (await db.IdempotencyRecords.CountAsync()).Should().Be(0);
    }

    private static string LerCorpo(DefaultHttpContext context) =>
        Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
}