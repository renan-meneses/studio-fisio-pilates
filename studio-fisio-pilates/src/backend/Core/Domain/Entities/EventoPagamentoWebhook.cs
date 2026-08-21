using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

/// <summary>
/// Evento recebido via webhook do provedor de pagamento. Deduplicado por
/// (ClinicaId, Provedor, EventoId): replays do provedor são ACKed sem
/// reprocessar. Falhas de processamento ficam registradas para reconciliação.
/// </summary>
public class EventoPagamentoWebhook : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public string Provedor { get; set; } = string.Empty;

    /// <summary>Identificador único do evento no provedor.</summary>
    public string EventoId { get; set; } = string.Empty;

    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>Payload bruto recebido (auditoria/reprocessamento manual).</summary>
    public string Payload { get; set; } = string.Empty;

    public bool Processado { get; set; }

    public DateTime? ProcessadoEmUtc { get; set; }

    public string? ErroProcessamento { get; set; }

    public void MarcarProcessado()
    {
        Processado = true;
        ProcessadoEmUtc = DateTime.UtcNow;
        ErroProcessamento = null;
    }

    public void MarcarFalha(string erro)
    {
        Processado = false;
        ErroProcessamento = erro;
    }
}