using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

public class ProntuarioEletronico : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public Guid PacienteId { get; set; }

    public Paciente? Paciente { get; set; }

    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;

    public bool Ativo { get; set; } = true;

    public ICollection<EvolucaoClinica> Evolucoes { get; set; } = new List<EvolucaoClinica>();
}