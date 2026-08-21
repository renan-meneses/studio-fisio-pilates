using Clinica.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Agendamento;

/// <summary>
/// Serializa operações de agenda por profissional via
/// <c>pg_advisory_xact_lock</c>: o lock vive até o fim da transação atual,
/// impedindo que duas requisições concorrentes passem juntas pela checagem
/// de sobreposição (TOCTOU). Em outros providers (ex.: SQLite nos testes de
/// unidade), não há lock — as regras lógicas continuam valendo.
/// </summary>
internal static class AgendamentoLock
{
    public static async Task AdquirirAsync(
        IApplicationDbContext db,
        Guid profissionalId,
        CancellationToken ct)
    {
        var ehPostgres =
            db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true;

        if (!ehPostgres)
            return;

        // Guid → long determinístico (colisões apenas serializam a mais).
        var bytes = profissionalId.ToByteArray();
        var chave = BitConverter.ToInt64(bytes, 0);

        // Array explícito: sem ele, o token de cancelamento é interpretado
        // como parâmetro SQL pelo overload params.
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})", [chave], ct);
    }
}
