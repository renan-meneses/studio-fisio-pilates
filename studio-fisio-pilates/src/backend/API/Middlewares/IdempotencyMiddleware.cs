using System.Collections.Concurrent;
using System.Text;
using Clinica.Domain.Entities;
using Clinica.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinica.API.Middlewares;

/// <summary>
/// Idempotência genérica para POST via header <c>Idempotency-Key</c>.
/// A primeira execução com a chave é processada e a resposta persistida;
/// replays da mesma chave/método/rota retornam a resposta original sem
/// efeitos colaterais. Persistência é best-effort (falha degrada para
/// execução direta, com warning no log).
/// </summary>
public sealed class IdempotencyMiddleware
{
    public const string HeaderName = "Idempotency-Key";

    private const int TtlHoras = 24;
    private const int MaxKeyLength = 200;

    private sealed record RespostaCacheada(int StatusCode, string Body, DateTime ExpiresAtUtc)
    {
        public bool Expirada => ExpiresAtUtc <= DateTime.UtcNow;
    }

    // Cache em memória para replays frequentes; o banco é a fonte de
    // verdade entre réplicas/restarts. Chaves são serializadas por semáforo.
    private static readonly ConcurrentDictionary<string, Lazy<SemaphoreSlim>> _locks = new();
    private static readonly ConcurrentDictionary<string, RespostaCacheada> _cache = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantDbContext db)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength ||
            context.Items[TenantHeaderMiddleware.TenantHeaderName] is not Guid tenantId)
        {
            await _next(context);
            return;
        }

        var escopo = $"{tenantId}|{key}|{context.Request.Method}|{context.Request.Path}";
        var semaforo = _locks.GetOrAdd(escopo, _ => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1))).Value;

        await semaforo.WaitAsync();
        try
        {
            if (_cache.TryGetValue(escopo, out var cacheada) && !cacheada.Expirada)
            {
                await ReplayAsync(context, cacheada);
                return;
            }

            var registro = await db.IdempotencyRecords
                .FirstOrDefaultAsync(
                    r => r.Key == key && r.Method == context.Request.Method &&
                         r.Path == context.Request.Path.Value && r.ExpiresAtUtc > DateTime.UtcNow,
                    context.RequestAborted);

            if (registro is not null)
            {
                var existente = new RespostaCacheada(registro.StatusCode, registro.ResponseBody, registro.ExpiresAtUtc);
                _cache[escopo] = existente;
                await ReplayAsync(context, existente);
                return;
            }

            var corpoOriginal = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await _next(context);
            }
            finally
            {
                context.Response.Body = corpoOriginal;
            }

            buffer.Position = 0;
            var corpo = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var expiraEm = DateTime.UtcNow.AddHours(TtlHoras);
                _cache[escopo] = new RespostaCacheada(context.Response.StatusCode, corpo, expiraEm);

                try
                {
                    db.IdempotencyRecords.Add(new IdempotencyRecord
                    {
                        ClinicaId = tenantId,
                        Key = key,
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value ?? string.Empty,
                        StatusCode = context.Response.StatusCode,
                        ResponseBody = corpo,
                        ExpiresAtUtc = expiraEm,
                    });
                    await db.SaveChangesAsync(context.RequestAborted);
                    await LimparExpiradosAsync(db, tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao persistir idempotência — degradado para execução direta.");
                }
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(corpoOriginal);
        }
        finally
        {
            semaforo.Release();
        }
    }

    private static async Task ReplayAsync(HttpContext context, RespostaCacheada resposta)
    {
        context.Response.StatusCode = resposta.StatusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(resposta.Body);
    }

    private static Task LimparExpiradosAsync(TenantDbContext db, Guid tenantId) =>
        db.IdempotencyRecords
            .Where(r => r.ClinicaId == tenantId && r.ExpiresAtUtc <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
}