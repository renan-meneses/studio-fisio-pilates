using Clinica.API.Middlewares;
using Clinica.Application.Features.Aluno;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/alunos")]
[Authorize]
[RequireTenant]
public sealed class AlunosController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlunosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlunoResponse>>> Listar(
        [FromQuery] string? termo,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarAlunosQuery(termo), ct));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Criar(CriarAlunoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Listar), new { }, new { id });
    }
}