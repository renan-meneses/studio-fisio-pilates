using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class MensalidadeConfiguration : IEntityTypeConfiguration<Mensalidade>
{
    public void Configure(EntityTypeBuilder<Mensalidade> builder)
    {
        builder.ToTable("mensalidades");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.ClinicaId, m.PacienteId, m.Competencia })
            .IsUnique()
            .HasDatabaseName("IX_mensalidades_tenant_paciente_competencia");

        builder.HasIndex(m => new { m.ClinicaId, m.Status })
            .HasDatabaseName("IX_mensalidades_tenant_status");

        builder.Property(m => m.Competencia).HasMaxLength(7).IsRequired();
        builder.Property(m => m.Valor).HasPrecision(18, 2);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(24);

        builder.HasOne(m => m.Paciente)
            .WithMany()
            .HasForeignKey(m => m.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class ContaPagarConfiguration : IEntityTypeConfiguration<ContaPagar>
{
    public void Configure(EntityTypeBuilder<ContaPagar> builder)
    {
        builder.ToTable("contas_pagar");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.ClinicaId, c.Status, c.DataVencimento })
            .HasDatabaseName("IX_contaspagar_tenant_status_vencimento");

        builder.Property(c => c.Fornecedor).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Descricao).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Valor).HasPrecision(18, 2);
        builder.Property(c => c.TipoCusto).HasConversion<string>().HasMaxLength(24);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(24);
    }
}

internal class PontoConfiguration : IEntityTypeConfiguration<Ponto>
{
    public void Configure(EntityTypeBuilder<Ponto> builder)
    {
        builder.ToTable("pontos");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ClinicaId, p.ProfissionalId, p.Data })
            .IsUnique()
            .HasDatabaseName("IX_pontos_tenant_profissional_data");

        builder.HasOne(p => p.Profissional)
            .WithMany()
            .HasForeignKey(p => p.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class FolhaConfiguration : IEntityTypeConfiguration<FolhaSalarial>
{
    public void Configure(EntityTypeBuilder<FolhaSalarial> builder)
    {
        builder.ToTable("folhas_salariais");

        builder.HasKey(f => f.Id);

        builder.HasIndex(f => new { f.ClinicaId, f.ProfissionalId, f.Competencia })
            .IsUnique()
            .HasDatabaseName("IX_folhas_tenant_profissional_competencia");

        builder.Property(f => f.Competencia).HasMaxLength(7).IsRequired();
        builder.Property(f => f.ValorBruto).HasPrecision(18, 2);
        builder.Property(f => f.Descontos).HasPrecision(18, 2);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(24);
    }
}