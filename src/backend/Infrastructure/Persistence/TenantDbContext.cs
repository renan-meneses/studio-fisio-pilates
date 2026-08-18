using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Common;
using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Persistence;

/// <summary>
/// DbContext multitenant do EF Core 8.
///
/// Isolamento lógico por <see cref="ITenantEntity.ClinicaId"/>:
///  - Global Query Filters aplicados via <see cref="QueryFilters.TenantQueryFilterBuilder"/>
///    (leitura: qualquer query retorna apenas dados do tenant ativo);
///  - TenantSaveChangesInterceptor (escrita: injeta ClinicaId em inserts).
/// </summary>
public class TenantDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentTenantService _tenant;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ICurrentTenantService tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Clinica> Clinicas => Set<Clinica>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Presenca> Presencas => Set<Presenca>();
    public DbSet<ProntuarioEletronico> Prontuarios => Set<ProntuarioEletronico>();
    public DbSet<EvolucaoClinica> Evolucoes => Set<EvolucaoClinica>();
    public DbSet<Mensalidade> Mensalidades => Set<Mensalidade>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
    public DbSet<Ponto> Pontos => Set<Ponto>();
    public DbSet<FolhaSalarial> FolhasSalariais => Set<FolhaSalarial>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);
        QueryFilters.TenantQueryFilterBuilder.ApplyGlobalFilters(builder, _tenant);
        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Garantia extra de que toda aplicação passa pelo interceptor de tenant.
        optionsBuilder.AddInterceptors(new Interceptors.TenantSaveChangesInterceptor(_tenant));
    }
}