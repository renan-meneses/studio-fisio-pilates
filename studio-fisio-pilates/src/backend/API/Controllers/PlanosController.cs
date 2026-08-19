using Clinica.API.Middlewares;
using Clinica.Application.Features.Plano;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/planos")]
[Authorize]
[RequireTenant]
public sealed class PlanosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlanosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanoResponse>>> Listar(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarPlanosQuery(), ct));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Criar(CriarPlanoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Listar), new { }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarPlanoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/servicos")]
    public async Task<IActionResult> AdicionarServico(Guid id, AdicionarServicoAoPlanoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { PlanoId = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/servicos/{servicoId:guid}")]
    public async Task<IActionResult> RemoverServico(Guid id, Guid servicoId, CancellationToken ct)
    {
        await _mediator.Send(new RemoverServicoDoPlanoCommand(id, servicoId), ct);
        return NoContent();
    }
}