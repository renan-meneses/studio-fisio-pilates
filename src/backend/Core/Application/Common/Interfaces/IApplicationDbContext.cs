using Clinica.Domain.Entities;
using ClinicaEntity = Clinica.Domain.Entities.Clinica;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Common.Interfaces;

/// <summary>
/// Abstração do DbContext para a camada de Application (Port/Adapter).
/// A implementação fica em Infrastructure/Persistence (TenantDbContext).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<ClinicaEntity> Clinicas { get; }

    DbSet<Paciente> Pacientes { get; }

    DbSet<Profissional> Profissionais { get; }

    DbSet<Agendamento> Agendamentos { get; }

    DbSet<Presenca> Presencas { get; }

    DbSet<ProntuarioEletronico> Prontuarios { get; }

    DbSet<EvolucaoClinica> Evolucoes { get; }

    DbSet<Mensalidade> Mensalidades { get; }

    DbSet<ContaPagar> ContasPagar { get; }

    DbSet<Ponto> Pontos { get; }

    DbSet<FolhaSalarial> FolhasSalariais { get; }

    DbSet<Usuario> Usuarios { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}