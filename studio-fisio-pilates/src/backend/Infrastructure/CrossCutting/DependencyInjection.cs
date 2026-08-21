using System.Text;
using Clinica.Application.Common.Interfaces;
using Clinica.CrossCutting.Auth;
using Clinica.CrossCutting.Pagamentos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Clinica.CrossCutting;

public static class DependencyInjection
{
    public static IServiceCollection AddCrossCutting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Provedor de pagamento simulado (dev/testes). PSP real = nova
        // implementação de IPaymentGateway + troca do registro abaixo.
        services.AddScoped<IPaymentGateway, SimulatedPaymentGateway>();
        services.AddSingleton<INotificacaoService, CrossCutting.Notifications.LoggingNotificacaoService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                          ?? new JwtOptions();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.PepAccess, policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(AuthorizationPolicies.PepAccessRoles));

            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(AuthorizationPolicies.AdminOnlyRoles));
        });

        return services;
    }
}