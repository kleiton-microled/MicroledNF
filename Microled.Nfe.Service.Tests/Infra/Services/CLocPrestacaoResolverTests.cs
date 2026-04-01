using Microled.Nfe.Service.Infra.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class CLocPrestacaoResolverTests
{
    [Fact]
    public void Resolve_WhenExplicitCLocValid_Wins()
    {
        var r = CLocPrestacaoResolver.Resolve(4114203, 3550308);
        Assert.Equal(4114203, r);
    }

    [Fact]
    public void Resolve_WhenNoExplicitCLoc_UsesPrestador()
    {
        var r = CLocPrestacaoResolver.Resolve(null, 3550308);
        Assert.Equal(3550308, r);
    }

    [Fact]
    public void Resolve_WhenInvalid_UsesFallbackSp()
    {
        var r = CLocPrestacaoResolver.Resolve(0, null);
        Assert.Equal(CLocPrestacaoResolver.DefaultSaoPaulo, r);
    }
}
