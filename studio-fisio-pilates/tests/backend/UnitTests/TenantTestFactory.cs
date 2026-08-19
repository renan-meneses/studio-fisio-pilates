using Clinica.Application.Common.Interfaces;
using Moq;
using Xunit;

namespace Clinica.UnitTests;

/// <summary>Fábrica de ICurrentTenantService/ICurrentTenantAccessor para testes.</summary>
public static class TenantTestFactory
{
    public static Mock<ICurrentTenantService> TenantOf(Guid tenantId, string nome = "Clínica Teste")
    {
        var mock = new Mock<ICurrentTenantService>();
        mock.Setup(t => t.TenantId).Returns(tenantId);
        mock.Setup(t => t.TenantName).Returns(nome);
        return mock;
    }
}