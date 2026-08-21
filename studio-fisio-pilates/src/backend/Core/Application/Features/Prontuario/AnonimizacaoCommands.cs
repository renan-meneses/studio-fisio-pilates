using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Prontuario;

public sealed record AnonimizarPacienteCommand(Guid PacienteId) : IRequest;

public sealed class AnonimizarPacienteCommandValidator
    : AbstractValidator<AnonimizarPacienteCommand>
{
    public AnonimizarPacienteCommandValidator()
    {
        RuleFor(r => r.PacienteId).NotEmpty();
    }
}

/// <summary>
/// LGPD (art. 16/18): eliminação dos dados pessoais identificáveis do
/// paciente mantendo a integridade referencial do histórico financeiro e
/// clínico (mensalidades, agendamentos e prontuário continuam existindo,
/// dissociados de um titular). Operação idempotente.
/// </summary>
public sealed class AnonimizarPacienteCommandHandler : IRequestHandler<AnonimizarPacienteCommand>
{
    private readonly IApplicationDbContext _db;

    public AnonimizarPacienteCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AnonimizarPacienteCommand request, CancellationToken ct)
    {
        var paciente = await _db.Pacientes
            .FirstOrDefaultAsync(p => p.Id == request.PacienteId, ct)
            ?? throw new NotFoundException("Paciente não encontrado.");

        if (paciente.Status == StatusPaciente.Anonimizado)
            return;

        paciente.Nome = "Titular removido (LGPD)";
        paciente.Sobrenome = null;
        paciente.CPF = null;
        paciente.DataNascimento = DateTime.MinValue;
        paciente.Telefone = null;
        paciente.Email = null;
        paciente.Endereco = null;
        paciente.Indicacao = null;
        paciente.Observacoes = null;
        paciente.Status = StatusPaciente.Anonimizado;

        await _db.SaveChangesAsync(ct);
    }
}
