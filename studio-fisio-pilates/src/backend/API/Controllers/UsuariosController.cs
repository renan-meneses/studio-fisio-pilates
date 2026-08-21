using Clinica.API.Middlewares;
using Clinica.Application.Features.Usuarios;
using Clinica.CrossCutting.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(AuthorizationPolicies.AdminOnly)]
[RequireTenant]
public sealed class UsuariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsuariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista os usuários da clínica autenticada.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> Listar(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarUsuariosQuery(), ct));
    }

    /// <summary>Cria um usuário de acesso com senha inicial definida pelo administrador.</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Criar(CriarUsuarioCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Listar), new { }, new { id });
    }

    /// <summary>Ativa ou desativa um usuário (não é possível desativar a si mesmo).</summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, AlterarStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(
            new AlterarStatusUsuarioCommand(User.GetUserId() ?? Guid.Empty, id, request.Ativo),
            ct);
        return NoContent();
    }

    /// <summary>Redefine a senha de um usuário (fluxo administrativo).</summary>
    [HttpPatch("{id}/senha")]
    public async Task<IActionResult> RedefinirSenha(Guid id, RedefinirSenhaRequest request, CancellationToken ct)
    {
        await _mediator.Send(new RedefinirSenhaCommand(id, request.NovaSenha), ct);
        return NoContent();
    }
}

public sealed record AlterarStatusRequest(bool Ativo);

public sealed record RedefinirSenhaRequest(string NovaSenha);
