using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using ClinicaEntity = Clinica.Domain.Entities.Clinica;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Persistence.Initialization;

/// <summary>
/// Aplica migrations e popula dados de demonstração no primeiro boot (dev).
///
/// Bancos criados pelo antigo <c>EnsureCreated</c> (sem histórico de
/// migrations) são adotados: a migration baseline é marcada como aplicada
/// e as migrations posteriores seguem o fluxo normal.
///
/// Também garante um usuário administrador: a clínica demo recebe
/// <c>admin@demo.clinica</c> (dev) e, quando <see cref="AdminBootstrapOptions"/>
/// estiver configurado, o admin indicado é criado de forma idempotente
/// (qualquer ambiente, inclusive produção).
/// </summary>
public static class DatabaseInitializer
{
    private const string EfMigrationsHistoryTable = "__EFMigrationsHistory";
    private const string EfCoreProductVersion = "8.0.10";

    /// <summary>Login padrão da clínica demo (apenas dev).</summary>
    public const string DemoAdminEmail = "admin@demo.clinica";
    public const string DemoAdminSenha = "Admin@Demo123";

    public static async Task InitializeAsync(
        TenantDbContext context,
        IPasswordHasher passwordHasher,
        AdminBootstrapOptions? bootstrap = null,
        CancellationToken ct = default)
    {
        await ApplyMigrationsAdoptingLegacySchemaAsync(context, ct);

        if (!await context.Clinicas.AnyAsync(ct))
        {
            await SeedDadosDemoAsync(context, passwordHasher, ct);
        }

        if (bootstrap is { Configurado: true })
        {
            await EnsureAdminUserAsync(context, passwordHasher, bootstrap, ct);
        }
    }

    /// <summary>
    /// Cria o usuário administrador descrito em <paramref name="options"/>
    /// caso ainda não exista (busca por e-mail ignora o filtro de tenant,
    /// pois o initializer roda fora do pipeline de requisições).
    /// </summary>
    public static async Task EnsureAdminUserAsync(
        TenantDbContext context,
        IPasswordHasher passwordHasher,
        AdminBootstrapOptions options,
        CancellationToken ct = default)
    {
        if (!options.Configurado)
            return;

        var existe = await context.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == options.Email, ct);

        if (existe)
            return;

        var clinicaId = options.ClinicaId
            ?? await context.Clinicas
                .OrderBy(c => c.Id)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                "AdminBootstrap configurado, mas nenhuma clínica existe no banco.");

        await context.Usuarios.AddAsync(new Usuario
        {
            ClinicaId = clinicaId,
            Nome = string.IsNullOrWhiteSpace(options.Nome) ? "Administrador" : options.Nome,
            Email = options.Email,
            SenhaHash = passwordHasher.Hash(options.Senha),
            Papel = PapelUsuario.Administrador,
        }, ct);

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedDadosDemoAsync(
        TenantDbContext context,
        IPasswordHasher passwordHasher,
        CancellationToken ct)
    {
        var clinicaDemo = new ClinicaEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Nome = "Clínica Demonstração",
            CNPJ = "00000000000100",
            Email = "contato@demo.clinica",
            Plano = PlanoContratacao.Profissional,
        };

        await context.Clinicas.AddAsync(clinicaDemo, ct);

        var paciente = new Paciente
        {
            ClinicaId = clinicaDemo.Id,
            Nome = "Maria da Silva",
            CPF = "12345678901",
            DataNascimento = new DateTime(1990, 5, 14),
            Telefone = "11999999999",
        };

        var profissional = new Profissional
        {
            ClinicaId = clinicaDemo.Id,
            Nome = "Dr. João Pereira",
            CPF = "98765432100",
            RegistroProfissional = "CREFITO 12345-F",
            Especialidades = "Fisioterapia, Pilates Clínico",
            SalarioBase = 4500m,
        };

        await context.Pacientes.AddAsync(paciente, ct);
        await context.Profissionais.AddAsync(profissional, ct);

        await EnsureAdminUserAsync(context, passwordHasher, new AdminBootstrapOptions
        {
            Nome = "Administrador Demo",
            Email = DemoAdminEmail,
            Senha = DemoAdminSenha,
            ClinicaId = clinicaDemo.Id,
        }, ct);
    }

    /// <summary>
    /// Aplica as migrations, adotando bancos criados antes da existência de
    /// migrations (EnsureCreated): schema presente sem <c>__EFMigrationsHistory</c>.
    /// A migration baseline é registrada como aplicada e as demais rodam.
    /// </summary>
    private static async Task ApplyMigrationsAdoptingLegacySchemaAsync(
        TenantDbContext context,
        CancellationToken ct)
    {
        var schemaExists = await TableExistsAsync(context, "Clinicas", ct);
        var historyExists = await TableExistsAsync(context, EfMigrationsHistoryTable, ct);

        if (schemaExists && !historyExists)
        {
            var baselineId = context.Database.GetMigrations().FirstOrDefault();
            if (baselineId is null)
                return;

            await context.Database.ExecuteSqlRawAsync(
                $"""
                 CREATE TABLE IF NOT EXISTS "{EfMigrationsHistoryTable}"
                 (
                     "MigrationId" text NOT NULL,
                     "ProductVersion" text NOT NULL,
                     PRIMARY KEY ("MigrationId")
                 );
                 """, ct);

            await context.Database.ExecuteSqlRawAsync(
                $$"""
                 INSERT INTO "{{EfMigrationsHistoryTable}}" ("MigrationId", "ProductVersion")
                 VALUES ({0}, {1})
                 ON CONFLICT ("MigrationId") DO NOTHING;
                 """,
                new object[] { baselineId, EfCoreProductVersion },
                ct);
        }

        await context.Database.MigrateAsync(ct);
    }

    private static async Task<bool> TableExistsAsync(
        TenantDbContext context,
        string tableName,
        CancellationToken ct)
    {
        var existe = await context.Database
            .SqlQueryRaw<int>(
                "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = {0}",
                tableName)
            .AnyAsync(ct);
        return existe;
    }
}