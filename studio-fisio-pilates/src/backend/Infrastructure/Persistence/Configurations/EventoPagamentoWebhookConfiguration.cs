using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class EventoPagamentoWebhookConfiguration : IEntityTypeConfiguration<EventoPagamentoWebhook>
{
    public void Configure(EntityTypeBuilder<EventoPagamentoWebhook> builder)
    {
        builder.ToTable("eventos_pagamento_webhook");

        builder.HasKey(e => e.Id);

        // Dedupe: o mesmo evento do provedor nunca é processado duas vezes.
        builder.HasIndex(e => new { e.ClinicaId, e.Provedor, e.EventoId })
            .IsUnique()
            .HasDatabaseName("IX_webhook_tenant_provedor_evento");

        builder.HasIndex(e => e.Processado)
            .HasDatabaseName("IX_webhook_nao_processados")
            .HasFilter("\"Processado\" = false");

        builder.Property(e => e.Provedor).HasMaxLength(40).IsRequired();
        builder.Property(e => e.EventoId).HasMaxLength(120).IsRequired();
        builder.Property(e => e.TipoEvento).HasMaxLength(60).IsRequired();
        builder.Property(e => e.Payload).HasColumnType("text");
        builder.Property(e => e.ErroProcessamento).HasMaxLength(1000);
    }
}