using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

public class Paciente : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Sobrenome { get; set; }

    public string? CPF { get; set; }

    public DateTime DataNascimento { get; set; }

    public Sexo Sexo { get; set; }

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public string? Endereco { get; set; }

    public string? Indicacao { get; set; }

    public string? Observacoes { get; set; }

    public StatusPaciente Status { get; set; } = StatusPaciente.Ativo;

    public DateTime? DataPrimeiraSessao { get; set; }

    public Guid? PlanoId { get; set; }

    public Plano? Plano { get; set; }
}