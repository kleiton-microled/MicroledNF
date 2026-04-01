using Microled.Nfe.Service.Infra.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class IbsCbsCClassTribValidatorTests
{
    [Fact]
    public void ValidateAndGet_WhenEmpty_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => IbsCbsCClassTribValidator.ValidateAndGet(null));
        Assert.Throws<InvalidOperationException>(() => IbsCbsCClassTribValidator.ValidateAndGet(""));
        Assert.Throws<InvalidOperationException>(() => IbsCbsCClassTribValidator.ValidateAndGet("   "));
    }

    [Fact]
    public void ValidateAndGet_WhenContainsNonDigits_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => IbsCbsCClassTribValidator.ValidateAndGet("12A3"));
        Assert.Throws<InvalidOperationException>(() => IbsCbsCClassTribValidator.ValidateAndGet("12-3"));
    }

    [Fact]
    public void ValidateAndGet_WhenValid_ShouldReturnTrimmedPreservingLeadingZeros()
    {
        Assert.Equal("000001", IbsCbsCClassTribValidator.ValidateAndGet(" 000001 "));
    }

    [Fact]
    public void ValidateAndGet_WhenFewerThanSixDigits_ShouldPadLeftToSix()
    {
        Assert.Equal("000220", IbsCbsCClassTribValidator.ValidateAndGet("220"));
    }

    [Fact]
    public void ValidateAndGet_WhenMoreThanSixDigits_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => IbsCbsCClassTribValidator.ValidateAndGet("1234567"));
    }
}


