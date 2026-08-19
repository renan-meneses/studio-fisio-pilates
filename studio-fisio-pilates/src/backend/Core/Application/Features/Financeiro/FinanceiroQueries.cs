using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Financeiro;

public sealed record MensalidadeResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    string Competencia,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    StatusMensalidade Status);

public sealed record ContaPagarResponse(
    Guid Id,
    string Fornecedor,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    TipoCusto TipoCusto,
    StatusContaPagar Status);

public sealed record DashboardFinanceiroResponse(
    decimal ReceitaMensal,
    decimal ReceitaRecebida,
    decimal DespesaMensal,
    decimal Resultado,
    int MensalidadesAtrasadas,
    IReadOnlyList<MensalidadeResponse> UltimasMensalidades,
    IReadOnlyList<ContaPagarResponse> ContasAVencer);

public sealed record ListarMensalidadesQuery(string? Competencia, StatusMensalidade? Status)
    : IRequest<IReadOnlyList<MensalidadeResponse>>;

public sealed record ListarContasPagarQuery(StatusContaPagar? Status)
    : IRequest<IReadOnlyList<ContaPagarResponse>>;

public sealed record ObterDashboardQuery(string Competencia) : IRequest<DashboardFinanceiroResponse>;

public sealed class ListarMensalidadesQueryHandler
    : IRequestHandler<ListarMensalidadesQuery, IReadOnlyList<MensalidadeResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarMensalidadesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MensalidadeResponse>> Handle(
        ListarMensalidadesQuery request,
        CancellationToken ct)
    {
        var query = _db.Mensalidades.Include(m => m.Paciente).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Competencia))
            query = query.Where(m => m.Competencia == request.Competencia);
        if (request.Status.HasValue)
            query = query.Where(m => m.Status == request.Status.Value);

        return (await query.OrderByDescending(m => m.Competencia).ToListAsync(ct))
            .Select(m => new MensalidadeResponse(
                m.Id, m.PacienteId, m.Paciente?.Nome ?? string.Empty,
                m.Competencia, m.Valor, m.DataVencimento, m.DataPagamento, m.Status))
            .ToList();
    }
}

public sealed class ListarContasPagarQueryHandler
    : IRequestHandler<ListarContasPagarQuery, IReadOnlyList<ContaPagarResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarContasPagarQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ContaPagarResponse>> Handle(
        ListarContasPagarQuery request,
        CancellationToken ct)
    {
        var query = _db.ContasPagar.AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        return (await query
            .OrderBy(c => c.DataVencimento)
            .ToListAsync(ct))
            .Select(c => new ContaPagarResponse(
                c.Id, c.Fornecedor, c.Descricao, c.Valor,
                c.DataVencimento, c.DataPagamento, c.TipoCusto, c.Status))
            .ToList();
    }
}

public sealed class ObterDashboardQueryHandler : IRequestHandler<ObterDashboardQuery, DashboardFinanceiroResponse>
{
    private readonly IApplicationDbContext _db;

    public ObterDashboardQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardFinanceiroResponse> Handle(ObterDashboardQuery request, CancellationToken ct)
    {
        var mensalidades = await _db.Mensalidades
            .Include(m => m.Paciente)
            .AsNoTracking()
            .Where(m => m.Competencia == request.Competencia)
            .ToListAsync(ct);

        var contas = await _db.ContasPagar
            .AsNoTracking()
            .Where(c => c.DataVencimento.Month == ToDate(request.Competencia).Month
                        && c.DataVencimento.Year == ToDate(request.Competencia).Year)
            .ToListAsync(ct);

        var receitaMensal = mensalidades.Sum(m => m.Valor);
        var receitaRecebida = mensalidades.Where(m => m.Status == StatusMensalidade.Paga).Sum(m => m.Valor);
        var despesaMensal = contas.Sum(c => c.Valor);
        var resultado = receitaRecebida - despesaMensal;

        return new DashboardFinanceiroResponse(
            receitaMensal,
            receitaRecebida,
            despesaMensal,
            resultado,
            mensalidades.Count(m => m.Status == StatusMensalidade.Atrasada),
            mensalidades.Take(5).Select(m => new MensalidadeResponse(
                m.Id, m.PacienteId, m.Paciente?.Nome ?? string.Empty,
                m.Competencia, m.Valor, m.DataVencimento, m.DataPagamento, m.Status)).ToList(),
            contas.Where(c => c.Status != StatusContaPagar.Paga).Take(5).Select(c => new ContaPagarResponse(
                c.Id, c.Fornecedor, c.Descricao, c.Valor,
                c.DataVencimento, c.DataPagamento, c.TipoCusto, c.Status)).ToList());
    }

    private static DateTime ToDate(string competencia) =>
        new(int.Parse(competencia[..4]), int.Parse(competencia[5..7]), 1);
}