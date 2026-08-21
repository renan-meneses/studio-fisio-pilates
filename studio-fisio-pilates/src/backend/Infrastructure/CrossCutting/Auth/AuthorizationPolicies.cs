namespace Clinica.CrossCutting.Auth;

/// <summary>
/// Políticas de autorização da aplicação (RBAC por role claim do JWT).
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Acesso a prontuário eletrônico e evoluções clínicas (PEP).</summary>
    public const string PepAccess = "PepAccess";

    /// <summary>Gestão de usuários e acessos da clínica — apenas Administrador.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Papéis autorizados à PEP. Decisão de negócio: o financeiro é tratado
    /// como papel estritamente administrativo e não acessa dados clínicos.
    /// Valores devem permanecer alinhados ao enum <c>PapelUsuario</c> (o
    /// teste AuthorizationPoliciesTests garante isso contra drift).
    /// </summary>
    public static readonly string[] PepAccessRoles =
        ["Administrador", "Atendente", "Profissional"];

    /// <summary>Papéis autorizados à gestão de usuários.</summary>
    public static readonly string[] AdminOnlyRoles = ["Administrador"];
}