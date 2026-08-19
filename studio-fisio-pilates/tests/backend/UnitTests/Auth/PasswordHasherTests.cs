using Clinica.CrossCutting.Auth;
using FluentAssertions;
using Xunit;

namespace Clinica.UnitTests.Auth;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_E_Verify_RetornamTrueParaSenhaCorreta()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("SenhaForte@123");

        hasher.Verify("SenhaForte@123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_RetornaFalseParaSenhaDiferente()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("SenhaForte@123");

        hasher.Verify("SenhaErrada@456", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_ProduzValoresUnicosParaMesmaSenha()
    {
        var hasher = new PasswordHasher();

        hasher.Hash("MesmaSenha").Should().NotBe(hasher.Hash("MesmaSenha"));
    }

    [Fact]
    public void Verify_RetornaFalseParaFormatoInvalido()
    {
        var hasher = new PasswordHasher();

        hasher.Verify("qualquer", "formato-invalido").Should().BeFalse();
    }
}