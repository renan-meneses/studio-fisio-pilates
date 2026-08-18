using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

public class FolhaSalarial : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public Guid ProfissionalId { get; set; }

    public Profissional? Profissional { get; set; }

    /// <summary>Competência no formato yyyy-MM.</summary>
    public string Competencia { get; set; } = string.Empty;

    public decimal ValorBruto { get; set; }

    public decimal Descontos { get; set; }

    public decimal ValorLiquido => ValorBruto - Descontos;

    public int DiasTrabalhados { get; set; }

    public int Faltas { get; set; }

    public StatusFolha Status { get; set; } = StatusFolha.Rascunho;

    public void Processar(decimal descontos)
    {
        Descontos = descontos;
        Status = StatusFolha.Processada;
    }
}