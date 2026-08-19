using System.Linq;
using FluentValidation.Results;

namespace Clinica.Application.Common.Exceptions;

/// <summary>Erros de validação FluentValidation — mapeados para HTTP 400.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("Um ou mais campos são inválidos.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}