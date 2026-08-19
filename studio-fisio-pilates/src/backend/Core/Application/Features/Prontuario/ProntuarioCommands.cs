using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProntuarioEntity = Clinica.Domain.Entities.ProntuarioEletronico;

namespace Clinica.Application.Features.Prontuario;

public sealed record AbrirProntuarioCommand(Guid PacienteId) : IRequest<Guid>;

public sealed record CriarPacienteCommand(
    string Nome,
    string? Cpf,
    string? Telefone,
    string? Email) : IRequest<Guid>;

public sealed class CriarPacienteCommandValidator : FluentValidation.AbstractValidator<CriarPacienteCommand>
{
    public CriarPacienteCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Cpf).MaximumLength(11);
        RuleFor(r => r.Email).EmailAddress().When(r => !string.IsNullOrWhiteSpace(r.Email));
        RuleFor(r => r.Telefone).MaximumLength(20);
    }
}

public sealed record AdicionarEvolucaoCommand(
    Guid ProntuarioId,
    Guid ProfissionalId,
    TipoEvolucao Tipo,
    string? QueixaPrincipal,
    string? Avaliacao,
    string? Conduta,
    string? Observacoes) : IRequest<Guid>;

public sealed class AdicionarEvolucaoCommandValidator : FluentValidation.AbstractValidator<AdicionarEvolucaoCommand>
{
    public AdicionarEvolucaoCommandValidator()
    {
        RuleFor(r => r.ProntuarioId).NotEmpty();
        RuleFor(r => r.ProfissionalId).NotEmpty();
        RuleFor(r => r.Tipo).IsInEnum();
        RuleFor(r => r.Conduta).NotEmpty().WithMessage("A conduta é obrigatória na evolução.");
        RuleFor(r => r.QueixaPrincipal).MaximumLength(500);
        RuleFor(r => r.Avaliacao).MaximumLength(2000);
        RuleFor(r => r.Conduta).MaximumLength(2000);
    }
}

public sealed class AbrirProntuarioCommandHandler : IRequestHandler<AbrirProntuarioCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AbrirProntuarioCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(AbrirProntuarioCommand request, CancellationToken ct)
    {
        var existe = await _db.Prontuarios.AnyAsync(p => p.PacienteId == request.PacienteId && p.Ativo, ct);

        if (existe)
            throw new BusinessRuleException("Paciente já possui prontuário ativo.");

        var prontuario = new ProntuarioEntity
        {
            PacienteId = request.PacienteId,
            DataAbertura = DateTime.UtcNow,
        };

        await _db.Prontuarios.AddAsync(prontuario, ct);
        await _db.SaveChangesAsync(ct);

        return prontuario.Id;
    }
}

public sealed class CriarPacienteCommandHandler : IRequestHandler<CriarPacienteCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CriarPacienteCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CriarPacienteCommand request, CancellationToken ct)
    {
        var paciente = new Paciente
        {
            Nome = request.Nome,
            CPF = string.IsNullOrWhiteSpace(request.Cpf) ? null : request.Cpf,
            Telefone = request.Telefone,
            Email = request.Email,
            Status = StatusPaciente.Ativo,
        };

        await _db.Pacientes.AddAsync(paciente, ct);
        await _db.SaveChangesAsync(ct);

        return paciente.Id;
    }
}

public sealed class AdicionarEvolucaoCommandHandler : IRequestHandler<AdicionarEvolucaoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AdicionarEvolucaoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(AdicionarEvolucaoCommand request, CancellationToken ct)
    {
        var prontuario = await _db.Prontuarios
            .FirstOrDefaultAsync(p => p.Id == request.ProntuarioId && p.Ativo, ct)
            ?? throw new NotFoundException("Prontuário não encontrado ou inativo.");

        var evolucao = new EvolucaoClinica
        {
            ProntuarioId = prontuario.Id,
            ProfissionalId = request.ProfissionalId,
            Tipo = request.Tipo,
            QueixaPrincipal = request.QueixaPrincipal,
            Avaliacao = request.Avaliacao,
            Conduta = request.Conduta,
            Observacoes = request.Observacoes,
        };

        // Fix-up não rastreia filhos fora do filtro de tenant — Add explícito.
        await _db.Evolucoes.AddAsync(evolucao, ct);
        await _db.SaveChangesAsync(ct);

        return evolucao.Id;
    }
}