using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Application.Features.Financeiro;
using Clinica.CrossCutting.Pagamentos;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using Clinica.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Clinica.UnitTests.Financeiro;

/// <summary>
/// Cobranças: emissão idempotente, dedupe de webhook e liquidação
/// atômica de cobrança + mensalidade.
/// </summary>
public class CobrancasTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid PacienteId = Guid.NewGuid();

    private static TenantDbContext CriarDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TenantDbContext(options, TenantTestFactory.TenantOf(Tenant).Object);
        db.Database.EnsureCreated();

        db.Pacientes.Add(new Paciente
        {
            Id = PacienteId,
            ClinicaId = Tenant,
            Nome = "Paciente Cobrança",
            CPF = "22222222222",
        });
        db.SaveChanges();
        return db;
    }

    private static async Task<Mensalidade> SeedMensalidadeAsync(TenantDbContext db, decimal valor = 320m)
    {
        var mensalidade = new Mensalidade
        {
            ClinicaId = Tenant,
            PacienteId = PacienteId,
            Competencia = "2026-08",
            Valor = valor,
            DataVencimento = new DateTime(2026, 8, 10),
        };
        await db.Mensalidades.AddAsync(mensalidade);
        await db.SaveChangesAsync();
        return mensalidade;
    }

    private static ICurrentTenantAccessor Accessor() =>
        Mock.Of<ICurrentTenantAccessor>();

    [Fact]
    public async Task EmitirCobranca_CriaPixComDadosDoGateway()
    {
        using var db = CriarDb();
        var mensalidade = await SeedMensalidadeAsync(db);
        var handler = new EmitirCobrancaCommandHandler(db, new SimulatedPaymentGateway());

        var resposta = await handler.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Pix), CancellationToken.None);

        resposta.Status.Should().Be(StatusCobranca.Pendente);
        resposta.Provedor.Should().Be("simulado");
        resposta.PixCopiaECola.Should().StartWith("00020126");
        resposta.BoletoLinhaDigitavel.Should().BeNull();
        resposta.ExpiraEmUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task EmitirCobranca_ReaproveitaPendenteNoRetry()
    {
        using var db = CriarDb();
        var mensalidade = await SeedMensalidadeAsync(db);
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(g => g.Nome).Returns("mock");
        gateway.Setup(g => g.CriarCobrancaAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<TipoCobranca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CobrancaGatewayResult("mock_1", null, "linha", DateTime.UtcNow.AddDays(1)));
        var handler = new EmitirCobrancaCommandHandler(db, gateway.Object);

        var primeira = await handler.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Boleto), CancellationToken.None);
        var segunda = await handler.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Boleto), CancellationToken.None);

        segunda.Id.Should().Be(primeira.Id, "retry deve devolver a cobrança pendente existente");
        gateway.Verify(g => g.CriarCobrancaAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<TipoCobranca>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmitirCobranca_MensalidadePagaLancaBusinessRule()
    {
        using var db = CriarDb();
        var mensalidade = await SeedMensalidadeAsync(db);
        mensalidade.RegistrarPagamento(DateTime.UtcNow);
        await db.SaveChangesAsync();
        var handler = new EmitirCobrancaCommandHandler(db, new SimulatedPaymentGateway());

        var ato = async () => await handler.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Pix), CancellationToken.None);

        await ato.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Webhook_LiquidaCobrancaEMensalidade()
    {
        using var db = CriarDb();
        var mensalidade = await SeedMensalidadeAsync(db);
        var emitir = new EmitirCobrancaCommandHandler(db, new SimulatedPaymentGateway());
        var cobranca = await emitir.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Pix), CancellationToken.None);

        var handler = new ProcessarWebhookPagamentoCommandHandler(db, Accessor());
        var resultado = await handler.Handle(new ProcessarWebhookPagamentoCommand(
            "simulado", "evt_1", "pagamento.confirmado", cobranca.Id,
            DateTime.UtcNow, "{}"), CancellationToken.None);

        resultado.Duplicado.Should().BeFalse();
        resultado.Processado.Should().BeTrue();

        (await db.Cobrancas.SingleAsync()).Status.Should().Be(StatusCobranca.Paga);
        (await db.Mensalidades.SingleAsync()).Status.Should().Be(StatusMensalidade.Paga);
    }

    [Fact]
    public async Task Webhook_ReplayDoMesmoEventoNaoReprocessa()
    {
        using var db = CriarDb();
        var mensalidade = await SeedMensalidadeAsync(db);
        var emitir = new EmitirCobrancaCommandHandler(db, new SimulatedPaymentGateway());
        var cobranca = await emitir.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Pix), CancellationToken.None);

        var handler = new ProcessarWebhookPagamentoCommandHandler(db, Accessor());
        var comando = new ProcessarWebhookPagamentoCommand(
            "simulado", "evt_replay", "pagamento.confirmado", cobranca.Id,
            DateTime.UtcNow, "{}");

        await handler.Handle(comando, CancellationToken.None);
        var replay = await handler.Handle(comando, CancellationToken.None);

        replay.Duplicado.Should().BeTrue();
        (await db.EventosPagamentoWebhook.CountAsync()).Should().Be(1);
        (await db.Mensalidades.SingleAsync()).DataPagamento.Should().NotBeNull();
    }

    [Fact]
    public async Task Webhook_CobrancaJaPagaRegistraFalhaSemAlterar()
    {
        using var db = CriarDb();
        var mensalidade = await SeedMensalidadeAsync(db);
        var emitir = new EmitirCobrancaCommandHandler(db, new SimulatedPaymentGateway());
        var cobranca = await emitir.Handle(
            new EmitirCobrancaCommand(mensalidade.Id, TipoCobranca.Pix), CancellationToken.None);

        var handler = new ProcessarWebhookPagamentoCommandHandler(db, Accessor());
        var pagoEm = DateTime.UtcNow.AddMinutes(-5);
        await handler.Handle(new ProcessarWebhookPagamentoCommand(
            "simulado", "evt_a", "pagamento.confirmado", cobranca.Id, pagoEm, "{}"), CancellationToken.None);

        var dataOriginal = (await db.Mensalidades.SingleAsync()).DataPagamento;

        // Segundo evento (id diferente) para cobrança já paga: ACKed mas não aplicado.
        var segundo = await handler.Handle(new ProcessarWebhookPagamentoCommand(
            "simulado", "evt_b", "pagamento.confirmado", cobranca.Id, DateTime.UtcNow, "{}"), CancellationToken.None);

        segundo.Duplicado.Should().BeFalse();
        segundo.Processado.Should().BeFalse();
        (await db.EventosPagamentoWebhook.OrderBy(e => e.CreatedAt).LastAsync())
            .ErroProcessamento.Should().NotBeNull();
        (await db.Mensalidades.SingleAsync()).DataPagamento.Should().Be(dataOriginal);
    }

    [Fact]
    public async Task Webhook_CobrancaDesconhecidaLancaNotFound()
    {
        using var db = CriarDb();
        var handler = new ProcessarWebhookPagamentoCommandHandler(db, Accessor());

        var ato = async () => await handler.Handle(new ProcessarWebhookPagamentoCommand(
            "simulado", "evt_x", "pagamento.confirmado", Guid.NewGuid(), null, "{}"), CancellationToken.None);

        await ato.Should().ThrowAsync<NotFoundException>();
    }
}