using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Senha) : IRequest<LoginResponse>;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    Guid TenantId,
    string TenantNome,
    Guid UsuarioId,
    string Nome,
    string Papel);

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _jwt;

    public LoginCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantService tenant,
        IPasswordHasher passwordHasher,
        ITokenService jwt)
    {
        _db = db;
        _tenant = tenant;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        if (_tenant.TenantId is null)
            throw new UnauthorizedException("Tenant não identificado: envie o header X-Tenant-Id.");

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(
                u => u.Email == request.Email && u.Ativo,
                ct);

        if (usuario is null || !_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
            throw new UnauthorizedException("E-mail ou senha inválidos.");

        var token = _jwt.CreateToken(usuario.Id, usuario.Email, usuario.ClinicaId, _tenant.TenantName ?? "");

        usuario.UltimoLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new LoginResponse(
            token,
            DateTime.UtcNow.AddHours(8),
            usuario.ClinicaId,
            _tenant.TenantName ?? string.Empty,
            usuario.Id,
            usuario.Nome,
            usuario.Papel.ToString());
    }
}