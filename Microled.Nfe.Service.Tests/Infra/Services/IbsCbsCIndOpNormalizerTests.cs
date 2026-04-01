using Microled.Nfe.Service.Infra.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class IbsCbsCIndOpNormalizerTests
{
    [Theory]
    [InlineData("100301", false)]
    [InlineData("020101", true)]
    [InlineData("100201", true)]
    public void ShouldSerializeImovelObra_MatchesPmspRule621(string cIndOp, bool expected)
    {
        Assert.Equal(expected, IbsCbsCIndOpNormalizer.ShouldSerializeImovelObra(cIndOp));
    }
}
