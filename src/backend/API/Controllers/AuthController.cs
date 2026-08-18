using Clinica.Application.Common.Exceptions;
using Clinica.Application.Features.Auth;
using Clinica.Application.Common.Interfaces;
using Clinica.CrossCutting.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>Autentica usuário do tenant (header X-Tenant-Id) e emite JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
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
            authenticated = user.Identity?.IsAuthenticated ?? false,
        });
    }
}