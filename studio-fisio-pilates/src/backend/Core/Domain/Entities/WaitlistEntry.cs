using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

/// <summary>
/// Aluno na fila de espera de uma turma. Uma entrada ativa por
/// (turma, paciente); a ordem da fila é a ordem de criação.
/// </summary>
public class WaitlistEntry : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public Guid TurmaId { get; set; }

    public Guid PacienteId { get; set; }

    public Paciente? Paciente { get; set; }

    public bool Ativo { get; set; } = true;
}
