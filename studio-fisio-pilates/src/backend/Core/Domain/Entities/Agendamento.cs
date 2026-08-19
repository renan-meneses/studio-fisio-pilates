using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

public class Agendamento : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public Guid PacienteId { get; set; }

    public Guid ProfissionalId { get; set; }

    public Paciente? Paciente { get; set; }

    public Profissional? Profissional { get; set; }

    public DateTime DataHoraInicio { get; set; }

    public DateTime DataHoraFim { get; set; }

    public TipoSessao TipoSessao { get; set; } = TipoSessao.PilatesSolo;

    public TipoAula TipoAula { get; set; } = TipoAula.Individual;

    public Guid? TurmaId { get; set; }

    public Turma? Turma { get; set; }

    public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;

    /// <summary>Valor cobrado na sessão (0 quando incluso na mensalidade).</summary>
    public decimal ValorSessao { get; set; }

    public string? Observacoes { get; set; }

    public Presenca? Presenca { get; set; }

    /// <summary>Regra de negócio: profissionais não podem ter sessões sobrepostas.</summary>
    public bool Sobrepoe(Agendamento outro) =>
        ProfissionalId == outro.ProfissionalId &&
        (DataHoraInicio, DataHoraFim) is var (inicio, fim) &&
        inicio < outro.DataHoraFim && fim > outro.DataHoraInicio;
}