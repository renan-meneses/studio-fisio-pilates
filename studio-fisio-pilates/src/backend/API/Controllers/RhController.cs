using Clinica.Application.Features.Rh;
using Clinica.API.Middlewares;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/rh")]
[Authorize]
[RequireTenant]
public sealed class RhController : ControllerBase
{
    private readonly IMediator _mediator;

    public RhController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("profissionais")]
    public async Task<ActionResult<IReadOnlyList<ProfissionalResumoResponse>>> ListarProfissionais(
        [FromQuery] string? termo,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarProfissionaisQuery(termo), ct));
    }

    [HttpPost("profissionais")]
    public async Task<ActionResult<Guid>> CriarProfissional(CriarProfissionalCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(ListarProfissionais), new { }, new { id });
    }

    [HttpGet("pontos")]
    public async Task<ActionResult<IReadOnlyList<PontoResponse>>> ListarPontos(
        [FromQuery] Guid? profissionalId,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarPontosQuery(profissionalId, de, ate), ct));
    }

    [HttpPost("pontos")]
    public async Task<ActionResult<Guid>> RegistrarPonto(RegistrarPontoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Ok(new { id });
    }

    [HttpGet("folha")]
    public async Task<ActionResult<FolhaResponse>> ObterFolha(
        [FromQuery] Guid profissionalId,
        [FromQuery] string competencia,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ObterFolhaQuery(profissionalId, competencia), ct));
    }

    [HttpPost("folha/calcular")]
    public async Task<ActionResult<Guid>> CalcularFolha(CalcularFolhaCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Ok(new { id });
    }
}