using System.Text.Json.Serialization;

namespace Clinica.Persistence.Initialization;

/// <summary>
/// Credenciais para criação idempotente de um usuário administrador no boot
/// (seção <c>AdminBootstrap</c>). Vazio = desabilitado.
///
/// Uso em produção via variáveis de ambiente:
///   AdminBootstrap__Email=admin@clinica.com.br
///   AdminBootstrap__Senha=<senha forte>
///   AdminBootstrap__ClinicaId=<opcional; default = primeira clínica>
/// </summary>
public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";

    public string Nome { get; set; } = "Administrador";

    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    /// <summary>Clínica alvo; quando nula, usa a primeira clínica existente.</summary>
    public Guid? ClinicaId { get; set; }

    [JsonIgnore]
    public bool Configurado =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Senha);
}
