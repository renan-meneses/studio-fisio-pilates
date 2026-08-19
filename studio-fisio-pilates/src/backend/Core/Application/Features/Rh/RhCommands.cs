using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Rh;

public sealed record RegistrarPontoCommand(
    Guid ProfissionalId,
    DateTime Data,
    TimeSpan? Entrada,
    TimeSpan? Saida,
    TimeSpan? AlmocoInicio,
    TimeSpan? AlmocoFim,
    TimeSpan? HorasExtras) : IRequest<Guid>;

public sealed record CalcularFolhaCommand(
    Guid ProfissionalId,
    string Competencia,
    decimal Descontos) : IRequest<Guid>;

public sealed record RegistrarPontoCommandHandler : IRequestHandler<RegistrarPontoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public RegistrarPontoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(RegistrarPontoCommand request, CancellationToken ct)
    {
        var profissionalExiste = await _db.Profissionais.AnyAsync(
            p => p.Id == request.ProfissionalId && p.Ativo, ct);

        if (!profissionalExiste)
            throw new BusinessRuleException("Profissional não encontrado ou inativo.");

        if (request.Entrada.HasValue && request.Saida.HasValue && request.Saida <= request.Entrada)
            throw new BusinessRuleException("A saída deve ser posterior à entrada do ponto.");

        var ponto = new Ponto
        {
            ProfissionalId = request.ProfissionalId,
            Data = request.Data.Date,
            Entrada = request.Entrada,
            Saida = request.Saida,
            AlmocoInicio = request.AlmocoInicio,
            AlmocoFim = request.AlmocoFim,
            HorasExtras = request.HorasExtras,
        };

        await _db.Pontos.AddAsync(ponto, ct);
        await _db.SaveChangesAsync(ct);

        return ponto.Id;
    }
}

public sealed class CalcularFolhaCommandHandler : IRequestHandler<CalcularFolhaCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CalcularFolhaCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CalcularFolhaCommand request, CancellationToken ct)
    {
        var profissional = await _db.Profissionais
            .FirstOrDefaultAsync(p => p.Id == request.ProfissionalId && p.Ativo, ct)
            ?? throw new BusinessRuleException("Profissional não encontrado ou inativo.");

        var folhaExistente = await _db.FolhasSalariais.AnyAsync(
            f => f.ProfissionalId == request.ProfissionalId && f.Competencia == request.Competencia, ct);

        if (folhaExistente)
            throw new BusinessRuleException("Folha já calculada para o profissional na competência.");

        if (request.Descontos < 0 || request.Descontos > profissional.SalarioBase)
            throw new BusinessRuleException("Descontos inválidos para a competência.");

        var inicio = new DateTime(int.Parse(request.Competencia[..4]), int.Parse(request.Competencia[5..7]), 1);
        var fim = inicio.AddMonths(1);

        var pontos = await _db.Pontos
            .AsNoTracking()
            .Where(p => p.ProfissionalId == request.ProfissionalId
                        && p.Data >= inicio && p.Data < fim)
            .ToListAsync(ct);

        var diasTrabalhados = pontos.Count(p => p.Entrada.HasValue && p.Saida.HasValue);
        var faltas = DateTime.DaysInMonth(inicio.Year, inicio.Month)
                     - diasTrabalhados
                     - pontos.Count(p => p.Observacoes != null && p.Observacoes.Contains("férias"));

        var folha = new FolhaSalarial
        {
            ClinicaId = profissional.ClinicaId,
            ProfissionalId = request.ProfissionalId,
            Competencia = request.Competencia,
            ValorBruto = profissional.SalarioBase,
            DiasTrabalhados = diasTrabalhados,
            Faltas = Math.Max(0, faltas),
        };

        folha.Processar(request.Descontos);

        await _db.FolhasSalariais.AddAsync(folha, ct);
        await _db.SaveChangesAsync(ct);

        return folha.Id;
    }
}