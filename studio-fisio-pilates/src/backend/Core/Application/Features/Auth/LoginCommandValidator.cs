using FluentValidation;

namespace Clinica.Application.Features.Auth;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("E-mail inválido.");

        RuleFor(r => r.Senha)
            .NotEmpty()
            .MinimumLength(6)
            .WithMessage("A senha deve ter ao menos 6 caracteres.");
    }
}