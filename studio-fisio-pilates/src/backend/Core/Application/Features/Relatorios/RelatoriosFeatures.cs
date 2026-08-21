using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Relatorios;

public sealed record ResumoDashboardResponse(
    int PacientesAtivos,
    int AgendamentosHoje,
    decimal ReceitaMes,
    decimal Inadimplencia);

public sealed record ResumoDashboardQuery : IRequest<ResumoDashboardResponse>;

public sealed class ResumoDashboardQueryHandler : IRequestHandler<ResumoDashboardQuery, ResumoDashboardResponse>
{
    private readonly IApplicationDbContext _db;

    public ResumoDashboardQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ResumoDashboardResponse> Handle(ResumoDashboardQuery request, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);

        var pacientesAtivos = await _db.Pacientes
            .CountAsync(p => p.Status == StatusPaciente.Ativo, ct);

        var agendamentosHoje = await _db.Agendamentos
            .CountAsync(a => a.DataHoraInicio >= hoje
                          && a.DataHoraInicio < hoje.AddDays(1)
                          && a.Status != StatusAgendamento.Cancelado, ct);

        // Agregações feitas em memória sobre colunas mínimas: mantém o mesmo
        // comportamento em Postgres (produção) e SQLite (testes), que não
        // traduz SUM(decimal) para SQL.
        var valoresPagosNoMes = await _db.Mensalidades
            .Where(m => m.Status == StatusMensalidade.Paga
                     && m.DataPagamento != null
                     && m.DataPagamento >= inicioMes
                     && m.DataPagamento < fimMes)
            .Select(m => m.Valor)
            .ToListAsync(ct);

        var valoresVencidos = await _db.Mensalidades
            .Where(m => (m.Status == StatusMensalidade.Pendente || m.Status == StatusMensalidade.Atrasada)
                     && m.DataVencimento < hoje)
            .Select(m => m.Valor)
            .ToListAsync(ct);

        return new ResumoDashboardResponse(
            pacientesAtivos,
            agendamentosHoje,
            valoresPagosNoMes.Sum(),
            valoresVencidos.Sum());
    }
}

public sealed record FaturamentoItem(string Competencia, decimal Receita, decimal Previsto);

public sealed record FaturamentoQuery(int Meses) : IRequest<IReadOnlyList<FaturamentoItem>>;

public sealed class FaturamentoQueryHandler : IRequestHandler<FaturamentoQuery, IReadOnlyList<FaturamentoItem>>
{
    private readonly IApplicationDbContext _db;

    public FaturamentoQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FaturamentoItem>> Handle(FaturamentoQuery request, CancellationToken ct)
    {
        var meses = Math.Clamp(request.Meses <= 0 ? 6 : request.Meses, 1, 24);
        var hoje = DateTime.UtcNow.Date;
        var inicio = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(meses - 1));
        var competenciasValidas = Enumerable.Range(0, meses)
            .Select(i => inicio.AddMonths(i).ToString("yyyy-MM"))
            .ToHashSet();

        var linhas = await _db.Mensalidades
            .Where(m => m.DataVencimento >= inicio && m.Status != StatusMensalidade.Cancelada)
            .Select(m => new { m.Competencia, m.Valor, m.Status })
            .ToListAsync(ct);

        var agrupado = linhas
            .GroupBy(l => l.Competencia)
            .Select(g => new
            {
                Competencia = g.Key,
                Receita = g.Sum(l => l.Status == StatusMensalidade.Paga ? l.Valor : 0m),
                Previsto = g.Sum(l => l.Valor),
            })
            .ToList();

        return competenciasValidas
            .OrderBy(c => c)
            .Select(c =>
            {
                var linha = agrupado.FirstOrDefault(l => l.Competencia == c);
                return new FaturamentoItem(c, linha?.Receita ?? 0m, linha?.Previsto ?? 0m);
            })
            .ToList();
    }
}

public sealed record OcupacaoDia(DateTime Data, int Total, int Realizados, int Faltas);

public sealed record OcupacaoQuery(int Dias) : IRequest<IReadOnlyList<OcupacaoDia>>;

public sealed class OcupacaoQueryHandler : IRequestHandler<OcupacaoQuery, IReadOnlyList<OcupacaoDia>>
{
    private readonly IApplicationDbContext _db;

    public OcupacaoQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OcupacaoDia>> Handle(OcupacaoQuery request, CancellationToken ct)
    {
        var dias = Math.Clamp(request.Dias <= 0 ? 30 : request.Dias, 1, 90);
        var hoje = DateTime.UtcNow.Date;
        var inicio = hoje.AddDays(-(dias - 1));

        var linhas = await _db.Agendamentos
            .Where(a => a.DataHoraInicio >= inicio && a.DataHoraInicio < hoje.AddDays(1))
            .Select(a => new { Data = a.DataHoraInicio.Date, a.Status })
            .ToListAsync(ct);

        var agrupado = linhas
            .GroupBy(l => l.Data)
            .Select(g => new
            {
                Data = g.Key,
                Total = g.Count(l => l.Status != StatusAgendamento.Cancelado),
                Realizados = g.Count(l => l.Status == StatusAgendamento.Realizado),
                Faltas = g.Count(l => l.Status == StatusAgendamento.Faltou),
            })
            .ToList();

        return Enumerable.Range(0, dias)
            .Select(i => inicio.AddDays(i))
            .Select(d => agrupado.FirstOrDefault(l => l.Data == d))
            .Select(l => new OcupacaoDia(l?.Data ?? default, l?.Total ?? 0, l?.Realizados ?? 0, l?.Faltas ?? 0))
            .ToList();
    }
}

public sealed record TopSessaoItem(string TipoSessao, int Quantidade, decimal Receita);

public sealed record TopSessoesQuery(int Top) : IRequest<IReadOnlyList<TopSessaoItem>>;

public sealed class TopSessoesQueryHandler : IRequestHandler<TopSessoesQuery, IReadOnlyList<TopSessaoItem>>
{
    private readonly IApplicationDbContext _db;

    public TopSessoesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TopSessaoItem>> Handle(TopSessoesQuery request, CancellationToken ct)
    {
        var top = Math.Clamp(request.Top <= 0 ? 5 : request.Top, 1, 10);

        var linhas = await _db.Agendamentos
            .Where(a => a.Status == StatusAgendamento.Realizado)
            .Select(a => new { a.TipoSessao, a.ValorSessao })
            .ToListAsync(ct);

        return linhas
            .GroupBy(l => l.TipoSessao)
            .Select(g => new TopSessaoItem(
                g.Key.ToString(),
                g.Count(),
                g.Sum(l => l.ValorSessao)))
            .OrderByDescending(l => l.Receita)
            .Take(top)
            .ToList();
    }
}
