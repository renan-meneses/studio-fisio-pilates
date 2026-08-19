using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AgendamentoEntity = Clinica.Domain.Entities.Agendamento;
using PresencaEntity = Clinica.Domain.Entities.Presenca;

namespace Clinica.Application.Features.Agendamento;

public sealed record CriarAgendamentoCommand(
    Guid PacienteId,
    Guid ProfissionalId,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    TipoSessao TipoSessao,
    TipoAula TipoAula = TipoAula.Individual,
    Guid? TurmaId = null,
    decimal ValorSessao = 0,
    string? Observacoes = null) : IAgendamentoRequest;

public sealed record AtualizarAgendamentoCommand(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    TipoSessao TipoSessao,
    TipoAula TipoAula = TipoAula.Individual,
    Guid? TurmaId = null,
    string? Observacoes = null) : IAgendamentoRequest;

public sealed record CancelarAgendamentoCommand(Guid Id, string Motivo) : IRequest;

public sealed record ConfirmarAgendamentoCommand(Guid Id) : IRequest;

public sealed record RegistrarPresencaCommand(Guid AgendamentoId, StatusAgendamento Resultado) : IRequest<Guid>;

/// <summary>Marca comum: comandos que alteram a janela de horário.</summary>
public interface IAgendamentoRequest : IRequest<Guid>
{
    Guid PacienteId { get; }

    Guid ProfissionalId { get; }

    DateTime DataHoraInicio { get; }

    DateTime DataHoraFim { get; }

    TipoSessao TipoSessao { get; }

    Guid? TurmaId { get; }
}

/// <summary>
/// Regras de negócio da agregação Agendamento:
///  - profissional não pode ter sessões sobrepostas;
///  - janela de horário deve ser válida (início < fim);
///  - paciente e profissional devem existir no tenant.
/// </summary>
public sealed class CriarAgendamentoCommandHandler : IRequestHandler<CriarAgendamentoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CriarAgendamentoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CriarAgendamentoCommand request, CancellationToken ct)
    {
        await ValidarDependencias(_db, request, null, ct);

        var agendamento = new AgendamentoEntity
        {
            PacienteId = request.PacienteId,
            ProfissionalId = request.ProfissionalId,
            DataHoraInicio = request.DataHoraInicio,
            DataHoraFim = request.DataHoraFim,
            TipoSessao = request.TipoSessao,
            TipoAula = request.TipoAula,
            TurmaId = request.TurmaId,
            ValorSessao = request.ValorSessao,
            Observacoes = request.Observacoes,
        };

        await _db.Agendamentos.AddAsync(agendamento, ct);
        await _db.SaveChangesAsync(ct);

        return agendamento.Id;
    }

    internal static async Task ValidarDependencias(
        IApplicationDbContext db,
        IAgendamentoRequest request,
        Guid? atualId,
        CancellationToken ct)
    {
        if (request.DataHoraInicio >= request.DataHoraFim)
            throw new BusinessRuleException("A janela de horário é inválida (início deve ser anterior ao fim).");

        if (request.TurmaId is not null)
        {
            var turma = await db.Turmas
                .FirstOrDefaultAsync(t => t.Id == request.TurmaId, ct)
                ?? throw new BusinessRuleException("Turma informada não existe.");

            if (turma.TipoSessao != request.TipoSessao)
                throw new BusinessRuleException(
                    $"A turma '{turma.Nome}' não é do tipo de sessão selecionado.");
        }

        var sobreposto = await db.Agendamentos.AnyAsync(
            a => a.Id != atualId
                 && a.ProfissionalId == request.ProfissionalId
                 && (a.Status == StatusAgendamento.Agendado
                     || a.Status == StatusAgendamento.Confirmado)
                 && a.DataHoraInicio < request.DataHoraFim
                 && a.DataHoraFim > request.DataHoraInicio,
            ct);

        if (sobreposto)
            throw new BusinessRuleException("Profissional já possui sessão no período informado.");
    }
}

public sealed class AtualizarAgendamentoCommandHandler
    : IRequestHandler<AtualizarAgendamentoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AtualizarAgendamentoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(AtualizarAgendamentoCommand request, CancellationToken ct)
    {
        var agendamento = await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        if (agendamento.Status is StatusAgendamento.Realizado or StatusAgendamento.Cancelado)
            throw new BusinessRuleException("Agendamento realizado/cancelado não pode ser alterado.");

        await CriarAgendamentoCommandHandler.ValidarDependencias(
            _db, request, agendamento.Id, ct);

        agendamento.PacienteId = request.PacienteId;
        agendamento.ProfissionalId = request.ProfissionalId;
        agendamento.DataHoraInicio = request.DataHoraInicio;
        agendamento.DataHoraFim = request.DataHoraFim;
        agendamento.TipoSessao = request.TipoSessao;
        agendamento.TipoAula = request.TipoAula;
        agendamento.TurmaId = request.TurmaId;
        agendamento.Observacoes = request.Observacoes;

        await _db.SaveChangesAsync(ct);
        return agendamento.Id;
    }
}

public sealed class CancelarAgendamentoCommandHandler : IRequestHandler<CancelarAgendamentoCommand>
{
    private readonly IApplicationDbContext _db;

    public CancelarAgendamentoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(CancelarAgendamentoCommand request, CancellationToken ct)
    {
        var agendamento = await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        if (agendamento.Status is StatusAgendamento.Realizado)
            throw new BusinessRuleException("Agendamento realizado não pode ser cancelado.");

        agendamento.Status = StatusAgendamento.Cancelado;
        agendamento.Observacoes = string.IsNullOrWhiteSpace(agendamento.Observacoes)
            ? $"Cancelado: {request.Motivo}"
            : $"{agendamento.Observacoes} — Cancelado: {request.Motivo}";

        await _db.SaveChangesAsync(ct);
    }
}

public sealed class ConfirmarAgendamentoCommandHandler : IRequestHandler<ConfirmarAgendamentoCommand>
{
    private readonly IApplicationDbContext _db;

    public ConfirmarAgendamentoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(ConfirmarAgendamentoCommand request, CancellationToken ct)
    {
        var agendamento = await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        if (agendamento.Status is not (StatusAgendamento.Agendado or StatusAgendamento.Confirmado))
            throw new BusinessRuleException("Só é possível confirmar agendamentos ativos.");

        agendamento.Status = StatusAgendamento.Confirmado;
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class RegistrarPresencaCommandHandler : IRequestHandler<RegistrarPresencaCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public RegistrarPresencaCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(RegistrarPresencaCommand request, CancellationToken ct)
    {
        var agendamento = await _db.Agendamentos
            .Include(a => a.Presenca)
            .FirstOrDefaultAsync(a => a.Id == request.AgendamentoId, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        if (agendamento.Status.IsAtivo() is false)
            throw new BusinessRuleException("Só é possível registrar presença de agendamentos ativos.");

        // Navigation fix-up não rastreia filhos com ClinicaId=Empty (não passam
        // no Global Query Filter) — adicionar explicitamente ao DbSet garante o
        // estado Added e o interceptor grava o tenant correto.
        var presenca = agendamento.Presenca;
        if (presenca is null)
        {
            presenca = new PresencaEntity
            {
                AgendamentoId = agendamento.Id,
                Entrada = DateTime.UtcNow,
            };
            agendamento.Presenca = presenca;
            await _db.Presencas.AddAsync(presenca, ct);
        }

        presenca.Status = request.Resultado == StatusAgendamento.Realizado
            ? StatusPresenca.Presente
            : StatusPresenca.Ausente;
        presenca.Saida = request.Resultado == StatusAgendamento.Realizado ? DateTime.UtcNow : null;

        agendamento.Status = request.Resultado;

        await _db.SaveChangesAsync(ct);
        return presenca.Id;
    }
}

public static class StatusAgendamentoExtensions
{
    public static bool IsAtivo(this StatusAgendamento status) =>
        status is StatusAgendamento.Agendado or StatusAgendamento.Confirmado;
}