using FluentValidation;

namespace Clinica.Application.Features.Financeiro;

public sealed class GerarMensalidadeCommandValidator : AbstractValidator<GerarMensalidadeCommand>
{
    public GerarMensalidadeCommandValidator()
    {
        RuleFor(r => r.PacienteId).NotEmpty();
        RuleFor(r => r.Competencia)
            .NotEmpty()
            .Matches("^\\d{4}-\\d{2}$")
            .WithMessage("Competência deve estar no formato yyyy-MM.");
        RuleFor(r => r.Valor).GreaterThan(0);
    }
}

public sealed class CadastrarContaPagarCommandValidator : AbstractValidator<CadastrarContaPagarCommand>
{
    public CadastrarContaPagarCommandValidator()
    {
        RuleFor(r => r.Fornecedor).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Descricao).NotEmpty().MaximumLength(300);
        RuleFor(r => r.Valor).GreaterThan(0);
        RuleFor(r => r.DataVencimento).NotEmpty();
        RuleFor(r => r.TipoCusto).IsInEnum();
    }
}

public sealed class GerarFaturamentoRecorrenteCommandValidator
    : AbstractValidator<GerarFaturamentoRecorrenteCommand>
{
    public GerarFaturamentoRecorrenteCommandValidator()
    {
        RuleFor(r => r.Competencia)
            .NotEmpty()
            .Matches("^\\d{4}-\\d{2}$")
            .WithMessage("Competência deve estar no formato yyyy-MM.");
    }
}

public sealed class EmitirCobrancaCommandValidator : AbstractValidator<EmitirCobrancaCommand>
{
    public EmitirCobrancaCommandValidator()
    {
        RuleFor(r => r.MensalidadeId).NotEmpty();
        RuleFor(r => r.Tipo).IsInEnum();
    }
}