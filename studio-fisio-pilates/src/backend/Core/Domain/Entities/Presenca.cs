using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

public class Presenca : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid AgendamentoId { get; set; }

    public Agendamento? Agendamento { get; set; }

    public DateTime? Entrada { get; set; }

    public DateTime? Saida { get; set; }

    public StatusPresenca Status { get; set; }

    public string? Observacoes { get; set; }
}