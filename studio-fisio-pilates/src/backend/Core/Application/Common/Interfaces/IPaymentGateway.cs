using Clinica.Domain.Enums;

namespace Clinica.Application.Common.Interfaces;

/// <summary>Resultado da emissão de uma cobrança no provedor.</summary>
public sealed record CobrancaGatewayResult(
    string ProvedorCobrancaId,
    string? PixCopiaECola,
    string? BoletoLinhaDigitavel,
    DateTime ExpiraEmUtc);

/// <summary>
/// Provedor de pagamento (PSP). A implementação concreta vive em
/// Infrastructure; a troca de provedor não afeta domínio nem aplicação.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Nome do provedor (persistido em Cobranca.Provedor).</summary>
    string Nome { get; }

    Task<CobrancaGatewayResult> CriarCobrancaAsync(
        Guid mensalidadeId,
        decimal valor,
        TipoCobranca tipo,
        CancellationToken ct);
}