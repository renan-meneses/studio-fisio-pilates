using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.ClinicaId, r.Key, r.Method, r.Path })
            .IsUnique()
            .HasDatabaseName("IX_idempotency_tenant_key_method_path");

        builder.HasIndex(r => new { r.ClinicaId, r.ExpiresAtUtc })
            .HasDatabaseName("IX_idempotency_tenant_expira");

        builder.Property(r => r.Key).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Method).HasMaxLength(10).IsRequired();
        builder.Property(r => r.Path).HasMaxLength(300).IsRequired();
        builder.Property(r => r.ResponseBody).HasColumnType("text");
    }
}