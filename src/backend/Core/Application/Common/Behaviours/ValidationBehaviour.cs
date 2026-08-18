using FluentValidation;
using MediatR;

namespace Clinica.Application.Common.Behaviours;

/// <summary>
/// Pipeline do MediatR: valida automaticamente todo request com
/// IValidator&lt;TRequest&gt; registrado antes do handler executar.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, ct)));

            var errors = failures
                .SelectMany(f => f.Errors)
                .Where(e => e is not null)
                .ToList();

            if (errors.Count > 0)
                throw new Exceptions.ValidationException(errors);
        }

        return await next();
    }
}