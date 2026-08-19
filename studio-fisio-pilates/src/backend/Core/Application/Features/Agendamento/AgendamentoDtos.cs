using Clinica.Domain.Enums;
using MediatR;

namespace Clinica.Application.Features.Agendamento;

public sealed record AgendamentoResponse(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    Guid ProfissionalId,
    string ProfissionalNome,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    TipoSessao TipoSessao,
    TipoAula TipoAula,
    Guid? TurmaId,
    string? TurmaNome,
    StatusAgendamento Status,
    decimal ValorSessao,
    string? Observacoes);

public sealed record ListarAgendamentosQuery(
    DateTime? De,
    DateTime? Ate,
    Guid? ProfissionalId,
    StatusAgendamento? Status) : IRequest<IReadOnlyList<AgendamentoResponse>>;

public sealed record ObterAgendamentoQuery(Guid Id) : IRequest<AgendamentoResponse>;