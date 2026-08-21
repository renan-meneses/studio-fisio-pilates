using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Auth;
using Clinica.Application.Features.Usuarios;
using Clinica.CrossCutting.Auth;
using Clinica.Domain.Entities;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.UnitTests.Auth;

public class UsuarioFeaturesTests
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

    private static readonly IPasswordHasher Hasher = new PasswordHasher();

    private static async Task<Usuario> CriarUsuarioAsync(
        TenantDbContext db, string email = "ana@teste.local", string senha = "Senha@1234")
    {
        var handler = new CriarUsuarioCommandHandler(db, Hasher);
        var id = await handler.Handle(
            new CriarUsuarioCommand("Ana Atendente", email, senha, "Atendente"),
            CancellationToken.None);

        return await db.Usuarios.SingleAsync(u => u.Id == id);
    }

    [Fact]
    public async Task CriarUsuario_NormalizaEmailEGeraHashVerificavel()
    {
        using var db = CriarDb();

        var usuario = await CriarUsuarioAsync(db, email: "  ANA@Teste.Local ");

        usuario.Email.Should().Be("ana@teste.local");
        usuario.Papel.Should().Be(PapelUsuario.Atendente);
        usuario.Ativo.Should().BeTrue();
        Hasher.Verify("Senha@1234", usuario.SenhaHash).Should().BeTrue();
    }

    [Fact]
    public async Task CriarUsuario_EmailDuplicadoLancaBusinessRule()
    {
        using var db = CriarDb();
        await CriarUsuarioAsync(db, email: "ana@teste.local");

        var handler = new CriarUsuarioCommandHandler(db, Hasher);
        var acao = () => handler.Handle(
            new CriarUsuarioCommand("Cópia", "ana@teste.local", "Senha@1234", "Atendente"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*e-mail*");
    }

    [Fact]
    public async Task AlterarStatus_NaoPermiteDesativarProprioUsuario()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);

        var handler = new AlterarStatusUsuarioCommandHandler(db);
        var acao = () => handler.Handle(
            new AlterarStatusUsuarioCommand(usuario.Id, usuario.Id, Ativo: false),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*próprio*");
        db.Usuarios.Single(u => u.Id == usuario.Id).Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task AlterarStatus_DesativaEReativaUsuario()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);
        var adminId = Guid.NewGuid();

        var handler = new AlterarStatusUsuarioCommandHandler(db);
        await handler.Handle(new AlterarStatusUsuarioCommand(adminId, usuario.Id, Ativo: false), CancellationToken.None);
        db.Usuarios.Single(u => u.Id == usuario.Id).Ativo.Should().BeFalse();

        await handler.Handle(new AlterarStatusUsuarioCommand(adminId, usuario.Id, Ativo: true), CancellationToken.None);
        db.Usuarios.Single(u => u.Id == usuario.Id).Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task RedefinirSenha_TrocaHashEAntigaDeixaDeValidar()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);

        var handler = new RedefinirSenhaCommandHandler(db, Hasher);
        await handler.Handle(new RedefinirSenhaCommand(usuario.Id, "NovaSenha@987"), CancellationToken.None);

        var atualizado = await db.Usuarios.SingleAsync(u => u.Id == usuario.Id);
        Hasher.Verify("Senha@1234", atualizado.SenhaHash).Should().BeFalse();
        Hasher.Verify("NovaSenha@987", atualizado.SenhaHash).Should().BeTrue();
    }

    [Fact]
    public async Task RedefinirSenha_UsuarioInexistenteLancaNotFound()
    {
        using var db = CriarDb();

        var handler = new RedefinirSenhaCommandHandler(db, Hasher);
        var acao = async () => await handler.Handle(new RedefinirSenhaCommand(Guid.NewGuid(), "NovaSenha@987"), CancellationToken.None);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AlterarSenhaPropria_SenhaAtualIncorretaLancaBusinessRule()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);

        var handler = new AlterarSenhaPropriaCommandHandler(db, Hasher);
        var acao = () => handler.Handle(
            new AlterarSenhaPropriaCommand(usuario.Id, "Errada@123", "NovaSenha@987"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*incorreta*");
    }

    [Fact]
    public async Task AlterarSenhaPropria_ComSenhaAtualCorretaTrocAHash()
    {
        using var db = CriarDb();
        var usuario = await CriarUsuarioAsync(db);

        var handler = new AlterarSenhaPropriaCommandHandler(db, Hasher);
        await handler.Handle(
            new AlterarSenhaPropriaCommand(usuario.Id, "Senha@1234", "NovaSenha@987"),
            CancellationToken.None);

        var atualizado = await db.Usuarios.SingleAsync(u => u.Id == usuario.Id);
        Hasher.Verify("NovaSenha@987", atualizado.SenhaHash).Should().BeTrue();
    }
}
