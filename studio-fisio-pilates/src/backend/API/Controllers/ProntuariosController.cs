using Clinica.Application.Features.Prontuario;
using Clinica.API.Middlewares;
using Clinica.CrossCutting.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

/// <summary>
/// Cadastro de pacientes é administrativo (qualquer usuário autenticado).
/// Prontuário e evoluções são dados clínicos: exigem a policy <see cref="AuthorizationPolicies.PepAccess"/>.
/// </summary>
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

    [HttpGet("pacientes")]
    public async Task<ActionResult<IReadOnlyList<PacienteResumoResponse>>> ListarPacientes(
        [FromQuery] string? termo,
        CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarPacientesQuery(termo), ct));
    }

    [HttpPost("pacientes")]
    public async Task<ActionResult<Guid>> CriarPaciente(CriarPacienteCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(ListarPacientes), new { }, new { id });
    }

    [HttpGet("paciente/{pacienteId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PepAccess)]
    public Task<ActionResult<ProntuarioResponse>> ObterPorPaciente(Guid pacienteId, CancellationToken ct) =>
        ResponderAsync(new ObterProntuarioPorPacienteQuery(pacienteId), ct);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PepAccess)]
    public Task<ActionResult<Guid>> Abrir(AbrirProntuarioCommand command, CancellationToken ct) =>
        ResponderAsync(command, ct);

    [HttpPost("{prontuarioId:guid}/evolucoes")]
    [Authorize(Policy = AuthorizationPolicies.PepAccess)]
    public Task<ActionResult<Guid>> AdicionarEvolucao(Guid prontuarioId, AdicionarEvolucaoCommand command, CancellationToken ct) =>
        ResponderAsync(command with { ProntuarioId = prontuarioId }, ct);

    [HttpGet("{prontuarioId:guid}/evolucoes")]
    [Authorize(Policy = AuthorizationPolicies.PepAccess)]
    public Task<ActionResult<IReadOnlyList<EvolucaoResponse>>> ListarEvolucoes(Guid prontuarioId, CancellationToken ct) =>
        ResponderAsync(new ListarEvolucoesQuery(prontuarioId), ct);

    private async Task<ActionResult<T>> ResponderAsync<T>(IRequest<T> request, CancellationToken ct)
    {
        var result = await _mediator.Send(request, ct);
        return Ok(result);
    }
}