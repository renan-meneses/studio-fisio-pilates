using Clinica.API.Middlewares;
using Clinica.Application.Features.Turma;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/turmas")]
[Authorize]
[RequireTenant]
public sealed class TurmasController : ControllerBase
{
    private readonly IMediator _mediator;

    public TurmasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TurmaResponse>>> Listar(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarTurmasQuery(), ct));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Criar(CriarTurmaCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Listar), new { }, new { id });
    }

    [HttpPost("{id:guid}/horarios")]
    public async Task<IActionResult> AdicionarHorario(Guid id, AdicionarHorarioCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { TurmaId = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/horarios/{horarioId:guid}")]
    public async Task<IActionResult> RemoverHorario(Guid id, Guid horarioId, CancellationToken ct)
    {
        await _mediator.Send(new RemoverHorarioCommand(id, horarioId), ct);
        return NoContent();
    }
}