using System.Security.Cryptography;
using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Auth;

/// <summary>
/// Fluxo "esqueci minha senha":
///  1. Solicitação gera token de alta entropia, persiste apenas o SHA-256
///     e envia o valor bruto ao usuário (validade curta);
///  2. Redefinição consome o token (uso único) e troca o hash da senha.
/// A resposta da solicitação é sempre igual exista ou não o e-mail —
/// evita enumeração de usuários.
/// </summary>
public sealed record SolicitarRedefinicaoCommand(string Email) : IRequest;

public sealed class SolicitarRedefinicaoCommandValidator : AbstractValidator<SolicitarRedefinicaoCommand>
{
    public SolicitarRedefinicaoCommandValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(120);
    }
}

public sealed class SolicitarRedefinicaoCommandHandler : IRequestHandler<SolicitarRedefinicaoCommand>
{
    private static readonly TimeSpan Validade = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _db;
    private readonly INotificacaoService _notificacoes;

    public SolicitarRedefinicaoCommandHandler(IApplicationDbContext db, INotificacaoService notificacoes)
    {
        _db = db;
        _notificacoes = notificacoes;
    }

    public async Task Handle(SolicitarRedefinicaoCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email && u.Ativo, ct);

        if (usuario is null)
            return; // resposta idêntica ao caso existente (anti-enumeração)

        // Invalida solicitações anteriores ainda não consumidas.
        var pendentes = await _db.TokensRedefinicaoSenha
            .Where(t => t.UsuarioId == usuario.Id && t.UsadoEm == null)
            .ToListAsync(ct);
        foreach (var antigo in pendentes)
            antigo.UsadoEm = DateTime.UtcNow;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        await _db.TokensRedefinicaoSenha.AddAsync(new TokenRedefinicaoSenha
        {
            ClinicaId = usuario.ClinicaId,
            UsuarioId = usuario.Id,
            TokenHash = HashDe(token),
            ExpiraEm = DateTime.UtcNow.Add(Validade),
        }, ct);
        await _db.SaveChangesAsync(ct);

        await _notificacoes.EnviarAsync(new NotificacaoMensagem(
            usuario.ClinicaId,
            usuario.Id,
            usuario.Email,
            "Redefinição de senha",
            $"Use o token abaixo para redefinir sua senha (válido por 1 hora):\n\n{token}"),
            ct);
    }

    public static string HashDe(string token) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}

public sealed record RedefinirSenhaTokenCommand(string Email, string Token, string NovaSenha) : IRequest;

public sealed class RedefinirSenhaTokenCommandValidator : AbstractValidator<RedefinirSenhaTokenCommand>
{
    public RedefinirSenhaTokenCommandValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(r => r.Token).NotEmpty();
        RuleFor(r => r.NovaSenha).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class RedefinirSenhaTokenCommandHandler : IRequestHandler<RedefinirSenhaTokenCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public RedefinirSenhaTokenCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(RedefinirSenhaTokenCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email && u.Ativo, ct)
            ?? throw new BusinessRuleException("Token inválido ou expirado.");

        var hash = SolicitarRedefinicaoCommandHandler.HashDe(request.Token);

        var token = await _db.TokensRedefinicaoSenha
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash
                  && t.UsuarioId == usuario.Id
                  && t.UsadoEm == null
                  && t.ExpiraEm > DateTime.UtcNow,
                ct)
            ?? throw new BusinessRuleException("Token inválido ou expirado.");

        token.UsadoEm = DateTime.UtcNow;
        usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha);
        await _db.SaveChangesAsync(ct);
    }
}
