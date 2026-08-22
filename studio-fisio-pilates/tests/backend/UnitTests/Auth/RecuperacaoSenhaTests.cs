using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Auth;
using Clinica.CrossCutting.Auth;
using Clinica.Domain.Entities;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Clinica.UnitTests.Agenda;
using Xunit;

namespace Clinica.UnitTests.Auth;

public class RecuperacaoSenhaTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static TenantDbContext CriarDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(Tenant).Object);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<Usuario> CriarUsuarioAsync(TenantDbContext db, string email = "maria@teste.local")
    {
        var hasher = new PasswordHasher();
        var usuario = new Usuario
        {
            ClinicaId = Tenant,
            Nome = "Maria",
            Email = email,
            SenhaHash = hasher.Hash("SenhaAntiga@1"),
            Papel = PapelUsuario.Atendente,
            Ativo = true,
        };
        await db.Usuarios.AddAsync(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static (SolicitarRedefinicaoCommandHandler Solicitar, NotificacaoSpy Spy) SolicitarHandler(
        TenantDbContext db)
    {
        var spy = new NotificacaoSpy();
        return (new SolicitarRedefinicaoCommandHandler(db, spy), spy);
    }

    [Fact]
    public async Task Solicitar_persiste_apenas_hash_e_envia_token_bruto()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);
        var (solicitar, spy) = SolicitarHandler(db);

        await solicitar.Handle(new SolicitarRedefinicaoCommand("Maria@Teste.Local"), CancellationToken.None);

        var token = await db.TokensRedefinicaoSenha.IgnoreQueryFilters().SingleAsync();
        token.UsuarioId.Should().Be(usuario.Id);
        token.UsadoEm.Should().BeNull();
        token.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));

        var bruto = spy.Mensagens.Single().Corpo.Split('\n').Last().Trim();
        bruto.Should().NotBe(token.TokenHash);
        SolicitarRedefinicaoCommandHandler.HashDe(bruto).Should().Be(token.TokenHash);
    }

    [Fact]
    public async Task Solicitar_email_inexistente_nao_notifica_nem_cria_token()
    {
        using var db = CriarDb();
        var (solicitar, spy) = SolicitarHandler(db);

        await solicitar.Handle(new SolicitarRedefinicaoCommand("fantasma@teste.local"), CancellationToken.None);

        db.TokensRedefinicaoSenha.IgnoreQueryFilters().Should().BeEmpty();
        spy.Mensagens.Should().BeEmpty();
    }

    [Fact]
    public async Task Nova_solicitacao_invalida_tokens_anteriores()
    {
        using var db = CriarDb();
        await CriarUsuarioAsync(db);
        var (solicitar, spy) = SolicitarHandler(db);

        await solicitar.Handle(new SolicitarRedefinicaoCommand("maria@teste.local"), CancellationToken.None);
        await solicitar.Handle(new SolicitarRedefinicaoCommand("maria@teste.local"), CancellationToken.None);

        var tokens = await db.TokensRedefinicaoSenha.IgnoreQueryFilters().ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Count(t => t.UsadoEm == null).Should().Be(1, "apenas o token mais recente permanece válido");
    }

    [Fact]
    public async Task Redefinir_com_token_valido_troca_senha_e_consome_token()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);
        var (solicitar, spy) = SolicitarHandler(db);
        await solicitar.Handle(new SolicitarRedefinicaoCommand("maria@teste.local"), CancellationToken.None);
        var tokenBruto = spy.Mensagens.Single().Corpo.Split('\n').Last().Trim();

        var hasher = new PasswordHasher();
        await new RedefinirSenhaTokenCommandHandler(db, hasher).Handle(
            new RedefinirSenhaTokenCommand("maria@teste.local", tokenBruto, "SenhaNova@2"),
            CancellationToken.None);

        var atualizado = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == usuario.Id);
        hasher.Verify("SenhaAntiga@1", atualizado.SenhaHash).Should().BeFalse();
        hasher.Verify("SenhaNova@2", atualizado.SenhaHash).Should().BeTrue();

        var token = await db.TokensRedefinicaoSenha.IgnoreQueryFilters().SingleAsync();
        token.UsadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Redefinir_token_reutilizado_ou_expirado_falha()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);
        var hasher = new PasswordHasher();
        var handler = new RedefinirSenhaTokenCommandHandler(db, hasher);

        var expirado = new TokenRedefinicaoSenha
        {
            ClinicaId = Tenant,
            UsuarioId = usuario.Id,
            TokenHash = SolicitarRedefinicaoCommandHandler.HashDe("token-expirado"),
            ExpiraEm = DateTime.UtcNow.AddMinutes(-5),
        };
        await db.TokensRedefinicaoSenha.AddAsync(expirado);
        await db.SaveChangesAsync();

        var acaoExpirado = async () => await handler.Handle(
            new RedefinirSenhaTokenCommand("maria@teste.local", "token-expirado", "SenhaNova@2"),
            CancellationToken.None);
        await acaoExpirado.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*inválido ou expirado*");

        // Mesmo token, agora dentro da validade: primeira redefinição funciona…
        expirado.ExpiraEm = DateTime.UtcNow.AddMinutes(30);
        await db.SaveChangesAsync();
        await handler.Handle(
            new RedefinirSenhaTokenCommand("maria@teste.local", "token-expirado", "SenhaNova@2"),
            CancellationToken.None);

        // …e a segunda tentativa falha por uso único.
        var acaoReuso = async () => await handler.Handle(
            new RedefinirSenhaTokenCommand("maria@teste.local", "token-expirado", "SenhaNova@3"),
            CancellationToken.None);
        await acaoReuso.Should().ThrowAsync<BusinessRuleException>();
    }
}
