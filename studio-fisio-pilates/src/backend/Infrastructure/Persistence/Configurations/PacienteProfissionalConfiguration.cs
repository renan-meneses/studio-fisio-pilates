using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("pacientes");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ClinicaId, p.CPF })
            .IsUnique()
            .HasDatabaseName("IX_pacientes_tenant_cpf");

        builder.HasIndex(p => new { p.ClinicaId, p.Nome })
            .HasDatabaseName("IX_pacientes_tenant_nome");

        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Sobrenome).HasMaxLength(150);
        builder.Property(p => p.CPF).HasMaxLength(11);
        builder.Property(p => p.Telefone).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(120);
        builder.Property(p => p.Endereco).HasMaxLength(255);
        builder.Property(p => p.Indicacao).HasMaxLength(255);
        builder.Property(p => p.Observacoes).HasMaxLength(1000);

        builder.HasOne(p => p.Plano)
            .WithMany()
            .HasForeignKey(p => p.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> builder)
    {
        builder.ToTable("profissionais");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ClinicaId, p.CPF })
            .IsUnique()
            .HasDatabaseName("IX_profissionais_tenant_cpf");

        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.CPF).HasMaxLength(11).IsRequired();
        builder.Property(p => p.RegistroProfissional).HasMaxLength(30);
        builder.Property(p => p.Especialidades).HasMaxLength(255);
        builder.Property(p => p.Cargo).HasMaxLength(80);
        builder.Property(p => p.Telefone).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(120);
        builder.Property(p => p.SalarioBase).HasPrecision(18, 2);
    }
}