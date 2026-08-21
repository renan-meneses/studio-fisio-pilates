using Clinica.API.Middlewares;
using Clinica.Application.Features.Relatorios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize]
[RequireTenant]
public sealed class RelatoriosController : ControllerBase
{
    private readonly IMediator _mediator;

    public RelatoriosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Cartões do dashboard: pacientes ativos, agenda de hoje, receita do mês e inadimplência.</summary>
    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoDashboardResponse>> Resumo(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ResumoDashboardQuery(), ct));
    }

    /// <summary>Faturamento por competência (receita recebida x previsto) para o gráfico mensal.</summary>
    [HttpGet("faturamento")]
    public async Task<ActionResult<IReadOnlyList<FaturamentoItem>>> Faturamento(
        [FromQuery] int meses = 6,
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new FaturamentoQuery(meses), ct));
    }

    /// <summary>Ocupação diária da agenda (total, realizados e faltas) para o período.</summary>
    [HttpGet("ocupacao")]
    public async Task<ActionResult<IReadOnlyList<OcupacaoDia>>> Ocupacao(
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new OcupacaoQuery(dias), ct));
    }

    /// <summary>Sessões realizadas com maior receita, agrupadas por tipo de sessão.</summary>
    [HttpGet("top-sessoes")]
    public async Task<ActionResult<IReadOnlyList<TopSessaoItem>>> TopSessoes(
        [FromQuery] int top = 5,
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new TopSessoesQuery(top), ct));
    }
}
