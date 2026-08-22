using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinica.Persistence.Configurations;

internal class TokenRedefinicaoSenhaConfiguration : IEntityTypeConfiguration<TokenRedefinicaoSenha>
{
    public void Configure(EntityTypeBuilder<TokenRedefinicaoSenha> builder)
    {
        builder.ToTable("tokens_redefinicao_senha");

        builder.HasKey(t => t.Id);

        // Lookup do fluxo de redefinição é sempre por hash do token.
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_tokens_redefinicao_hash");

        builder.Property(t => t.TokenHash).HasMaxLength(100).IsRequired();

        builder.HasOne(t => t.Usuario)
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
