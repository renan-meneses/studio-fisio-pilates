using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

/// <summary>Raiz da agregação multitenant: o estúdio/clínica dono de todos os dados.</summary>
public class Clinica : BaseEntity, IAggregateRoot
{
    public string Nome { get; set; } = string.Empty;

    public string CNPJ { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefone { get; set; }

    public string? Endereco { get; set; }

    public PlanoContratacao Plano { get; set; } = PlanoContratacao.Basico;

    public bool Ativa { get; set; } = true;
}

public enum PlanoContratacao
{
    Basico = 1,
    Profissional = 2,
    Enterprise = 3,
}