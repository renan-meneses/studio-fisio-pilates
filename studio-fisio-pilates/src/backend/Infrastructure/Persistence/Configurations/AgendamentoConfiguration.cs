using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("agendamentos");

        builder.HasKey(a => a.Id);

        // Índice composto multitenant: consultas de agenda por tenant + janela temporal.
        builder.HasIndex(a => new { a.ClinicaId, a.DataHoraInicio })
            .HasDatabaseName("IX_agendamentos_tenant_inicio");

        builder.HasIndex(a => new { a.ClinicaId, a.ProfissionalId, a.DataHoraInicio })
            .HasDatabaseName("IX_agendamentos_tenant_profissional");

        builder.Property(a => a.TipoSessao).HasConversion<string>().HasMaxLength(32);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(24);

        builder.Property(a => a.ValorSessao).HasPrecision(18, 2);

        builder.HasOne(a => a.Paciente)
            .WithMany()
            .HasForeignKey(a => a.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Profissional)
            .WithMany()
            .HasForeignKey(a => a.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Presenca)
            .WithOne(p => p.Agendamento)
            .HasForeignKey<Presenca>(p => p.AgendamentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}