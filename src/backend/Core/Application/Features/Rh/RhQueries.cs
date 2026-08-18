using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Rh;

public sealed record PontoResponse(
    Guid Id,
    Guid ProfissionalId,
    string ProfissionalNome,
    DateTime Data,
    TimeSpan? Entrada,
    TimeSpan? Saida,
    TimeSpan HorasTrabalhadas,
    TimeSpan? HorasExtras);

public sealed record FolhaResponse(
    Guid Id,
    Guid ProfissionalId,
    string ProfissionalNome,
    string Competencia,
    decimal ValorBruto,
    decimal Descontos,
    decimal ValorLiquido,
    int DiasTrabalhados,
    int Faltas,
    StatusFolha Status);

public sealed record ListarPontosQuery(Guid? ProfissionalId, DateTime? De, DateTime? Ate)
    : IRequest<IReadOnlyList<PontoResponse>>;

public sealed record ObterFolhaQuery(Guid ProfissionalId, string Competencia) : IRequest<FolhaResponse>;

public sealed class ListarPontosQueryHandler : IRequestHandler<ListarPontosQuery, IReadOnlyList<PontoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarPontosQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PontoResponse>> Handle(ListarPontosQuery request, CancellationToken ct)
    {
        var query = _db.Pontos.Include(p => p.Profissional).AsNoTracking();

        if (request.ProfissionalId.HasValue)
            query = query.Where(p => p.ProfissionalId == request.ProfissionalId.Value);
        if (request.De.HasValue)
            query = query.Where(p => p.Data >= request.De.Value.Date);
        if (request.Ate.HasValue)
            query = query.Where(p => p.Data <= request.Ate.Value.Date);

        return (await query.OrderByDescending(p => p.Data).ToListAsync(ct))
            .Select(p => new PontoResponse(
                p.Id,
                p.ProfissionalId,
                p.Profissional?.Nome ?? string.Empty,
                p.Data,
                p.Entrada,
                p.Saida,
                p.HorasTrabalhadas(),
                p.HorasExtras))
            .ToList();
    }
}

public sealed class ObterFolhaQueryHandler : IRequestHandler<ObterFolhaQuery, FolhaResponse>
{
    private readonly IApplicationDbContext _db;

    public ObterFolhaQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FolhaResponse> Handle(ObterFolhaQuery request, CancellationToken ct)
    {
        var folha = await _db.FolhasSalariais
            .Include(f => f.Profissional)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.ProfissionalId == request.ProfissionalId && f.Competencia == request.Competencia,
                ct);

        if (folha is null)
            throw new NotFoundException("Folha não encontrada para o profissional/competência.");

        return new FolhaResponse(
            folha.Id,
            folha.ProfissionalId,
            folha.Profissional?.Nome ?? string.Empty,
            folha.Competencia,
            folha.ValorBruto,
            folha.Descontos,
            folha.ValorLiquido,
            folha.DiasTrabalhados,
            folha.Faltas,
            folha.Status);
    }
}