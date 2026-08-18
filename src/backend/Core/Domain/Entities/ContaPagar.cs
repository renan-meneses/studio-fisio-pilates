using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

public class ContaPagar : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public string Fornecedor { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateTime DataVencimento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public TipoCusto TipoCusto { get; set; } = TipoCusto.Variavel;

    public StatusContaPagar Status { get; set; } = StatusContaPagar.EmAberto;
}