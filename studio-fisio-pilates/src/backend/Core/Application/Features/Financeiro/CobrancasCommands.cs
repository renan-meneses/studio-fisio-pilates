using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Financeiro;

// ---------------------------------------------------------------------------
// Faturamento recorrente
// ---------------------------------------------------------------------------

public sealed record GerarFaturamentoRecorrenteCommand(string Competencia)
    : IRequest<FaturamentoRecorrenteResponse>;

public sealed record FaturamentoRecorrenteResponse(int Geradas, int JaExistentes);

/// <summary>
/// Gera mensalidades do ciclo para todos os pacientes ativos com plano.
/// Idempotente por competência: pacientes já faturados são ignorados,
/// permitindo reexecução segura (retry/agendamento).
/// </summary>
public sealed class GerarFaturamentoRecorrenteCommandHandler
    : IRequestHandler<GerarFaturamentoRecorrenteCommand, FaturamentoRecorrenteResponse>
{
    private readonly IApplicationDbContext _db;

    public GerarFaturamentoRecorrenteCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FaturamentoRecorrenteResponse> Handle(
        GerarFaturamentoRecorrenteCommand request,
        CancellationToken ct)
    {
        var pacientes = await _db.Pacientes
            .AsNoTracking()
            .Where(p => p.Status == StatusPaciente.Ativo && p.PlanoId != null)
            .Select(p => new { p.Id, ValorPlano = p.Plano!.Valor })
            .ToListAsync(ct);

        var jaFaturados = await _db.Mensalidades
            .Where(m => m.Competencia == request.Competencia)
            .Select(m => m.PacienteId)
            .ToListAsync(ct);
        var faturar = pacientes
            .Where(p => !jaFaturados.Contains(p.Id))
            .ToList();

        var vencimento = new DateTime(
            int.Parse(request.Competencia[..4]),
            int.Parse(request.Competencia[5..7]),
            10);

        foreach (var paciente in faturar)
        {
            await _db.Mensalidades.AddAsync(new Mensalidade
            {
                PacienteId = paciente.Id,
                Competencia = request.Competencia,
                Valor = paciente.ValorPlano,
                DataVencimento = vencimento,
                Status = StatusMensalidade.Pendente,
            }, ct);
        }

        await _db.SaveChangesAsync(ct);

        return new FaturamentoRecorrenteResponse(faturar.Count, jaFaturados.Count);
    }
}

// ---------------------------------------------------------------------------
// Emissão de cobrança (Pix/Boleto)
// ---------------------------------------------------------------------------

public sealed record EmitirCobrancaCommand(Guid MensalidadeId, TipoCobranca Tipo) : IRequest<CobrancaResponse>;

public sealed record CobrancaResponse(
    Guid Id,
    Guid MensalidadeId,
    TipoCobranca Tipo,
    string Provedor,
    string ProvedorCobrancaId,
    decimal Valor,
    StatusCobranca Status,
    string? PixCopiaECola,
    string? BoletoLinhaDigitavel,
    DateTime ExpiraEmUtc,
    DateTime? PagaEmUtc);

public sealed class EmitirCobrancaCommandHandler : IRequestHandler<EmitirCobrancaCommand, CobrancaResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;

    public EmitirCobrancaCommandHandler(IApplicationDbContext db, IPaymentGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public async Task<CobrancaResponse> Handle(EmitirCobrancaCommand request, CancellationToken ct)
    {
        var mensalidade = await _db.Mensalidades
            .FirstOrDefaultAsync(m => m.Id == request.MensalidadeId, ct)
            ?? throw new NotFoundException("Mensalidade não encontrada.");

        if (mensalidade.Status is StatusMensalidade.Paga or StatusMensalidade.Cancelada)
            throw new BusinessRuleException("Mensalidade não está em aberto para cobrança.");

        // Retry seguro: cobrança pendente do mesmo tipo é reaproveitada.
        var existente = await _db.Cobrancas
            .FirstOrDefaultAsync(c => c.MensalidadeId == request.MensalidadeId
                                      && c.Tipo == request.Tipo
                                      && c.Status == StatusCobranca.Pendente, ct);

        if (existente is not null)
            return ParaResponse(existente);

        var emitida = await _gateway.CriarCobrancaAsync(
            mensalidade.Id, mensalidade.Valor, request.Tipo, ct);

        var cobranca = new Cobranca
        {
            MensalidadeId = mensalidade.Id,
            Tipo = request.Tipo,
            Provedor = _gateway.Nome,
            ProvedorCobrancaId = emitida.ProvedorCobrancaId,
            Valor = mensalidade.Valor,
            Status = StatusCobranca.Pendente,
            PixCopiaECola = emitida.PixCopiaECola,
            BoletoLinhaDigitavel = emitida.BoletoLinhaDigitavel,
            ExpiraEmUtc = emitida.ExpiraEmUtc,
        };

        await _db.Cobrancas.AddAsync(cobranca, ct);
        await _db.SaveChangesAsync(ct);

        return ParaResponse(cobranca);
    }

    private static CobrancaResponse ParaResponse(Cobranca c) => new(
        c.Id, c.MensalidadeId, c.Tipo, c.Provedor, c.ProvedorCobrancaId,
        c.Valor, c.Status, c.PixCopiaECola, c.BoletoLinhaDigitavel,
        c.ExpiraEmUtc, c.PagaEmUtc);
}

// ---------------------------------------------------------------------------
// Webhook de pagamento
// ---------------------------------------------------------------------------

public sealed record ProcessarWebhookPagamentoCommand(
    string Provedor,
    string EventoId,
    string TipoEvento,
    Guid CobrancaId,
    DateTime? PagoEmUtc,
    string PayloadJson) : IRequest<WebhookProcessadoResponse>;

public sealed record WebhookProcessadoResponse(bool Duplicado, bool Processado);

/// <summary>
/// Deduplica o evento por (tenant, provedor, eventoId) e liquida a cobrança +
/// a mensalidade em uma única transação (um SaveChanges). O tenant é
/// resolvido pela própria cobrança — webhooks chegam sem X-Tenant-Id.
/// Evento de cobrança desconhecida é armazenado como não processado para
/// reconciliação, mas ACKed ao provedor (evita retry storm).
/// </summary>
public sealed class ProcessarWebhookPagamentoCommandHandler
    : IRequestHandler<ProcessarWebhookPagamentoCommand, WebhookProcessadoResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public ProcessarWebhookPagamentoCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantAccessor tenantAccessor)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<WebhookProcessadoResponse> Handle(
        ProcessarWebhookPagamentoCommand request,
        CancellationToken ct)
    {
        var cobranca = await _db.Cobrancas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.CobrancaId, ct);

        if (cobranca is null)
            throw new NotFoundException("Cobrança não encontrada para o evento informado.");

        // A partir daqui o escopo é o tenant da cobrança: queries normais
        // (com filtro global) passam a enxergar apenas dados dela.
        _tenantAccessor.Set(cobranca.ClinicaId, cobranca.ClinicaId.ToString());

        var duplicado = await _db.EventosPagamentoWebhook.AnyAsync(
            e => e.Provedor == request.Provedor && e.EventoId == request.EventoId, ct);

        if (duplicado)
            return new WebhookProcessadoResponse(Duplicado: true, Processado: false);

        var evento = new EventoPagamentoWebhook
        {
            ClinicaId = cobranca.ClinicaId,
            Provedor = request.Provedor,
            EventoId = request.EventoId,
            TipoEvento = request.TipoEvento,
            Payload = request.PayloadJson,
        };

        if (cobranca.Status == StatusCobranca.Pendente)
        {
            var pagoEm = request.PagoEmUtc ?? DateTime.UtcNow;
            cobranca.MarcarPaga(pagoEm);

            var mensalidade = await _db.Mensalidades
                .FirstOrDefaultAsync(m => m.Id == cobranca.MensalidadeId, ct);

            if (mensalidade is not null && mensalidade.Status != StatusMensalidade.Paga)
                mensalidade.RegistrarPagamento(pagoEm);

            evento.MarcarProcessado();
        }
        else
        {
            evento.MarcarFalha($"Cobrança em status {cobranca.Status} — pagamento não aplicado.");
        }

        await _db.EventosPagamentoWebhook.AddAsync(evento, ct);
        await _db.SaveChangesAsync(ct);

        return new WebhookProcessadoResponse(Duplicado: false, Processado: evento.Processado);
    }
}