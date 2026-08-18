using FluentValidation;

namespace Clinica.Application.Features.Clinicas;

public sealed class CriarClinicaCommandValidator : AbstractValidator<CriarClinicaCommand>
{
    public CriarClinicaCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Cnpj).NotEmpty().Length(14);
        RuleFor(r => r.Email).NotEmpty().EmailAddress();
        RuleFor(r => r.NomeAdministrador).NotEmpty().MaximumLength(150);
        RuleFor(r => r.EmailAdministrador).NotEmpty().EmailAddress();
        RuleFor(r => r.SenhaAdministrador)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("A senha do administrador deve ter ao menos 8 caracteres.");
    }
}