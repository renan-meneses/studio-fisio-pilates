using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Usuarios;

public sealed record UsuarioResponse(
    Guid Id,
    string Nome,
    string Email,
    string Papel,
    bool Ativo,
    DateTime? UltimoLogin,
    DateTime CreatedAt);

public sealed record ListarUsuariosQuery : IRequest<IReadOnlyList<UsuarioResponse>>;

public sealed class ListarUsuariosQueryHandler : IRequestHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarUsuariosQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UsuarioResponse>> Handle(ListarUsuariosQuery request, CancellationToken ct)
    {
        var usuarios = await _db.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nome)
            .ToListAsync(ct);

        return usuarios
            .Select(u => new UsuarioResponse(
                u.Id,
                u.Nome,
                u.Email,
                u.Papel.ToString(),
                u.Ativo,
                u.UltimoLogin,
                u.CreatedAt))
            .ToList();
    }
}

public sealed record CriarUsuarioCommand(string Nome, string Email, string Senha, string Papel) : IRequest<Guid>;

public sealed class CriarUsuarioCommandValidator : AbstractValidator<CriarUsuarioCommand>
{
    public CriarUsuarioCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(r => r.Senha).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(r => r.Papel).NotEmpty()
            .Must(p => Enum.TryParse<PapelUsuario>(p, true, out _))
            .WithMessage("Papel inválido. Use: Administrador, Atendente, Financeiro ou Profissional.");
    }
}

public sealed class CriarUsuarioCommandHandler : IRequestHandler<CriarUsuarioCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public CriarUsuarioCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CriarUsuarioCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existe = await _db.Usuarios.AnyAsync(u => u.Email == email, ct);
        if (existe)
            throw new BusinessRuleException("Já existe um usuário com este e-mail nesta clínica.");

        if (!Enum.TryParse<PapelUsuario>(request.Papel, true, out var papel))
            throw new BusinessRuleException("Papel inválido.");

        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = email,
            SenhaHash = _passwordHasher.Hash(request.Senha),
            Papel = papel,
            Ativo = true,
        };

        await _db.Usuarios.AddAsync(usuario, ct);
        await _db.SaveChangesAsync(ct);

        return usuario.Id;
    }
}

public sealed record AlterarStatusUsuarioCommand(Guid UsuarioId, Guid Id, bool Ativo) : IRequest;

public sealed class AlterarStatusUsuarioCommandHandler : IRequestHandler<AlterarStatusUsuarioCommand>
{
    private readonly IApplicationDbContext _db;

    public AlterarStatusUsuarioCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AlterarStatusUsuarioCommand request, CancellationToken ct)
    {
        if (!request.Ativo && request.Id == request.UsuarioId)
            throw new BusinessRuleException("Não é possível desativar o próprio usuário.");

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        usuario.Ativo = request.Ativo;
        await _db.SaveChangesAsync(ct);
    }
}

public sealed record RedefinirSenhaCommand(Guid Id, string NovaSenha) : IRequest;

public sealed class RedefinirSenhaCommandValidator : AbstractValidator<RedefinirSenhaCommand>
{
    public RedefinirSenhaCommandValidator()
    {
        RuleFor(r => r.NovaSenha).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class RedefinirSenhaCommandHandler : IRequestHandler<RedefinirSenhaCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public RedefinirSenhaCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(RedefinirSenhaCommand request, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha);
        await _db.SaveChangesAsync(ct);
    }
}
