using Clinica.API.Middlewares;
using Clinica.Application.Features.Plano;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize]
[RequireTenant]
public sealed class ServicosController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServicoResponse>>> Listar(
        [FromQuery] string? termo,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarServicosQuery(termo), ct));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Criar(CriarServicoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Listar), new { }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarServicoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
}