using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class PlanoConfiguration : IEntityTypeConfiguration<Plano>
{
    public void Configure(EntityTypeBuilder<Plano> builder)
    {
        builder.ToTable("planos");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ClinicaId, p.Nome })
            .IsUnique()
            .HasDatabaseName("IX_planos_tenant_nome");

        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(500);
        builder.Property(p => p.Valor).HasPrecision(18, 2);
    }
}

internal class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("servicos");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.ClinicaId, s.Nome })
            .IsUnique()
            .HasDatabaseName("IX_servicos_tenant_nome");

        builder.Property(s => s.Nome).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Descricao).HasMaxLength(500);
        builder.Property(s => s.Valor).HasPrecision(18, 2);
    }
}

internal class PlanoServicoConfiguration : IEntityTypeConfiguration<PlanoServico>
{
    public void Configure(EntityTypeBuilder<PlanoServico> builder)
    {
        builder.ToTable("planos_servicos");

        builder.HasKey(ps => ps.Id);

        builder.HasIndex(ps => new { ps.PlanoId, ps.ServicoId })
            .IsUnique()
            .HasDatabaseName("IX_planos_servicos_plano_servico");

        builder.HasOne(ps => ps.Plano)
            .WithMany(p => p.PlanoServicos)
            .HasForeignKey(ps => ps.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ps => ps.Servico)
            .WithMany(s => s.PlanoServicos)
            .HasForeignKey(ps => ps.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}