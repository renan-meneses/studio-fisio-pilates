using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

public class Profissional : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string CPF { get; set; } = string.Empty;

    /// <summary>Registro no conselho de classe (ex.: CREFITO, COFFITO).</summary>
    public string? RegistroProfissional { get; set; }

    /// <summary>Especialidades separadas por vírgula (ex.: "Fisioterapia, Pilates Clínico").</summary>
    public string Especialidades { get; set; } = string.Empty;

    public string Cargo { get; set; } = "Fisioterapeuta";

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public decimal SalarioBase { get; set; }

    public DateTime DataAdmissao { get; set; } = DateTime.UtcNow;

    public DateTime? DataDemissao { get; set; }

    public bool Ativo { get; set; } = true;
}