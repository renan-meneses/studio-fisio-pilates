using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;

namespace Clinica.CrossCutting.Pagamentos;

/// <summary>
/// Provedor simulado para desenvolvimento e testes: determinístico,
/// sem chamadas externas. Contrato do webhook correspondente documentado
/// em WebhooksController. A troca por PSP real (Asaas, Mercado Pago, etc.)
/// é uma nova implementação desta interface + registro no DI.
/// </summary>
public sealed class SimulatedPaymentGateway : IPaymentGateway
{
    public string Nome => "simulado";

    public Task<CobrancaGatewayResult> CriarCobrancaAsync(
        Guid mensalidadeId,
        decimal valor,
        TipoCobranca tipo,
        CancellationToken ct)
    {
        var provedorId = $"sim_{mensalidadeId:N}_{(int)tipo}";
        var expiraEm = tipo == TipoCobranca.Pix
            ? DateTime.UtcNow.AddHours(24)
            : DateTime.UtcNow.AddDays(3);

        var resultado = tipo switch
        {
            TipoCobranca.Pix => new CobrancaGatewayResult(
                provedorId,
                PixCopiaECola: $"00020126BR.GOV.BCB.PIX01CLINICA{valor:0.00}{provedorId}",
                BoletoLinhaDigitavel: null,
                expiraEm),
            TipoCobranca.Boleto => new CobrancaGatewayResult(
                provedorId,
                PixCopiaECola: null,
                BoletoLinhaDigitavel: $"34191.{Random.Shared.Next(10000, 99999)} {Random.Shared.Next(10000, 99999)}.{Random.Shared.Next(100000, 999999)} {Random.Shared.Next(1, 9)} {valor:0.00}",
                expiraEm),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null),
        };

        return Task.FromResult(resultado);
    }
}