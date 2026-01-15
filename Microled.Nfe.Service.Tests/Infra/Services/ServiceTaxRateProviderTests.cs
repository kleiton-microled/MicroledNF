using Microled.Nfe.Service.Infra.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class ServiceTaxRateProviderTests
{
    [Fact]
    public void GetAliquota_WhenCodigoServico2919_ShouldReturn0029()
    {
        var p = new ServiceTaxRateProvider();
        Assert.Equal(0.029m, p.GetAliquota(2919));
    }

    [Fact]
    public void GetAliquota_WhenUnknown_ShouldReturn0()
    {
        var p = new ServiceTaxRateProvider();
        Assert.Equal(0m, p.GetAliquota(9999));
    }
}


