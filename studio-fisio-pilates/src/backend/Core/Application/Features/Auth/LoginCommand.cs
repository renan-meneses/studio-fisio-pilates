using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using FluentValidation;
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
    string Papel,
    string Tema);

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _jwt;

    public LoginCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor,
        IPasswordHasher passwordHasher,
        ITokenService jwt)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Email == request.Email && u.Ativo,
                ct);

        if (usuario is null || !_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
            throw new UnauthorizedException("E-mail ou senha inválidos.");

        var clinica = await _db.Clinicas
            .FirstOrDefaultAsync(c => c.Id == usuario.ClinicaId, ct)
            ?? throw new UnauthorizedException("Clínica do usuário não encontrada.");

        _tenantAccessor.Set(clinica.Id, clinica.Nome);

        var emissao = _jwt.CreateToken(usuario.Id, usuario.Email, usuario.ClinicaId, clinica.Nome, usuario.Papel.ToString());

        usuario.UltimoLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new LoginResponse(
            emissao.Token,
            emissao.ExpiresAt,
            clinica.Id,
            clinica.Nome,
            usuario.Id,
            usuario.Nome,
            usuario.Papel.ToString(),
            usuario.Tema.ToString());
    }
}

public sealed record AtualizarTemaCommand(Guid UsuarioId, string Tema) : IRequest;

public sealed class AtualizarTemaCommandHandler : IRequestHandler<AtualizarTemaCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public AtualizarTemaCommandHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(AtualizarTemaCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<TemaPreferencia>(request.Tema, true, out var tema))
            throw new BusinessRuleException("Tema inválido. Use 'Claro' ou 'Escuro'.");

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(
                u => u.Id == request.UsuarioId && (_tenant.TenantId == null || u.ClinicaId == _tenant.TenantId),
                ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        usuario.Tema = tema;
        await _db.SaveChangesAsync(ct);
    }
}

public sealed record AlterarSenhaPropriaCommand(Guid UsuarioId, string SenhaAtual, string NovaSenha) : IRequest;

public sealed class AlterarSenhaPropriaCommandValidator : AbstractValidator<AlterarSenhaPropriaCommand>
{
    public AlterarSenhaPropriaCommandValidator()
    {
        RuleFor(r => r.SenhaAtual).NotEmpty();
        RuleFor(r => r.NovaSenha).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class AlterarSenhaPropriaCommandHandler : IRequestHandler<AlterarSenhaPropriaCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AlterarSenhaPropriaCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(AlterarSenhaPropriaCommand request, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == request.UsuarioId, ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (!_passwordHasher.Verify(request.SenhaAtual, usuario.SenhaHash))
            throw new BusinessRuleException("Senha atual incorreta.");

        usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha);
        await _db.SaveChangesAsync(ct);
    }
}