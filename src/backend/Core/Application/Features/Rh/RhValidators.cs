using FluentValidation;

namespace Clinica.Application.Features.Rh;

public sealed class RegistrarPontoCommandValidator : AbstractValidator<RegistrarPontoCommand>
{
    public RegistrarPontoCommandValidator()
    {
        RuleFor(r => r.ProfissionalId).NotEmpty();
        RuleFor(r => r.Data).NotEmpty();
        RuleFor(r => r.Entrada).NotNull().WithMessage("Entrada é obrigatória.");
        RuleFor(r => r.Saida).NotNull().WithMessage("Saída é obrigatória.");
    }
}

public sealed class CalcularFolhaCommandValidator : AbstractValidator<CalcularFolhaCommand>
{
    public CalcularFolhaCommandValidator()
    {
        RuleFor(r => r.ProfissionalId).NotEmpty();
        RuleFor(r => r.Competencia)
            .NotEmpty()
            .Matches("^\\d{4}-\\d{2}$")
            .WithMessage("Competência deve estar no formato yyyy-MM.");
        RuleFor(r => r.Descontos).GreaterThanOrEqualTo(0);
    }
}