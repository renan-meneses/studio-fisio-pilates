namespace Clinica.Domain.Repositories;

/// <summary>
/// Contrato de persistência para raízes de agregação.
/// Implementações ficam em Infrastructure/Persistence (EF Core).
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}