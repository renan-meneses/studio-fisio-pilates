using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Turma;

public sealed record TurmaHorarioResponse(
    Guid Id,
    int DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFim);

public sealed record TurmaResponse(
    Guid Id,
    string Nome,
    TipoSessao TipoSessao,
    Guid? ProfissionalId,
    string? ProfissionalNome,
    bool Ativo,
    IReadOnlyList<TurmaHorarioResponse> Horarios);

public sealed record HorarioRequest(int DiaSemana, TimeSpan HoraInicio, TimeSpan HoraFim);

public sealed record ListarTurmasQuery : IRequest<IReadOnlyList<TurmaResponse>>;

public sealed record CriarTurmaCommand(
    string Nome,
    TipoSessao TipoSessao,
    Guid? ProfissionalId,
    IReadOnlyList<HorarioRequest>? Horarios = null) : IRequest<Guid>;

public sealed record AdicionarHorarioCommand(
    Guid TurmaId,
    int DiaSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFim) : IRequest;

public sealed record RemoverHorarioCommand(Guid TurmaId, Guid HorarioId) : IRequest;

public sealed class CriarTurmaCommandValidator : AbstractValidator<CriarTurmaCommand>
{
    public CriarTurmaCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(120);
        RuleFor(r => r.TipoSessao).IsInEnum();
        RuleForEach(r => r.Horarios)
            .Must(h => h is { DiaSemana: >= 1 and <= 7 })
            .WithMessage("Dia da semana deve estar entre 1 (Segunda) e 7 (Domingo).")
            .Must(h => h.HoraInicio < h.HoraFim)
            .WithMessage("Hora de início deve ser anterior à hora de fim.");
    }
}

public sealed class AdicionarHorarioCommandValidator : AbstractValidator<AdicionarHorarioCommand>
{
    public AdicionarHorarioCommandValidator()
    {
        RuleFor(r => r.DiaSemana).InclusiveBetween(1, 7);
        RuleFor(r => r.HoraInicio).LessThan(r => r.HoraFim)
            .WithMessage("Hora de início deve ser anterior à hora de fim.");
    }
}

public sealed class ListarTurmasQueryHandler
    : IRequestHandler<ListarTurmasQuery, IReadOnlyList<TurmaResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarTurmasQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TurmaResponse>> Handle(ListarTurmasQuery request, CancellationToken ct)
    {
        var turmas = await _db.Turmas
            .Include(t => t.Profissional)
            .Include(t => t.Horarios)
            .AsNoTracking()
            .OrderBy(t => t.Nome)
            .ToListAsync(ct);

        return turmas.Select(t => new TurmaResponse(
            t.Id,
            t.Nome,
            t.TipoSessao,
            t.ProfissionalId,
            t.Profissional?.Nome,
            t.Ativo,
            t.Horarios
                .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
                .Select(h => new TurmaHorarioResponse(h.Id, h.DiaSemana, h.HoraInicio, h.HoraFim))
                .ToList())).ToList();
    }
}

public sealed class CriarTurmaCommandHandler : IRequestHandler<CriarTurmaCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CriarTurmaCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CriarTurmaCommand request, CancellationToken ct)
    {
        if (request.ProfissionalId is not null)
        {
            var existe = await _db.Profissionais
                .AnyAsync(p => p.Id == request.ProfissionalId, ct);
            if (!existe)
                throw new BusinessRuleException("Profissional informado não existe.");
        }

        var turma = new Domain.Entities.Turma
        {
            Nome = request.Nome.Trim(),
            TipoSessao = request.TipoSessao,
            ProfissionalId = request.ProfissionalId,
            Ativo = true,
        };

        foreach (var h in request.Horarios ?? [])
        {
            turma.Horarios.Add(new TurmaHorario
            {
                ClinicaId = Guid.Empty,
                TurmaId = turma.Id,
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio,
                HoraFim = h.HoraFim,
            });
        }

        await _db.Turmas.AddAsync(turma, ct);
        await _db.SaveChangesAsync(ct);

        return turma.Id;
    }
}

public sealed class AdicionarHorarioCommandHandler : IRequestHandler<AdicionarHorarioCommand>
{
    private readonly IApplicationDbContext _db;

    public AdicionarHorarioCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AdicionarHorarioCommand request, CancellationToken ct)
    {
        var turma = await _db.Turmas
            .FirstOrDefaultAsync(t => t.Id == request.TurmaId, ct)
            ?? throw new NotFoundException("Turma não encontrada.");

        var duplicado = await _db.TurmaHorarios.AnyAsync(
            h => h.TurmaId == request.TurmaId
                 && h.DiaSemana == request.DiaSemana
                 && h.HoraInicio == request.HoraInicio,
            ct);
        if (duplicado)
            throw new BusinessRuleException("A turma já possui este horário.");

        _db.TurmaHorarios.Add(new TurmaHorario
        {
            TurmaId = turma.Id,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim,
        });

        await _db.SaveChangesAsync(ct);
    }
}

public sealed class RemoverHorarioCommandHandler : IRequestHandler<RemoverHorarioCommand>
{
    private readonly IApplicationDbContext _db;

    public RemoverHorarioCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RemoverHorarioCommand request, CancellationToken ct)
    {
        var horario = await _db.TurmaHorarios
            .FirstOrDefaultAsync(h => h.Id == request.HorarioId && h.TurmaId == request.TurmaId, ct)
            ?? throw new NotFoundException("Horário não encontrado.");

        _db.TurmaHorarios.Remove(horario);
        await _db.SaveChangesAsync(ct);
    }
}