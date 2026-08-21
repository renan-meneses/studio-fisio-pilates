using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Prontuario;

public sealed record EvolucaoResponse(
    Guid Id,
    DateTime Data,
    TipoEvolucao Tipo,
    string ProfissionalNome,
    string? QueixaPrincipal,
    string? Avaliacao,
    string? Conduta,
    string? Observacoes);

public sealed record PacienteResumoResponse(
    Guid Id,
    string Nome,
    string? Telefone,
    bool Ativo);

public sealed record ListarPacientesQuery(
    string? Termo,
    int Limite = 200) : IRequest<IReadOnlyList<PacienteResumoResponse>>;

public sealed class ListarPacientesQueryHandler
    : IRequestHandler<ListarPacientesQuery, IReadOnlyList<PacienteResumoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarPacientesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PacienteResumoResponse>> Handle(
        ListarPacientesQuery request,
        CancellationToken ct)
    {
        var query = _db.Pacientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Termo))
            query = query.Where(p => p.Nome.ToLower().Contains(request.Termo.ToLower()));

        return (await query.OrderBy(p => p.Nome).Take(request.Limite).ToListAsync(ct))
            .Select(p => new PacienteResumoResponse(
                p.Id,
                p.Nome,
                p.Telefone,
                p.Status == StatusPaciente.Ativo))
            .ToList();
    }
}

public sealed record ProntuarioResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    DateTime DataAbertura,
    int TotalEvolucoes,
    IReadOnlyList<EvolucaoResponse> Evolucoes);

public sealed record ObterProntuarioPorPacienteQuery(Guid PacienteId) : IRequest<ProntuarioResponse>;

public sealed record ListarEvolucoesQuery(Guid ProntuarioId) : IRequest<IReadOnlyList<EvolucaoResponse>>;

public sealed class ListarEvolucoesQueryHandler
    : IRequestHandler<ListarEvolucoesQuery, IReadOnlyList<EvolucaoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarEvolucoesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EvolucaoResponse>> Handle(ListarEvolucoesQuery request, CancellationToken ct)
    {
        var prontuario = await _db.Prontuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProntuarioId && p.Ativo, ct)
            ?? throw new NotFoundException("Prontuário não encontrado.");

        var evolucoes = await _db.Evolucoes
            .Include(e => e.Profissional)
            .AsNoTracking()
            .Where(e => e.ProntuarioId == prontuario.Id)
            .OrderByDescending(e => e.Data)
            .ToListAsync(ct);

        return evolucoes
            .Select(e => new EvolucaoResponse(
                e.Id,
                e.Data,
                e.Tipo,
                e.Profissional?.Nome ?? string.Empty,
                e.QueixaPrincipal,
                e.Avaliacao,
                e.Conduta,
                e.Observacoes))
            .ToList();
    }
}
    public sealed class ObterProntuarioPorPacienteQueryHandler
    : IRequestHandler<ObterProntuarioPorPacienteQuery, ProntuarioResponse>
{
    private readonly IApplicationDbContext _db;

    public ObterProntuarioPorPacienteQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProntuarioResponse> Handle(ObterProntuarioPorPacienteQuery request, CancellationToken ct)
    {
        var prontuario = await _db.Prontuarios
            .Include(p => p.Paciente)
            .Include(p => p.Evolucoes).ThenInclude(e => e.Profissional)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PacienteId == request.PacienteId && p.Ativo, ct)
            ?? throw new NotFoundException("Prontuário não encontrado para o paciente.");

        return new ProntuarioResponse(
            prontuario.Id,
            prontuario.PacienteId,
            prontuario.Paciente?.Nome ?? string.Empty,
            prontuario.DataAbertura,
            prontuario.Evolucoes.Count,
            prontuario.Evolucoes
                .OrderByDescending(e => e.Data)
                .Select(e => new EvolucaoResponse(
                    e.Id,
                    e.Data,
                    e.Tipo,
                    e.Profissional?.Nome ?? string.Empty,
                    e.QueixaPrincipal,
                    e.Avaliacao,
                    e.Conduta,
                    e.Observacoes))
                .ToList());
    }
}