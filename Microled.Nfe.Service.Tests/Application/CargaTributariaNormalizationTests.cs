using Microled.Nfe.Service.Application.NfseSpTax;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class CargaTributariaNormalizationTests
{
    [Theory]
    [InlineData(0.1645, 0.1645)]
    [InlineData(16.45, 0.1645)]
    [InlineData(1645, 0.1645)]
    public void NormalizePercentualToFraction_MapsLegacyAndStandard(decimal input, decimal expectedFraction)
    {
        var r = CargaTributariaNormalization.NormalizePercentualToFraction(input);
        Assert.Equal(expectedFraction, r);
    }

    [Fact]
    public void RepairFromFonteComposta_Legacy1645PercentSlashIbpt_FixesFraction()
    {
        var (valor, fracao) = CargaTributariaNormalization.RepairFromFonteComposta(
            3757.92m,
            1645m,
            "1645,00% / IBPT");

        Assert.Equal(3757.92m, valor);
        Assert.Equal(0.1645m, fracao);
    }

    [Fact]
    public void CanonicalFonteCargaTributariaXml_Composite_ReturnsIbpt()
    {
        Assert.Equal("IBPT", CargaTributariaNormalization.CanonicalFonteCargaTributariaXml("1645,00% / IBPT"));
        Assert.Equal("IBPT", CargaTributariaNormalization.CanonicalFonteCargaTributariaXml("IBPT"));
    }

    [Fact]
    public void FormatIbptDiscriminacaoLine_MatchesExpectedShape()
    {
        var line = CargaTributariaNormalization.FormatIbptDiscriminacaoLine(3757.92m, 0.1645m, "IBPT");
        Assert.Equal("R$ 3.757,92 (16,45%) / IBPT", line);
    }
}
