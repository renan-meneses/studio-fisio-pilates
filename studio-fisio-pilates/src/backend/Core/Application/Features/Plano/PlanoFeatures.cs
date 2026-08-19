using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Plano;

public sealed record ServicoResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Valor,
    bool Ativo);

public sealed record PlanoResponse(
    Guid Id,
    string Nome,
    decimal Valor,
    string? Descricao,
    bool Ativo,
    IReadOnlyList<ServicoResponse> Servicos);

public sealed record ListarPlanosQuery : IRequest<IReadOnlyList<PlanoResponse>>;

public sealed record ListarServicosQuery(string? Termo) : IRequest<IReadOnlyList<ServicoResponse>>;

public sealed record CriarPlanoCommand(string Nome, decimal Valor, string? Descricao) : IRequest<Guid>;

public sealed record AtualizarPlanoCommand(
    Guid Id,
    string Nome,
    decimal Valor,
    string? Descricao,
    bool Ativo) : IRequest;

public sealed record CriarServicoCommand(string Nome, string? Descricao, decimal Valor) : IRequest<Guid>;

public sealed record AtualizarServicoCommand(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Valor,
    bool Ativo) : IRequest;

public sealed record AdicionarServicoAoPlanoCommand(Guid PlanoId, Guid ServicoId) : IRequest;

public sealed record RemoverServicoDoPlanoCommand(Guid PlanoId, Guid ServicoId) : IRequest;

public sealed class CriarPlanoCommandValidator : AbstractValidator<CriarPlanoCommand>
{
    public CriarPlanoCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Valor).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Descricao).MaximumLength(500);
    }
}

public sealed class CriarServicoCommandValidator : AbstractValidator<CriarServicoCommand>
{
    public CriarServicoCommandValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().MaximumLength(150);
        RuleFor(r => r.Valor).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Descricao).MaximumLength(500);
    }
}

public sealed class ListarPlanosQueryHandler
    : IRequestHandler<ListarPlanosQuery, IReadOnlyList<PlanoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarPlanosQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlanoResponse>> Handle(ListarPlanosQuery request, CancellationToken ct)
    {
        var planos = await _db.Planos
            .Include(p => p.PlanoServicos).ThenInclude(ps => ps.Servico)
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .ToListAsync(ct);

        return planos
            .Select(p => new PlanoResponse(
                p.Id,
                p.Nome,
                p.Valor,
                p.Descricao,
                p.Ativo,
                p.PlanoServicos
                    .Where(ps => ps.Servico is not null)
                    .Select(ps => new ServicoResponse(
                        ps.Servico!.Id,
                        ps.Servico.Nome,
                        ps.Servico.Descricao,
                        ps.Servico.Valor,
                        ps.Servico.Ativo))
                    .OrderBy(s => s.Nome)
                    .ToList()))
            .ToList();
    }
}

public sealed class ListarServicosQueryHandler
    : IRequestHandler<ListarServicosQuery, IReadOnlyList<ServicoResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListarServicosQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServicoResponse>> Handle(ListarServicosQuery request, CancellationToken ct)
    {
        var query = _db.Servicos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Termo))
            query = query.Where(s => s.Nome.ToLower().Contains(request.Termo.ToLower()));

        return (await query.OrderBy(s => s.Nome).ToListAsync(ct))
            .Select(s => new ServicoResponse(s.Id, s.Nome, s.Descricao, s.Valor, s.Ativo))
            .ToList();
    }
}

public sealed class CriarPlanoCommandHandler : IRequestHandler<CriarPlanoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CriarPlanoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CriarPlanoCommand request, CancellationToken ct)
    {
        var plano = new Domain.Entities.Plano
        {
            Nome = request.Nome,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Ativo = true,
        };

        await _db.Planos.AddAsync(plano, ct);
        await _db.SaveChangesAsync(ct);

        return plano.Id;
    }
}

public sealed class CriarServicoCommandHandler : IRequestHandler<CriarServicoCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CriarServicoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CriarServicoCommand request, CancellationToken ct)
    {
        var servico = new Domain.Entities.Servico
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Valor = request.Valor,
            Ativo = true,
        };

        await _db.Servicos.AddAsync(servico, ct);
        await _db.SaveChangesAsync(ct);

        return servico.Id;
    }
}

public sealed class AtualizarPlanoCommandHandler : IRequestHandler<AtualizarPlanoCommand>
{
    private readonly IApplicationDbContext _db;

    public AtualizarPlanoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AtualizarPlanoCommand request, CancellationToken ct)
    {
        var plano = await _db.Planos
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Plano não encontrado.");

        plano.Nome = request.Nome;
        plano.Valor = request.Valor;
        plano.Descricao = request.Descricao;
        plano.Ativo = request.Ativo;

        await _db.SaveChangesAsync(ct);
    }
}

public sealed class AtualizarServicoCommandHandler : IRequestHandler<AtualizarServicoCommand>
{
    private readonly IApplicationDbContext _db;

    public AtualizarServicoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AtualizarServicoCommand request, CancellationToken ct)
    {
        var servico = await _db.Servicos
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("Serviço não encontrado.");

        servico.Nome = request.Nome;
        servico.Descricao = request.Descricao;
        servico.Valor = request.Valor;
        servico.Ativo = request.Ativo;

        await _db.SaveChangesAsync(ct);
    }
}

public sealed class AdicionarServicoAoPlanoCommandHandler : IRequestHandler<AdicionarServicoAoPlanoCommand>
{
    private readonly IApplicationDbContext _db;

    public AdicionarServicoAoPlanoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AdicionarServicoAoPlanoCommand request, CancellationToken ct)
    {
        var plano = await _db.Planos.AnyAsync(p => p.Id == request.PlanoId, ct);
        if (!plano)
            throw new NotFoundException("Plano não encontrado.");

        var servico = await _db.Servicos.AnyAsync(s => s.Id == request.ServicoId, ct);
        if (!servico)
            throw new NotFoundException("Serviço não encontrado.");

        var jaExiste = await _db.PlanoServicos.AnyAsync(
            ps => ps.PlanoId == request.PlanoId && ps.ServicoId == request.ServicoId,
            ct);

        if (jaExiste)
            throw new BusinessRuleException("Serviço já adicionado ao plano.");

        _db.PlanoServicos.Add(new Domain.Entities.PlanoServico
        {
            PlanoId = request.PlanoId,
            ServicoId = request.ServicoId,
        });

        await _db.SaveChangesAsync(ct);
    }
}

public sealed class RemoverServicoDoPlanoCommandHandler : IRequestHandler<RemoverServicoDoPlanoCommand>
{
    private readonly IApplicationDbContext _db;

    public RemoverServicoDoPlanoCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RemoverServicoDoPlanoCommand request, CancellationToken ct)
    {
        var vinculo = await _db.PlanoServicos
            .FirstOrDefaultAsync(
                ps => ps.PlanoId == request.PlanoId && ps.ServicoId == request.ServicoId,
                ct)
            ?? throw new NotFoundException("Serviço não está vinculado ao plano.");

        _db.PlanoServicos.Remove(vinculo);
        await _db.SaveChangesAsync(ct);
    }
}