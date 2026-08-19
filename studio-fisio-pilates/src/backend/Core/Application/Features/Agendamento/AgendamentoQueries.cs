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
            .Include(a => a.Turma)
            .AsNoTracking();

        if (request.De.HasValue)
            query = query.Where(a => a.DataHoraInicio >= request.De.Value);
        if (request.Ate.HasValue)
        {
            // "ate" com hora zerada (ex.: ?ate=2026-08-19) significa fim do dia.
            var ate = request.Ate.Value.TimeOfDay == TimeSpan.Zero
                ? request.Ate.Value.Date.AddDays(1).AddTicks(-1)
                : request.Ate.Value;
            query = query.Where(a => a.DataHoraFim <= ate);
        }
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
            .Include(a => a.Turma)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        return agendamento.ToResponse();
    }
}