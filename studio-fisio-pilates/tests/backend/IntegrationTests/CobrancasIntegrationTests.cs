using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Fase 3 ponta a ponta: faturamento recorrente → emissão de cobrança Pix →
/// webhook assinado liquida a mensalidade; replay é ACKed sem duplicar;
/// assinatura inválida é rejeitada; inadimplência reflete vencidos.
/// </summary>
[Collection("api")]
public sealed class CobrancasIntegrationTests : IClassFixture<ResetDb>
{
    private const string WebhookSecret = "dev-webhook-secret-troque-em-producao-0a1b2c3d4e5f";

    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public CobrancasIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task FluxoCompleto_Faturamento_Cobranca_Webhook_Liquida()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Cobranças");
        await VincularPlanoAsync(seed, valorMensal: 320m);
        var client = _fixture.CreateClient();
        await AutenticarAsync(client, seed);

        // 1. Faturamento recorrente da competência (idempotente).
        var competencia = "2026-08";
        var faturamento = await client.PostAsJsonAsync(
            "/api/financeiro/faturamento-recorrente", new { competencia });
        faturamento.StatusCode.Should().Be(HttpStatusCode.OK);
        var resumoFaturamento = await faturamento.Content.ReadFromJsonAsync<JsonElement>();
        resumoFaturamento.GetProperty("geradas").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        // Reexecução: nada novo é gerado.
        var reexecucao = await client.PostAsJsonAsync(
            "/api/financeiro/faturamento-recorrente", new { competencia });
        (await reexecucao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("geradas").GetInt32().Should().Be(0);

        // 2. Lista mensalidades e emite cobrança Pix para a primeira.
        var lista = await client.GetAsync($"/api/mensalidades?competencia={competencia}");
        var mensalidadeId = (await lista.Content.ReadFromJsonAsync<JsonElement>())[0]
            .GetProperty("id").GetGuid();

        var emissao = await client.PostAsJsonAsync(
            $"/api/mensalidades/{mensalidadeId}/cobrancas", new { tipo = 1 }); // Pix
        emissao.StatusCode.Should().Be(HttpStatusCode.OK);
        var cobranca = await emissao.Content.ReadFromJsonAsync<JsonElement>();
        cobranca.GetProperty("pixCopiaECola").GetString().Should().NotBeNullOrWhiteSpace();
        var cobrancaId = cobranca.GetProperty("id").GetGuid();

        // 3. Webhook assinado liquida a cobrança.
        var primeiroAck = await EnviarWebhookAsync(client, cobrancaId, "evt_fluxo_1");
        primeiroAck.StatusCode.Should().Be(HttpStatusCode.OK);
        var ack = await primeiroAck.Content.ReadFromJsonAsync<JsonElement>();
        ack.GetProperty("duplicado").GetBoolean().Should().BeFalse();
        ack.GetProperty("processado").GetBoolean().Should().BeTrue();

        // Mensalidade aparece como paga.
        var aposPagamento = await client.GetAsync(
            $"/api/mensalidades?competencia={competencia}&status=2");
        var pagas = await aposPagamento.Content.ReadFromJsonAsync<JsonElement>();
        pagas.GetArrayLength().Should().Be(1);
        pagas[0].GetProperty("id").GetString().Should().Be(mensalidadeId.ToString());

        // 4. Replay do mesmo evento: ACK sem reprocessar.
        var replay = await EnviarWebhookAsync(client, cobrancaId, "evt_fluxo_1");
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replay.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duplicado").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Webhook_ComAssinaturaInvalidaERejeitado()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Webhook Inválido");
        var client = _fixture.CreateClient();

        var corpo = JsonSerializer.Serialize(new
        {
            eventoId = $"evt_{Guid.NewGuid():N}",
            tipo = "pagamento.confirmado",
            cobrancaId = Guid.NewGuid(),
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/api/webhooks/pagamentos/simulado")
        {
            Content = new StringContent(corpo, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Assinatura", "assinatura-falsificada");

        var resposta = await client.SendAsync(request);
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inadimplencia_reflete_mensalidades_vencidas()
    {
        var seed = await _fixture.SeedClinicaAsync("Clínica Inadimplência");
        var client = _fixture.CreateClient();
        await AutenticarAsync(client, seed);

        // Competência passada: vencimento dia 10 já passou e não foi paga.
        var competenciaPassada = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM");
        await client.PostAsJsonAsync("/api/mensalidades", new
        {
            pacienteId = seed.PacienteId,
            competencia = competenciaPassada,
            valor = 280.00m,
        });

        var relatorio = await client.GetAsync("/api/financeiro/inadimplencia");
        relatorio.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await relatorio.Content.ReadFromJsonAsync<JsonElement>();

        corpo.GetProperty("totalVencido").GetDecimal().Should().BeGreaterThanOrEqualTo(280.00m);
        corpo.GetProperty("itens").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        corpo.GetProperty("porFaixa").EnumerateObject().Should().Contain(p =>
            p.Name == "1-30" || p.Name == "31-60" || p.Name == "61-90" || p.Name == "90+");
    }

    private async Task VincularPlanoAsync(SeedData seed, decimal valorMensal)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Clinica.Persistence.TenantDbContext>();

        var plano = new Clinica.Domain.Entities.Plano
        {
            ClinicaId = seed.ClinicaId,
            Nome = "Pilates 2x/semana",
            Valor = valorMensal,
            Ativo = true,
        };
        await db.Planos.AddAsync(plano);

        var paciente = await db.Pacientes.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == seed.PacienteId);
        paciente.PlanoId = plano.Id;

        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> EnviarWebhookAsync(
        HttpClient client, Guid cobrancaId, string eventoId)
    {
        var corpo = JsonSerializer.Serialize(new
        {
            eventoId,
            tipo = "pagamento.confirmado",
            cobrancaId,
            pagoEmUtc = DateTime.UtcNow.ToString("O"),
        });

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var assinatura = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(corpo)));

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/api/webhooks/pagamentos/simulado")
        {
            Content = new StringContent(corpo, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Assinatura", assinatura);

        return await client.SendAsync(request);
    }

    private static async Task AutenticarAsync(HttpClient client, SeedData seed)
    {
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);
    }
}