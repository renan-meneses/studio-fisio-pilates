using Clinica.Application.Features.Prontuario;
using Clinica.API.Middlewares;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/prontuarios")]
[Authorize]
[RequireTenant]
public sealed class ProntuariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProntuariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("paciente/{pacienteId:guid}")]
    public Task<ActionResult<ProntuarioResponse>> ObterPorPaciente(Guid pacienteId, CancellationToken ct) =>
        ResponderAsync(new ObterProntuarioPorPacienteQuery(pacienteId), ct);

    [HttpPost]
    public Task<ActionResult<Guid>> Abrir(AbrirProntuarioCommand command, CancellationToken ct) =>
        ResponderAsync(command, ct);

    [HttpPost("{prontuarioId:guid}/evolucoes")]
    public Task<ActionResult<Guid>> AdicionarEvolucao(Guid prontuarioId, AdicionarEvolucaoCommand command, CancellationToken ct) =>
        ResponderAsync(command with { ProntuarioId = prontuarioId }, ct);

    [HttpGet("{prontuarioId:guid}/evolucoes")]
    public Task<ActionResult<IReadOnlyList<EvolucaoResponse>>> ListarEvolucoes(Guid prontuarioId, CancellationToken ct) =>
        ResponderAsync(new ListarEvolucoesQuery(prontuarioId), ct);

    private async Task<ActionResult<T>> ResponderAsync<T>(IRequest<T> request, CancellationToken ct)
    {
        var result = await _mediator.Send(request, ct);
        return Ok(result);
    }
}