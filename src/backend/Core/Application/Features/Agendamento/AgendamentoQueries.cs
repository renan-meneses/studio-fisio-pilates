using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Agendamento;

public sealed class ListarAgendamentosQueryHandler
    : IRequestHandler<ListarAgendamentosQuery, IReadOnlyList<AgendamentoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarAgendamentosQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AgendamentoResponse>> Handle(
        ListarAgendamentosQuery request,
        CancellationToken ct)
    {
        var query = _db.Agendamentos
            .Include(a => a.Paciente)
            .Include(a => a.Profissional)
            .AsNoTracking();

        if (request.De.HasValue)
            query = query.Where(a => a.DataHoraInicio >= request.De.Value);
        if (request.Ate.HasValue)
            query = query.Where(a => a.DataHoraFim <= request.Ate.Value);
        if (request.ProfissionalId.HasValue)
            query = query.Where(a => a.ProfissionalId == request.ProfissionalId.Value);
        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        var agendamentos = await query
            .OrderBy(a => a.DataHoraInicio)
            .ToListAsync(ct);

        return agendamentos.Select(a => a.ToResponse()).ToList();
    }
}

public sealed class ObterAgendamentoQueryHandler : IRequestHandler<ObterAgendamentoQuery, AgendamentoResponse>
{
    private readonly IApplicationDbContext _db;

    public ObterAgendamentoQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AgendamentoResponse> Handle(ObterAgendamentoQuery request, CancellationToken ct)
    {
        var agendamento = await _db.Agendamentos
            .Include(a => a.Paciente)
            .Include(a => a.Profissional)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        return agendamento.ToResponse();
    }
}