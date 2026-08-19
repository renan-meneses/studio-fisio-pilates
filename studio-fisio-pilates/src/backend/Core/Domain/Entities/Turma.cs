using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

/// <summary>Turma de pilates (ex.: "Turma Segunda 18h") com horários semanais.</summary>
public class Turma : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoSessao TipoSessao { get; set; } = TipoSessao.PilatesSolo;

    public Guid? ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }

    public bool Ativo { get; set; } = true;

    public ICollection<TurmaHorario> Horarios { get; set; } = new List<TurmaHorario>();
}

/// <summary>Horário semanal de uma turma (dia da semana + faixa de horário).</summary>
public class TurmaHorario : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid TurmaId { get; set; }

    public Turma? Turma { get; set; }

    /// <summary>1 = Segunda-feira … 7 = Domingo.</summary>
    public int DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFim { get; set; }
}