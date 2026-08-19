using Clinica.Application.Features.Clinicas;
using Clinica.API.Middlewares;
using Clinica.CrossCutting.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

/// <summary>Onboarding: criação de clínica (tenant) e usuário administrador.</summary>
[ApiController]
[Route("api/clinicas")]
public sealed class ClinicasController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClinicasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("onboarding")]
    [AllowAnonymous]
    public async Task<ActionResult<Guid>> Onboarding(CriarClinicaCommand command, CancellationToken ct)
    {
        var clinicaId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Onboarding), new { id = clinicaId }, new { clinicaId });
    }
}