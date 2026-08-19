using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Financeiro;
using Clinica.API.Middlewares;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[RequireTenant]
public sealed class FinanceiroController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinanceiroController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("financeiro/dashboard")]
    public Task<ActionResult<DashboardFinanceiroResponse>> Dashboard([FromQuery] string competencia, CancellationToken ct) =>
        ResponderAsync(new ObterDashboardQuery(competencia), ct);

    [HttpGet("mensalidades")]
    public Task<ActionResult<IReadOnlyList<MensalidadeResponse>>> ListarMensalidades(
        [FromQuery] string? competencia,
        [FromQuery] StatusMensalidade? status,
        CancellationToken ct) =>
        ResponderAsync(new ListarMensalidadesQuery(competencia, status), ct);

    [HttpPost("mensalidades")]
    public Task<ActionResult<Guid>> GerarMensalidade(GerarMensalidadeCommand command, CancellationToken ct) =>
        ResponderAsync(command, ct);

    [HttpPost("mensalidades/{id:guid}/pagar")]
    public Task<IActionResult> PagarMensalidade(Guid id, CancellationToken ct) =>
        NoContentAsync(new RegistrarPagamentoMensalidadeCommand(id), ct);

    [HttpGet("contas-pagar")]
    public Task<ActionResult<IReadOnlyList<ContaPagarResponse>>> ListarContas(
        [FromQuery] StatusContaPagar? status,
        CancellationToken ct) =>
        ResponderAsync(new ListarContasPagarQuery(status), ct);

    [HttpPost("contas-pagar")]
    public Task<ActionResult<Guid>> CadastrarConta(CadastrarContaPagarCommand command, CancellationToken ct) =>
        ResponderAsync(command, ct);

    [HttpPost("contas-pagar/{id:guid}/baixar")]
    public Task<IActionResult> BaixarConta(Guid id, CancellationToken ct) =>
        NoContentAsync(new BaixarContaPagarCommand(id), ct);

    private async Task<ActionResult<T>> ResponderAsync<T>(IRequest<T> request, CancellationToken ct)
    {
        var resultado = await _mediator.Send(request, ct);
        return Ok(resultado);
    }

    private async Task<IActionResult> NoContentAsync(IRequest request, CancellationToken ct)
    {
        await _mediator.Send(request, ct);
        return NoContent();
    }
}