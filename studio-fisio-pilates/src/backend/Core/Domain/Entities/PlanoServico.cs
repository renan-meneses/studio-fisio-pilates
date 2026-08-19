using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

/// <summary>Plano comercial da clínica (ex.: Pilates 2x por semana, Fisioterapia mensal).</summary>
public class Plano : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public string? Descricao { get; set; }

    public bool Ativo { get; set; } = true;

    public ICollection<PlanoServico> PlanoServicos { get; set; } = new List<PlanoServico>();
}

/// <summary>Serviço oferecido (ex.: Fisioterapia individual, Pilates em grupo).</summary>
public class Servico : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public decimal Valor { get; set; }

    public bool Ativo { get; set; } = true;

    public ICollection<PlanoServico> PlanoServicos { get; set; } = new List<PlanoServico>();
}

/// <summary>Associação plano × serviço (N:N).</summary>
public class PlanoServico : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public Guid PlanoId { get; set; }

    public Plano? Plano { get; set; }

    public Guid ServicoId { get; set; }

    public Servico? Servico { get; set; }
}