using Npgsql;
using Respawn;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Fixture de classe por teste: apenas limpa o banco (Respawn) antes de
/// cada teste, aproveitando o container/factory compartilhado da coleção.
/// </summary>
public sealed class ResetDb : IAsyncLifetime
{
    private Respawner? _respawner;

    public ResetDb(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    public ApiFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(Fixture.ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
        // Limpa estado residual do teste anterior garantindo determinismo.
        await _respawner.ResetAsync(conn);
    }

    public Task DisposeAsync()
    {
        // O banco será limpo no próximo InitializeAsync; nada a fazer aqui.
        return Task.CompletedTask;
    }
}