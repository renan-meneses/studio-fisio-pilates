using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

/// <summary>
/// Cobrança emitida junto a um provedor de pagamento (Pix/boleto) para
/// liquidar uma mensalidade. A baixa acontece exclusivamente via webhook
/// do provedor, deduplicado por evento.
/// </summary>
public class Cobranca : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid MensalidadeId { get; set; }

    public TipoCobranca Tipo { get; set; }

    /// <summary>Identificador do provedor (ex.: "simulado").</summary>
    public string Provedor { get; set; } = string.Empty;

    /// <summary>Identificador da cobrança no provedor — único por provedor.</summary>
    public string ProvedorCobrancaId { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public StatusCobranca Status { get; set; } = StatusCobranca.Pendente;

    public string? PixCopiaECola { get; set; }

    public string? BoletoLinhaDigitavel { get; set; }

    public DateTime ExpiraEmUtc { get; set; }

    public DateTime? PagaEmUtc { get; set; }

    /// <summary>Liquida a cobrança (chamado apenas pelo processamento de webhook).</summary>
    public void MarcarPaga(DateTime pagaEmUtc)
    {
        if (Status != StatusCobranca.Pendente)
            return;

        Status = StatusCobranca.Paga;
        PagaEmUtc = pagaEmUtc;
    }

    public void Cancelar()
    {
        if (Status == StatusCobranca.Paga)
            return;

        Status = StatusCobranca.Cancelada;
    }
}