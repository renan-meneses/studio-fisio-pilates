namespace Clinica.Domain.Common;

/// <summary>Marca raízes de agregação (DDD). Facilita repositórios transacionais.</summary>
public interface IAggregateRoot
{
}

/// <summary>Versionamento otimista (não utilizado diretamente pelo EF Core nesta versão).</summary>
public interface IVersionable
{
    int Version { get; }
}