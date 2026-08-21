using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WaitlistEntryEntity = Clinica.Domain.Entities.WaitlistEntry;

namespace Clinica.Application.Features.Turma;

public sealed record EntrarWaitlistCommand(Guid TurmaId, Guid PacienteId) : IRequest<Guid>;

public sealed record SairWaitlistCommand(Guid EntradaId) : IRequest;

public sealed record ListarWaitlistQuery(Guid TurmaId)
    : IRequest<IReadOnlyList<WaitlistResponse>>;

public sealed record WaitlistResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    DateTime EntradaEm);

public sealed class EntrarWaitlistCommandValidator : AbstractValidator<EntrarWaitlistCommand>
{
    public EntrarWaitlistCommandValidator()
    {
        RuleFor(r => r.TurmaId).NotEmpty();
        RuleFor(r => r.PacienteId).NotEmpty();
    }
}

public sealed class SairWaitlistCommandValidator : AbstractValidator<SairWaitlistCommand>
{
    public SairWaitlistCommandValidator()
    {
        RuleFor(r => r.EntradaId).NotEmpty();
    }
}

/// <summary>
/// Fila de espera de turma: entrada idempotente por (turma, paciente),
/// saída lógica e listagem na ordem de chegada.
/// </summary>
public sealed class EntrarWaitlistCommandHandler : IRequestHandler<EntrarWaitlistCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public EntrarWaitlistCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(EntrarWaitlistCommand request, CancellationToken ct)
    {
        _ = await _db.Turmas
            .FirstOrDefaultAsync(t => t.Id == request.TurmaId, ct)
            ?? throw new NotFoundException("Turma não encontrada.");

        _ = await _db.Pacientes
            .FirstOrDefaultAsync(p => p.Id == request.PacienteId, ct)
            ?? throw new NotFoundException("Paciente não encontrado.");

        var existente = await _db.WaitlistEntries
            .FirstOrDefaultAsync(e =>
                e.TurmaId == request.TurmaId &&
                e.PacienteId == request.PacienteId &&
                e.Ativo, ct);

        if (existente is not null)
            return existente.Id;

        var entrada = new WaitlistEntryEntity
        {
            TurmaId = request.TurmaId,
            PacienteId = request.PacienteId,
        };

        await _db.WaitlistEntries.AddAsync(entrada, ct);
        await _db.SaveChangesAsync(ct);

        return entrada.Id;
    }
}

public sealed class SairWaitlistCommandHandler : IRequestHandler<SairWaitlistCommand>
{
    private readonly IApplicationDbContext _db;

    public SairWaitlistCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(SairWaitlistCommand request, CancellationToken ct)
    {
        var entrada = await _db.WaitlistEntries
            .FirstOrDefaultAsync(e => e.Id == request.EntradaId && e.Ativo, ct)
            ?? throw new NotFoundException("Entrada da lista de espera não encontrada.");

        entrada.Ativo = false;
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class ListarWaitlistQueryHandler
    : IRequestHandler<ListarWaitlistQuery, IReadOnlyList<WaitlistResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarWaitlistQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WaitlistResponse>> Handle(ListarWaitlistQuery request, CancellationToken ct)
    {
        var entradas = await _db.WaitlistEntries
            .Where(e => e.TurmaId == request.TurmaId && e.Ativo)
            .Include(e => e.Paciente)
            .AsNoTracking()
            .OrderBy(e => e.CreatedAt)
            .Select(e => new WaitlistResponse(
                e.Id,
                e.PacienteId,
                e.Paciente!.Nome,
                e.CreatedAt))
            .ToListAsync(ct);

        return entradas;
    }
}
