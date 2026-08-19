using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        builder.ToTable("turmas");

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.ClinicaId, t.TipoSessao })
            .HasDatabaseName("IX_turmas_tenant_tipo");

        builder.Property(t => t.Nome).HasMaxLength(120);

        builder.Property(t => t.TipoSessao).HasConversion<string>().HasMaxLength(32);

        builder.HasOne(t => t.Profissional)
            .WithMany()
            .HasForeignKey(t => t.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Horarios)
            .WithOne(h => h.Turma)
            .HasForeignKey(h => h.TurmaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class TurmaHorarioConfiguration : IEntityTypeConfiguration<TurmaHorario>
{
    public void Configure(EntityTypeBuilder<TurmaHorario> builder)
    {
        builder.ToTable("turmas_horarios");

        builder.HasKey(h => h.Id);

        // Um mesmo horário não pode ser repetido na turma.
        builder.HasIndex(h => new { h.TurmaId, h.DiaSemana, h.HoraInicio })
            .IsUnique()
            .HasDatabaseName("IX_turmas_horarios_turma_dia_hora");
    }
}