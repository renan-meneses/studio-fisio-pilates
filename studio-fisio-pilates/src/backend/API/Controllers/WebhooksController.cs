using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Financeiro;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Clinica.API.Controllers;

/// <summary>Opções de autenticação de webhooks (HMAC-SHA256).</summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    /// <summary>Segredo compartilhado com o provedor para assinar o corpo.</summary>
    public string SecretKey { get; set; } = string.Empty;
}

/// <summary>
/// Recepção de eventos de pagamento dos provedores (PSPs).
///
/// Autenticação: header X-Assinatura = HMAC-SHA256 hex do corpo bruto com o
/// segredo compartilhado (Webhooks:SecretKey). Sem JWT — a assinatura é a
/// prova de origem.
///
/// Contrato do provedor simulado:
/// <code>
/// POST /api/webhooks/pagamentos/simulado
/// X-Assinatura: &lt;hex hmac&gt;
/// {
///   "eventoId":  "evt_abc123",          // único no provedor (dedupe)
///   "tipo":      "pagamento.confirmado",
///   "cobrancaId":"&lt;guid da cobranca&gt;",
///   "pagoEmUtc": "2026-08-21T12:00:00Z"
/// }
/// </code>
///
/// Replays do mesmo eventoId são ACKed com 200 sem reprocessar; falhas de
/// processamento ficam registradas em eventos_pagamento_webhook para
/// reconciliação.
/// </summary>
[ApiController]
[Route("api/webhooks/pagamentos")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly WebhookOptions _opcoes;

    public WebhooksController(IMediator mediator, IOptions<WebhookOptions> opcoes)
    {
        _mediator = mediator;
        _opcoes = opcoes.Value;
    }

    [HttpPost("{provedor}")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceberPagamento(string provedor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.SecretKey))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Webhook não configurado." });

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var corpo = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var assinatura = Request.Headers["X-Assinatura"].FirstOrDefault();
        if (!AssinaturaValida(corpo, assinatura))
            return Unauthorized(new { error = "Assinatura inválida." });

        JsonElement json;
        try
        {
            json = JsonSerializer.Deserialize<JsonElement>(corpo);
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Payload não é um JSON válido." });
        }

        if (!TryLer(json, "eventoId", out var eventoId) ||
            !TryLer(json, "tipo", out var tipo) ||
            !json.TryGetProperty("cobrancaId", out var cobrancaEl) ||
            !Guid.TryParse(cobrancaEl.GetString(), out var cobrancaId))
        {
            return BadRequest(new { error = "Payload requer eventoId, tipo e cobrancaId." });
        }

        DateTime? pagoEmUtc = null;
        if (json.TryGetProperty("pagoEmUtc", out var pagoEl) &&
            DateTime.TryParse(pagoEl.GetString(), out var pago))
            pagoEmUtc = pago.ToUniversalTime();

        var resultado = await _mediator.Send(new ProcessarWebhookPagamentoCommand(
            provedor, eventoId, tipo, cobrancaId, pagoEmUtc, corpo), ct);

        // ACK sempre 200: dedupe/falha de negócio são registradas internamente.
        return Ok(resultado);
    }

    private bool AssinaturaValida(string corpo, string? assinatura)
    {
        if (string.IsNullOrWhiteSpace(assinatura))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_opcoes.SecretKey));
        var esperado = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(corpo)));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(esperado),
            Encoding.UTF8.GetBytes(assinatura.ToUpperInvariant()));
    }

    private static bool TryLer(JsonElement json, string propriedade, out string valor)
    {
        valor = string.Empty;
        if (json.TryGetProperty(propriedade, out var el) && el.ValueKind == JsonValueKind.String)
        {
            valor = el.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(valor);
        }

        return false;
    }
}