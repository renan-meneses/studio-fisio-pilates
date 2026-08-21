using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Auth;
using Clinica.Application.Common.Interfaces;
using Clinica.CrossCutting.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Autentica usuário e descobre o tenant da clínica no retorno (tenantId/tenantNome).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(LoginCommand command, CancellationToken ct)
    {
        var response = await _mediator.Send(command, ct);
        return Ok(response);
    }

    /// <summary>Retorna o tenant/usuário autenticado (diagnóstico de claims).</summary>
    [HttpGet("me")]
    public IActionResult Me()
    {
        var user = HttpContext.User;
        return Ok(new
        {
            userId = user.GetUserId(),
            email = user.GetEmail(),
            tenantId = user.GetTenantId(),
            papel = user.GetPapel(),
            authenticated = user.Identity?.IsAuthenticated ?? false,
        });
    }

    /// <summary>Atualiza a preferência de tema (Claro/Escuro) do usuário autenticado.</summary>
    [HttpPatch("tema")]
    public async Task<IActionResult> AtualizarTema(AtualizarTemaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { UsuarioId = User.GetUserId() ?? Guid.Empty }, ct);
        return NoContent();
    }
}