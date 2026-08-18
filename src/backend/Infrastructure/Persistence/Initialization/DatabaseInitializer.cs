using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using ClinicaEntity = Clinica.Domain.Entities.Clinica;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Persistence.Initialization;

/// <summary>
/// Cria o schema e popula dados de demonstração no primeiro boot (dev).
/// Em produção, use `dotnet ef database update` (migrations).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(TenantDbContext context, CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);

        if (await context.Clinicas.AnyAsync(ct))
            return;

        var clinicaDemo = new ClinicaEntity
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Nome = "Clínica Demonstração",
            CNPJ = "00000000000100",
            Email = "contato@demo.clinica",
            Plano = PlanoContratacao.Profissional,
        };

        await context.Clinicas.AddAsync(clinicaDemo, ct);

        var paciente = new Paciente
        {
            ClinicaId = clinicaDemo.Id,
            Nome = "Maria da Silva",
            CPF = "12345678901",
            DataNascimento = new DateTime(1990, 5, 14),
            Telefone = "11999999999",
        };

        var profissional = new Profissional
        {
            ClinicaId = clinicaDemo.Id,
            Nome = "Dr. João Pereira",
            CPF = "98765432100",
            RegistroProfissional = "CREFITO 12345-F",
            Especialidades = "Fisioterapia, Pilates Clínico",
            SalarioBase = 4500m,
        };

        await context.Pacientes.AddAsync(paciente, ct);
        await context.Profissionais.AddAsync(profissional, ct);

        await context.SaveChangesAsync(ct);
    }
}