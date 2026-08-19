using Clinica.Domain.Common;
using Clinica.Domain.Enums;

namespace Clinica.Domain.Entities;

/// <summary>Usuário de acesso ao sistema, escopado pela clínica (tenant).</summary>
public class Usuario : BaseEntity, ITenantEntity
{
    public Guid ClinicaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 gerado por CrossCutting.Auth.PasswordHasher.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public PapelUsuario Papel { get; set; } = PapelUsuario.Atendente;

    public bool Ativo { get; set; } = true;

    public DateTime? UltimoLogin { get; set; }

    public TemaPreferencia Tema { get; set; } = TemaPreferencia.Claro;
}

public enum PapelUsuario
{
    Administrador = 1,
    Atendente = 2,
    Financeiro = 3,
    Profissional = 4,
}

public enum TemaPreferencia
{
    Claro = 1,
    Escuro = 2,
}