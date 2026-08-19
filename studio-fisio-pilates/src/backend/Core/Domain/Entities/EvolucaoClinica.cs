using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

/// <summary>Ficha de evolução clínica vinculada a um prontuário.</summary>
public class EvolucaoClinica : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid ProntuarioId { get; set; }

    public ProntuarioEletronico? Prontuario { get; set; }

    public Guid ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }

    public DateTime Data { get; set; } = DateTime.UtcNow;

    public TipoEvolucao Tipo { get; set; } = TipoEvolucao.Evolucao;

    public string? QueixaPrincipal { get; set; }

    public string? Avaliacao { get; set; }

    public string? Conduta { get; set; }

    public string? Observacoes { get; set; }
}