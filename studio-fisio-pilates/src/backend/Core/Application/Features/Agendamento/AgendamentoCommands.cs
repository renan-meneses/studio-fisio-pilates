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
        // Serializa agendamentos do mesmo profissional: a checagem de
        // sobreposição vira TOCTOU sem um lock entre leitura e escrita.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await AgendamentoLock.AdquirirAsync(_db, request.ProfissionalId, ct);

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
        await tx.CommitAsync(ct);

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

            var ocupantes = await db.Agendamentos
                .Where(a => a.Id != atualId
                            && a.TurmaId == request.TurmaId
                            && (a.Status == StatusAgendamento.Agendado
                                || a.Status == StatusAgendamento.Confirmado)
                            && a.DataHoraInicio < request.DataHoraFim
                            && a.DataHoraFim > request.DataHoraInicio)
                .Select(a => a.PacienteId)
                .Distinct()
                .ToListAsync(ct);

            if (!ocupantes.Contains(request.PacienteId) && ocupantes.Count >= turma.Capacidade)
                throw new BusinessRuleException(
                    $"A turma '{turma.Nome}' está cheia neste horário " +
                    $"(capacidade {turma.Capacidade}). Entre na lista de espera.");
        }

        var sobreposto = await db.Agendamentos.AnyAsync(
            a => a.Id != atualId
                 && a.ProfissionalId == request.ProfissionalId
                 && (a.Status == StatusAgendamento.Agendado
                     || a.Status == StatusAgendamento.Confirmado)
                 && a.DataHoraInicio < request.DataHoraFim
                 && a.DataHoraFim > request.DataHoraInicio
                 // Alunos da MESMA turma compartilham o horário do
                 // profissional: lá vale a regra de capacidade.
                 && !(request.TurmaId != null && a.TurmaId == request.TurmaId),
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

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await AgendamentoLock.AdquirirAsync(_db, request.ProfissionalId, ct);

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
        await tx.CommitAsync(ct);
        return agendamento.Id;
    }
}

public sealed class CancelarAgendamentoCommandHandler : IRequestHandler<CancelarAgendamentoCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly INotificacaoService _notificacoes;

    public CancelarAgendamentoCommandHandler(IApplicationDbContext db, INotificacaoService notificacoes)
    {
        _db = db;
        _notificacoes = notificacoes;
    }

    public async Task Handle(CancelarAgendamentoCommand request, CancellationToken ct)
    {
        var agendamento = await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException("Agendamento não encontrado.");

        if (agendamento.Status is StatusAgendamento.Realizado)
            throw new BusinessRuleException("Agendamento realizado não pode ser cancelado.");

        // Mesmo padrão de Criar/Atualizar: lock advisory serializa a janela do
        // profissional para que a checagem de capacidade na promoção não sofra
        // TOCTOU com agendamentos concorrentes.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await AgendamentoLock.AdquirirAsync(_db, agendamento.ProfissionalId, ct);

        agendamento.Status = StatusAgendamento.Cancelado;
        agendamento.Observacoes = string.IsNullOrWhiteSpace(agendamento.Observacoes)
            ? $"Cancelado: {request.Motivo}"
            : $"{agendamento.Observacoes} — Cancelado: {request.Motivo}";

        await _db.SaveChangesAsync(ct);

        await PromoverPrimeiroDaFilaAsync(_db, agendamento, _notificacoes, ct);

        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Liberação de vaga em turma: promove automaticamente o primeiro da fila
    /// de espera para a janela cancelada. A promoção revalida todas as regras
    /// (capacidade, tipo de sessão, conflitos do profissional); se alguma
    /// falhar, a fila permanece intacta para a próxima vaga.
    /// </summary>
    internal static async Task<Guid?> PromoverPrimeiroDaFilaAsync(
        IApplicationDbContext db,
        AgendamentoEntity cancelado,
        INotificacaoService notificacoes,
        CancellationToken ct)
    {
        if (cancelado.TurmaId is null)
            return null;

        var entrada = await db.WaitlistEntries
            .Include(w => w.Paciente)
            .Where(w => w.TurmaId == cancelado.TurmaId && w.Ativo)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (entrada is null)
            return null;

        var requisicao = new CriarAgendamentoCommand(
            entrada.PacienteId,
            cancelado.ProfissionalId,
            cancelado.DataHoraInicio,
            cancelado.DataHoraFim,
            cancelado.TipoSessao,
            cancelado.TipoAula,
            cancelado.TurmaId,
            cancelado.ValorSessao,
            "Promovido da lista de espera.");

        try
        {
            await CriarAgendamentoCommandHandler.ValidarDependencias(db, requisicao, null, ct);
        }
        catch (BusinessRuleException)
        {
            return null;
        }

        var promovido = new AgendamentoEntity
        {
            PacienteId = entrada.PacienteId,
            ProfissionalId = cancelado.ProfissionalId,
            DataHoraInicio = cancelado.DataHoraInicio,
            DataHoraFim = cancelado.DataHoraFim,
            TipoSessao = cancelado.TipoSessao,
            TipoAula = cancelado.TipoAula,
            TurmaId = cancelado.TurmaId,
            ValorSessao = cancelado.ValorSessao,
            Observacoes = "Promovido da lista de espera.",
        };

        await db.Agendamentos.AddAsync(promovido, ct);
        entrada.Ativo = false;
        await db.SaveChangesAsync(ct);

        await notificacoes.EnviarAsync(new NotificacaoMensagem(
            cancelado.ClinicaId,
            entrada.PacienteId,
            entrada.Paciente?.Email,
            "Vaga abriu na sua turma!",
            $"Uma vaga foi liberada e você foi promovido da lista de espera " +
            $"para {cancelado.DataHoraInicio:dd/MM/yyyy HH:mm}. Bom treino!"),
            ct);

        return promovido.Id;
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