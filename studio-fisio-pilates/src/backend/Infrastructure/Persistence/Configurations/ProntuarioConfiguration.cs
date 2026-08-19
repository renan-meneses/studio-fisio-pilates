using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class ProntuarioConfiguration : IEntityTypeConfiguration<ProntuarioEletronico>
{
    public void Configure(EntityTypeBuilder<ProntuarioEletronico> builder)
    {
        builder.ToTable("prontuarios");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ClinicaId, p.PacienteId })
            .IsUnique()
            .HasDatabaseName("IX_prontuarios_tenant_paciente");

        builder.HasMany(p => p.Evolucoes)
            .WithOne(e => e.Prontuario)
            .HasForeignKey(e => e.ProntuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class EvolucaoConfiguration : IEntityTypeConfiguration<EvolucaoClinica>
{
    public void Configure(EntityTypeBuilder<EvolucaoClinica> builder)
    {
        builder.ToTable("evolucoes");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.ClinicaId, e.ProntuarioId, e.Data })
            .HasDatabaseName("IX_evolucoes_tenant_prontuario_data");

        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(24);

        builder.Property(e => e.QueixaPrincipal).HasMaxLength(500);
        builder.Property(e => e.Avaliacao).HasMaxLength(2000);
        builder.Property(e => e.Conduta).HasMaxLength(2000);
        builder.Property(e => e.Observacoes).HasMaxLength(1000);
    }
}