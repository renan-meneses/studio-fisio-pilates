namespace Clinica.CrossCutting.Auth;

/// <summary>Opções do JWT carregadas de appsettings (seção "JWT").</summary>
public sealed class JwtOptions
{
    public const string SectionName = "JWT";

    public string Issuer { get; set; } = "clinica-api";

    public string Audience { get; set; } = "clinica-web";

    public string SecretKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 480;
}