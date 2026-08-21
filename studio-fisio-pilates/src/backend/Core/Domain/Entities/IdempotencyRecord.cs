using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

/// <summary>
/// Registro de idempotência de requisições HTTP (Idempotency-Key).
/// Replays da mesma chave + método + rota retornam a resposta já
/// processada sem duplicar efeitos colaterais.
/// </summary>
public class IdempotencyRecord : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    /// <summary>Chave fornecida pelo cliente no header Idempotency-Key.</summary>
    public string Key { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    /// <summary>Rota sem query string (ex.: /api/prontuarios/pacientes).</summary>
    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string ResponseBody { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Sobrescreve os dados com uma nova resposta (mesma chave reutilizada).</summary>
    public void Atualizar(int statusCode, string responseBody, DateTime expiresAtUtc)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        ExpiresAtUtc = expiresAtUtc;
    }
}