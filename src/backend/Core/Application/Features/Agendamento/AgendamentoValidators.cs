using Clinica.Domain.Enums;
using FluentValidation;

namespace Clinica.Application.Features.Agendamento;

public sealed class CriarAgendamentoCommandValidator : AbstractValidator<CriarAgendamentoCommand>
{
    public CriarAgendamentoCommandValidator()
    {
        RuleFor(r => r.PacienteId).NotEmpty();
        RuleFor(r => r.ProfissionalId).NotEmpty();
        RuleFor(r => r.DataHoraInicio).NotEmpty();
        RuleFor(r => r.DataHoraFim).NotEmpty();
        RuleFor(r => r.DataHoraFim)
            .GreaterThan(r => r.DataHoraInicio)
            .WithMessage("A sessão deve terminar após o início.");
        RuleFor(r => r.ValorSessao).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Observacoes).MaximumLength(500);
    }
}

public sealed class AtualizarAgendamentoCommandValidator : AbstractValidator<AtualizarAgendamentoCommand>
{
    public AtualizarAgendamentoCommandValidator()
    {
        RuleFor(r => r.Id).NotEmpty();
        RuleFor(r => r.PacienteId).NotEmpty();
        RuleFor(r => r.ProfissionalId).NotEmpty();
        RuleFor(r => r.DataHoraFim)
            .GreaterThan(r => r.DataHoraInicio)
            .WithMessage("A sessão deve terminar após o início.");
    }
}

public sealed class RegistrarPresencaCommandValidator : AbstractValidator<RegistrarPresencaCommand>
{
    public RegistrarPresencaCommandValidator()
    {
        RuleFor(r => r.AgendamentoId).NotEmpty();
        RuleFor(r => r.Resultado)
            .Must(s => s is StatusAgendamento.Realizado or StatusAgendamento.Faltou)
            .WithMessage("Resultado deve ser Realizado ou Faltou.");
    }
}