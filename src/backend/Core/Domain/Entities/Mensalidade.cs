using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

public class Mensalidade : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid PacienteId { get; set; }

    public Paciente? Paciente { get; set; }

    /// <summary>Competência no formato yyyy-MM (ex.: 2026-08).</summary>
    public string Competencia { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateTime DataVencimento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public StatusMensalidade Status { get; set; } = StatusMensalidade.Pendente;

    /// <summary>Liquida a mensalidade registrando a data de pagamento.</summary>
    public void RegistrarPagamento(DateTime dataPagamento)
    {
        DataPagamento = dataPagamento;
        Status = StatusMensalidade.Paga;
    }
}