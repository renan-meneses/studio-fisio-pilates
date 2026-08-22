using Clinica.Domain.Common;

namespace Clinica.Domain.Entities;

/// <summary>
/// Token de uso único para redefinição de senha. O valor bruto nunca é
/// persistido — apenas o SHA-256 dele; a expiração é curta (1 hora).
/// </summary>
public class TokenRedefinicaoSenha : BaseEntity, ITenantEntity, IAggregateRoot
{
    public Guid ClinicaId { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    /// <summary>Momento do consumo; nulo enquanto o token é válido.</summary>
    public DateTime? UsadoEm { get; set; }
}
