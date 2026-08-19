using Clinica.Application.Common.Exceptions;
using Clinica.Application.Common.Interfaces;
using Clinica.Domain.Entities;
using Clinica.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Application.Features.Financeiro;

public sealed record GerarMensalidadeCommand(Guid PacienteId, string Competencia, decimal Valor) : IRequest<Guid>;

public sealed record RegistrarPagamentoMensalidadeCommand(Guid MensalidadeId) : IRequest;

public sealed record CadastrarContaPagarCommand(
    string Fornecedor,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    TipoCusto TipoCusto) : IRequest<Guid>;

public sealed record BaixarContaPagarCommand(Guid ContaId) : IRequest;

public sealed class GerarMensalidadeCommandHandler : IRequestHandler<GerarMensalidadeCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public GerarMensalidadeCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(GerarMensalidadeCommand request, CancellationToken ct)
    {
        if (request.Valor <= 0)
            throw new BusinessRuleException("O valor da mensalidade deve ser positivo.");

        var jaExiste = await _db.Mensalidades.AnyAsync(
            m => m.PacienteId == request.PacienteId && m.Competencia == request.Competencia, ct);

        if (jaExiste)
            throw new BusinessRuleException("Mensalidade já gerada para a competência.");

        var mensalidade = new Mensalidade
        {
            PacienteId = request.PacienteId,
            Competencia = request.Competencia,
            Valor = request.Valor,
            DataVencimento = new DateTime(
                int.Parse(request.Competencia[..4]),
                int.Parse(request.Competencia[5..7]),
                10),
            Status = StatusMensalidade.Pendente,
        };

        await _db.Mensalidades.AddAsync(mensalidade, ct);
        await _db.SaveChangesAsync(ct);

        return mensalidade.Id;
    }
}

public sealed class RegistrarPagamentoMensalidadeCommandHandler
    : IRequestHandler<RegistrarPagamentoMensalidadeCommand>
{
    private readonly IApplicationDbContext _db;

    public RegistrarPagamentoMensalidadeCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RegistrarPagamentoMensalidadeCommand request, CancellationToken ct)
    {
        var mensalidade = await _db.Mensalidades
            .FirstOrDefaultAsync(m => m.Id == request.MensalidadeId, ct)
            ?? throw new NotFoundException("Mensalidade não encontrada.");

        if (mensalidade.Status == StatusMensalidade.Paga)
            throw new BusinessRuleException("Mensalidade já paga.");

        mensalidade.RegistrarPagamento(DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class CadastrarContaPagarCommandHandler : IRequestHandler<CadastrarContaPagarCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CadastrarContaPagarCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CadastrarContaPagarCommand request, CancellationToken ct)
    {
        var conta = new ContaPagar
        {
            Fornecedor = request.Fornecedor,
            Descricao = request.Descricao,
            Valor = request.Valor,
            DataVencimento = request.DataVencimento,
            TipoCusto = request.TipoCusto,
        };

        await _db.ContasPagar.AddAsync(conta, ct);
        await _db.SaveChangesAsync(ct);

        return conta.Id;
    }
}

public sealed class BaixarContaPagarCommandHandler : IRequestHandler<BaixarContaPagarCommand>
{
    private readonly IApplicationDbContext _db;

    public BaixarContaPagarCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Handle(BaixarContaPagarCommand request, CancellationToken ct)
    {
        var conta = await _db.ContasPagar
            .FirstOrDefaultAsync(c => c.Id == request.ContaId, ct)
            ?? throw new NotFoundException("Conta a pagar não encontrada.");

        if (conta.Status == StatusContaPagar.Paga)
            throw new BusinessRuleException("Conta já está paga.");

        conta.Status = StatusContaPagar.Paga;
        conta.DataPagamento = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}