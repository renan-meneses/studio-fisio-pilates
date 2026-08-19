using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

public class Ponto : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }

    public DateTime Data { get; set; }

    public TimeSpan? Entrada { get; set; }

    public TimeSpan? Saida { get; set; }

    public TimeSpan? AlmocoInicio { get; set; }

    public TimeSpan? AlmocoFim { get; set; }

    public TimeSpan? HorasExtras { get; set; }

    public string? Observacoes { get; set; }

    /// <summary>Horas trabalhadas no dia, descontando o intervalo do almoço.</summary>
    public TimeSpan HorasTrabalhadas()
    {
        if (Entrada is null || Saida is null)
            return TimeSpan.Zero;

        var total = Saida.Value - Entrada.Value;
        if (AlmocoInicio is not null && AlmocoFim is not null)
            total -= AlmocoFim.Value - AlmocoInicio.Value;
        return total > TimeSpan.Zero ? total : TimeSpan.Zero;
    }
}