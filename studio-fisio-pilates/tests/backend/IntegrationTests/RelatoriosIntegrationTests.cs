using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clinica.IntegrationTests;

/// <summary>
/// Dashboard: agregações de resumo, faturamento, ocupação e top sessões
/// expostas em /api/relatorios para o usuário autenticado da clínica.
/// </summary>
[Collection("api")]
public sealed class RelatoriosIntegrationTests : IClassFixture<ResetDb>
{
    private readonly ResetDb _reset;
    private readonly ApiFixture _fixture;

    public RelatoriosIntegrationTests(ResetDb reset, ApiFixture fixture)
    {
        _reset = reset;
        _fixture = fixture;
    }

    [Fact]
    public async Task Dashboard_retorna_agregados_consistentes()
    {
        var seed = await _fixture.SeedClinicaAsync();
        await SemearDadosAsync(seed.ClinicaId, seed.PacienteId, seed.ProfissionalId);
        var client = await AutenticarAsync(seed);

        var resumo = await client.GetFromJsonAsync<JsonElement>("/api/relatorios/resumo");
        resumo.GetProperty("agendamentosHoje").GetInt32().Should().Be(1);
        resumo.GetProperty("receitaMes").GetDecimal().Should().Be(300m);
        resumo.GetProperty("inadimplencia").GetDecimal().Should().Be(250m);

        var faturamento = await client.GetFromJsonAsync<JsonElement>("/api/relatorios/faturamento?meses=3");
        faturamento.GetArrayLength().Should().Be(3);

        var ocupacao = await client.GetFromJsonAsync<JsonElement>("/api/relatorios/ocupacao?dias=7");
        ocupacao.GetArrayLength().Should().Be(7);
        var totalHoje = ocupacao.EnumerateArray()
            .Where(d => DateTime.Parse(d.GetProperty("data").GetString()!) == DateTime.UtcNow.Date)
            .Sum(d => d.GetProperty("total").GetInt32());
        totalHoje.Should().Be(1);
        var totalOntem = ocupacao.EnumerateArray()
            .Where(d => DateTime.Parse(d.GetProperty("data").GetString()!) == DateTime.UtcNow.Date.AddDays(-1))
            .Sum(d => d.GetProperty("total").GetInt32());
        totalOntem.Should().Be(1, "o agendamento cancelado de ontem não conta no total");

        var top = await client.GetFromJsonAsync<JsonElement>("/api/relatorios/top-sessoes");
        top.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        top[0].GetProperty("tipoSessao").GetString().Should().Be("Fisioterapia");
    }

    [Fact]
    public async Task Parametros_fora_da_faixa_usam_limites_seguros()
    {
        var seed = await _fixture.SeedClinicaAsync();
        var client = await AutenticarAsync(seed);

        var faturamento = await client.GetFromJsonAsync<JsonElement>("/api/relatorios/faturamento?meses=999");
        faturamento.GetArrayLength().Should().Be(24, "limite superior de meses é 24");

        var ocupacao = await client.GetFromJsonAsync<JsonElement>("/api/relatorios/ocupacao?dias=-5");
        ocupacao.GetArrayLength().Should().Be(30, "valor inválido cai no padrão de 30 dias");
    }

    private async Task SemearDadosAsync(Guid clinicaId, Guid pacienteId, Guid profissionalId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Clinica.Persistence.TenantDbContext>();
        var hoje = DateTime.UtcNow.Date;

        db.Mensalidades.AddRange(
            new Mensalidade
            {
                ClinicaId = clinicaId,
                PacienteId = pacienteId,
                Competencia = hoje.ToString("yyyy-MM"),
                Valor = 300m,
                DataVencimento = hoje.AddDays(-10),
                Status = StatusMensalidade.Paga,
                DataPagamento = hoje.AddDays(-3),
            },
            new Mensalidade
            {
                ClinicaId = clinicaId,
                PacienteId = pacienteId,
                Competencia = "2020-01",
                Valor = 250m,
                DataVencimento = new DateTime(2020, 1, 10),
                Status = StatusMensalidade.Atrasada,
            });

        db.Agendamentos.Add(new Agendamento
        {
            ClinicaId = clinicaId,
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            DataHoraInicio = hoje.AddHours(9),
            DataHoraFim = hoje.AddHours(10),
            TipoSessao = TipoSessao.PilatesSolo,
            TipoAula = TipoAula.Plano,
            Status = StatusAgendamento.Confirmado,
            ValorSessao = 100m,
        });

        var realizada = new Agendamento
        {
            ClinicaId = clinicaId,
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            DataHoraInicio = hoje.AddDays(-1).AddHours(8),
            DataHoraFim = hoje.AddDays(-1).AddHours(9),
            TipoSessao = TipoSessao.Fisioterapia,
            TipoAula = TipoAula.Individual,
            Status = StatusAgendamento.Realizado,
            ValorSessao = 200m,
        };
        var cancelada = new Agendamento
        {
            ClinicaId = clinicaId,
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            DataHoraInicio = hoje.AddDays(-1).AddHours(14),
            DataHoraFim = hoje.AddDays(-1).AddHours(15),
            TipoSessao = TipoSessao.Domiciliar,
            TipoAula = TipoAula.Individual,
            Status = StatusAgendamento.Cancelado,
            ValorSessao = 500m,
        };
        db.Agendamentos.AddRange(realizada, cancelada);

        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> AutenticarAsync(SeedData seed)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", seed.TenantHeaderValue);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, senha = seed.Senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.GetProperty("accessToken").GetString()!);

        return client;
    }
}
