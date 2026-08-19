using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Aluno;

public sealed record AlunoResponse(
    Guid Id,
    string Nome,
    string? Sobrenome,
    string NomeCompleto,
    string? Endereco,
    string? Telefone,
    string? Email,
    DateTime? DataNascimento,
    Guid? PlanoId,
    string? PlanoNome,
    bool Ativo);

public sealed record ListarAlunosQuery(string? Termo) : IRequest<IReadOnlyList<AlunoResponse>>;

public sealed record CriarAlunoCommand(
    string Nome,
    string? Sobrenome,
    string? Endereco,
    string? Telefone,
    string? Email,
    DateTime? DataNascimento,
    Guid? PlanoId) : IRequest<Guid>;

public sealed class CriarAlunoCommandValidator : AbstractValidator<CriarAlunoCommand>
{
    public CriarAlunoCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Sobrenome).MaximumLength(150);
        RuleFor(r => r.Endereco).MaximumLength(255);
        RuleFor(r => r.Telefone).MaximumLength(20);
        RuleFor(r => r.Email).EmailAddress().When(r => !string.IsNullOrWhiteSpace(r.Email));
        RuleFor(r => r.Email).MaximumLength(120);
    }
}

public sealed class ListarAlunosQueryHandler : IRequestHandler<ListarAlunosQuery, IReadOnlyList<AlunoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarAlunosQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AlunoResponse>> Handle(ListarAlunosQuery request, CancellationToken ct)
    {
        var query = _db.Pacientes
            .Include(p => p.Plano)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Termo))
            query = query.Where(p =>
                p.Nome.ToLower().Contains(request.Termo.ToLower())
                || (p.Sobrenome != null && p.Sobrenome.ToLower().Contains(request.Termo.ToLower())));

        return (await query.OrderBy(p => p.Nome).ToListAsync(ct))
            .Select(p => new AlunoResponse(
                p.Id,
                p.Nome,
                p.Sobrenome,
                string.Join(' ', new[] { p.Nome, p.Sobrenome }.Where(s => !string.IsNullOrWhiteSpace(s))),
                p.Endereco,
                p.Telefone,
                p.Email,
                p.DataNascimento == default ? null : p.DataNascimento,
                p.PlanoId,
                p.Plano?.Nome,
                p.Status == StatusPaciente.Ativo))
            .ToList();
    }
}

public sealed class CriarAlunoCommandHandler : IRequestHandler<CriarAlunoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CriarAlunoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CriarAlunoCommand request, CancellationToken ct)
    {
        if (request.PlanoId.HasValue)
        {
            var planoExiste = await _db.Planos.AnyAsync(p => p.Id == request.PlanoId.Value, ct);
            if (!planoExiste)
                throw new Clinica.Application.Common.Exceptions.NotFoundException("Plano não encontrado.");
        }

        var aluno = new Paciente
        {
            Nome = request.Nome,
            Sobrenome = request.Sobrenome,
            Endereco = request.Endereco,
            Telefone = request.Telefone,
            Email = request.Email,
            DataNascimento = request.DataNascimento ?? default,
            PlanoId = request.PlanoId,
            Status = StatusPaciente.Ativo,
        };

        await _db.Pacientes.AddAsync(aluno, ct);
        await _db.SaveChangesAsync(ct);

        return aluno.Id;
    }
}