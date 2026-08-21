using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Financeiro;

public sealed record ObterCobrancasQuery(Guid MensalidadeId)
    : IRequest<IReadOnlyList<CobrancaResponse>>;

public sealed class ObterCobrancasQueryHandler
    : IRequestHandler<ObterCobrancasQuery, IReadOnlyList<CobrancaResponse>>
{
    private readonly IApplicationDbContext _db;

    public ObterCobrancasQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CobrancaResponse>> Handle(
        ObterCobrancasQuery request,
        CancellationToken ct)
    {
        return await _db.Cobrancas
            .AsNoTracking()
            .Where(c => c.MensalidadeId == request.MensalidadeId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CobrancaResponse(
                c.Id, c.MensalidadeId, c.Tipo, c.Provedor, c.ProvedorCobrancaId,
                c.Valor, c.Status, c.PixCopiaECola, c.BoletoLinhaDigitavel,
                c.ExpiraEmUtc, c.PagaEmUtc))
            .ToListAsync(ct);
    }
}

// ---------------------------------------------------------------------------
// Inadimplência (aging de contas a receber)
// ---------------------------------------------------------------------------

public sealed record ItemInadimplencia(
    Guid MensalidadeId,
    Guid PacienteId,
    string PacienteNome,
    string Competencia,
    decimal Valor,
    DateTime DataVencimento,
    int DiasAtraso,
    string FaixaAtraso);

public sealed record InadimplenciaResponse(
    decimal TotalVencido,
    int TotalPacientes,
    IReadOnlyDictionary<string, decimal> PorFaixa,
    IReadOnlyList<ItemInadimplencia> Itens);

public sealed record ObterInadimplenciaQuery : IRequest<InadimplenciaResponse>;

/// <summary>
/// Aging de mensalidades vencidas em aberto: faixas 1-30, 31-60, 61-90 e
/// 90+ dias. O atraso é calculado sobre DataVencimento (status Pendente).
/// </summary>
public sealed class ObterInadimplenciaQueryHandler
    : IRequestHandler<ObterInadimplenciaQuery, InadimplenciaResponse>
{
    private readonly IApplicationDbContext _db;

    public ObterInadimplenciaQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<InadimplenciaResponse> Handle(
        ObterInadimplenciaQuery request,
        CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;

        var vencidas = await _db.Mensalidades
            .AsNoTracking()
            .Include(m => m.Paciente)
            .Where(m => m.Status == StatusMensalidade.Pendente && m.DataVencimento < hoje)
            .OrderBy(m => m.DataVencimento)
            .ToListAsync(ct);

        var itens = vencidas.Select(m =>
        {
            var dias = (hoje - m.DataVencimento.Date).Days;
            return new ItemInadimplencia(
                m.Id, m.PacienteId, m.Paciente?.Nome ?? string.Empty,
                m.Competencia, m.Valor, m.DataVencimento, dias, Faixa(dias));
        }).ToList();

        var porFaixa = itens
            .GroupBy(i => i.FaixaAtraso)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Valor));

        return new InadimplenciaResponse(
            itens.Sum(i => i.Valor),
            itens.Select(i => i.PacienteId).Distinct().Count(),
            porFaixa,
            itens);
    }

    private static string Faixa(int dias) => dias switch
    {
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+",
    };
}