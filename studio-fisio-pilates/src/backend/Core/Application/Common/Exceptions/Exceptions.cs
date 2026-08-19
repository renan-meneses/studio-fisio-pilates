namespace Clinica.Application.Common.Exceptions;

/// <summary>Regra de negócio violada — mapeada para HTTP 409 Conflict.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// <summary>Recurso não encontrado no tenant ativo — HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Credenciais inválidas ou acesso negado — HTTP 401/403.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}