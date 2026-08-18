using Clinica.Domain.Entities;

namespace Clinica.Application.Features.Agendamento;

public static class AgendamentoMappings
{
    public static AgendamentoResponse ToResponse(this Domain.Entities.Agendamento a) => new(
        a.Id,
        a.PacienteId,
        a.Paciente?.Nome ?? string.Empty,
        a.ProfissionalId,
        a.Profissional?.Nome ?? string.Empty,
        a.DataHoraInicio,
        a.DataHoraFim,
        a.TipoSessao,
        a.Status,
        a.ValorSessao,
        a.Observacoes);
}