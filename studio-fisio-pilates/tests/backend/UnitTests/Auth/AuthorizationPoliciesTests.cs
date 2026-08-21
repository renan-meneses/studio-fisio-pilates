using Clinica.CrossCutting.Auth;
using Clinica.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Clinica.UnitTests.Auth;

/// <summary>
/// Garante que as roles declaradas nas policies existem no domínio
/// (proteção contra drift quando o enum PapelUsuario mudar).
/// </summary>
public class AuthorizationPoliciesTests
{
    [Fact]
    public void PepAccessRoles_sao_papeis_validos_do_dominio()
    {
        var papeisValidos = Enum.GetNames<PapelUsuario>();

        AuthorizationPolicies.PepAccessRoles.Should().NotBeEmpty();
        AuthorizationPolicies.PepAccessRoles.Should().OnlyContain(r => papeisValidos.Contains(r));
    }
}