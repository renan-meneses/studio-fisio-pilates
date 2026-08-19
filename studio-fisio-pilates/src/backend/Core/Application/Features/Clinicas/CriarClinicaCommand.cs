using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using ClinicaEntity = Clinica.Domain.Entities.Clinica;
using Clinica.Domain.Enums;
using MediatR;

namespace Clinica.Application.Features.Clinicas;

public sealed record CriarClinicaCommand(
    string Nome,
    string Cnpj,
    string Email,
    string Telefone,
    string NomeAdministrador,
    string EmailAdministrador,
    string SenhaAdministrador) : IRequest<Guid>;

public sealed class CriarClinicaCommandHandler : IRequestHandler<CriarClinicaCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public CriarClinicaCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CriarClinicaCommand request, CancellationToken ct)
    {
        var clinica = new ClinicaEntity
        {
            Nome = request.Nome,
            CNPJ = request.Cnpj,
            Email = request.Email,
            Telefone = request.Telefone,
            Plano = PlanoContratacao.Basico,
        };

        var admin = new Usuario
        {
            ClinicaId = clinica.Id,
            Nome = request.NomeAdministrador,
            Email = request.EmailAdministrador,
            SenhaHash = _passwordHasher.Hash(request.SenhaAdministrador),
            Papel = PapelUsuario.Administrador,
        };

        await _db.Clinicas.AddAsync(clinica, ct);
        await _db.Usuarios.AddAsync(admin, ct);
        await _db.SaveChangesAsync(ct);

        return clinica.Id;
    }
}