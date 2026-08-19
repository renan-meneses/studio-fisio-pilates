using Clinica.Application.Features.Agendamento;
using Clinica.API.Middlewares;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/agendamentos")]
[Authorize]
[RequireTenant]
public sealed class AgendamentosController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgendamentosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgendamentoResponse>>> Listar(
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] Guid? profissionalId,
        [FromQuery] StatusAgendamento? status,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarAgendamentosQuery(de, ate, profissionalId, status), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgendamentoResponse>> Obter(Guid id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ObterAgendamentoQuery(id), ct));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Criar(CriarAgendamentoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Guid>> Atualizar(Guid id, AtualizarAgendamentoCommand command, CancellationToken ct)
    {
        var atualizado = command with { Id = id };
        return Ok(await _mediator.Send(atualizado, ct));
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancelarAgendamentoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ConfirmarAgendamentoCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/presenca")]
    public async Task<ActionResult<Guid>> RegistrarPresenca(Guid id, RegistrarPresencaCommand command, CancellationToken ct)
    {
        return Ok(await _mediator.Send(command with { AgendamentoId = id }, ct));
    }
}