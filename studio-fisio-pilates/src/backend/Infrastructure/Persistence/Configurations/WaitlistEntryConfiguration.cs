using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable("waitlist_entries");

        builder.HasKey(e => e.Id);

        // Apenas UMA entrada ativa por (tenant, turma, paciente).
        // Filtro com aspas: a coluna é criada como "Ativo" (case preservado
        // pelo Npgsql); sem aspas o Postgres resolveria minúsculo e falharia.
        builder.HasIndex(e => new { e.ClinicaId, e.TurmaId, e.PacienteId })
            .HasDatabaseName("IX_waitlist_ativa_unica")
            .IsUnique()
            .HasFilter("\"Ativo\" = true");

        builder.HasIndex(e => new { e.ClinicaId, e.TurmaId })
            .HasDatabaseName("IX_waitlist_tenant_turma");

        builder.HasOne<Turma>()
            .WithMany()
            .HasForeignKey(e => e.TurmaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Paciente)
            .WithMany()
            .HasForeignKey(e => e.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
