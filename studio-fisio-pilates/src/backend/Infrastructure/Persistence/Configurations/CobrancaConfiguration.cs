using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class CobrancaConfiguration : IEntityTypeConfiguration<Cobranca>
{
    public void Configure(EntityTypeBuilder<Cobranca> builder)
    {
        builder.ToTable("cobrancas");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.ClinicaId, c.MensalidadeId })
            .HasDatabaseName("IX_cobrancas_tenant_mensalidade");

        builder.HasIndex(c => new { c.Provedor, c.ProvedorCobrancaId })
            .IsUnique()
            .HasDatabaseName("IX_cobrancas_provedor_id");

        builder.Property(c => c.Provedor).HasMaxLength(40).IsRequired();
        builder.Property(c => c.ProvedorCobrancaId).HasMaxLength(120).IsRequired();
        builder.Property(c => c.PixCopiaECola).HasMaxLength(500);
        builder.Property(c => c.BoletoLinhaDigitavel).HasMaxLength(100);
        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(12);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(12);
    }
}