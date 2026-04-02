using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Enums;
using Microled.Nfe.Service.Application.NfseSpTax;
using Microled.Nfe.Service.Application.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class NfseSpTaxCalculationServiceTests
{
    private readonly NfseSpTaxCalculationService _sut = new();

    /// <summary>
    /// Cenário de referência (valores já validados) — Lucro Presumido com alíquotas padrão.
    /// </summary>
    [Fact]
    public void Calculate_ExampleFromSpec_MatchesExpected()
    {
        var req = new NfseSpTaxCalculationRequest
        {
            ValorServico = 1000.00m,
            ValorDeducoes = 100.00m,
            DescontoIncondicional = 50.00m,
            DescontoCondicional = 0.00m,
            AliquotaIss = 0.05m,
            IssRetido = true,
            ReterPis = true,
            AliquotaPis = 0.0065m,
            ReterCofins = true,
            AliquotaCofins = 0.03m,
            ReterCsll = true,
            AliquotaCsll = 0.01m,
            ReterIr = true,
            AliquotaIr = 0.015m,
            ReterInss = false,
            AliquotaInss = 0.00m,
            ConsiderarDescontoCondicionalNoLiquido = false,
            ArredondarNaCasaFiscal = true,
            CodigoServico = 101,
            RegimeTributario = RegimeTributarioNfseSp.LucroPresumido,
            BaseFederalSobreValorLiquido = true
        };

        var r = _sut.Calculate(req);

        Assert.Equal(850.00m, r.BaseCalculoIss);
        Assert.Equal(850.00m, r.BaseCalculoFederal);
        Assert.Equal(42.50m, r.ValorIss);
        Assert.Equal(5.53m, r.ValorPis);
        Assert.Equal(25.50m, r.ValorCofins);
        Assert.Equal(8.50m, r.ValorCsll);
        Assert.Equal(12.75m, r.ValorIr);
        Assert.Equal(0m, r.ValorInss);
        Assert.Equal(52.28m, r.TotalRetencoesFederais);
        Assert.Equal(94.78m, r.TotalRetencoes);
        Assert.Equal(855.22m, r.ValorLiquido);
    }

    [Fact]
    public void Calculate_LucroPresumido_WithWrongFederalRatesInRequest_IgnoresAndUsesDefaults()
    {
        var req = BaseLucroPresumidoRequest();
        req.ArredondarNaCasaFiscal = true;
        req.ReterPis = false;
        req.AliquotaPis = 0.99m;
        req.AliquotaCofins = 0.99m;
        req.AliquotaCsll = 0.99m;
        req.AliquotaIr = 0.99m;

        var r = _sut.Calculate(req);

        Assert.Equal(5.53m, r.ValorPis);
        Assert.Equal(25.50m, r.ValorCofins);
        Assert.Equal(52.28m, r.TotalRetencoesFederais);
        Assert.Contains(
            "foram ignorados",
            string.Join(' ', r.MemoriaCalculo),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_LucroPresumido_WithoutFederalRatesInRequest_UsesDefaults()
    {
        var req = BaseLucroPresumidoRequest();
        req.ArredondarNaCasaFiscal = true;
        req.ReterPis = req.ReterCofins = req.ReterCsll = req.ReterIr = false;
        req.AliquotaPis = req.AliquotaCofins = req.AliquotaCsll = req.AliquotaIr = 0m;

        var r = _sut.Calculate(req);

        Assert.Equal(5.53m, r.ValorPis);
        Assert.Equal(855.22m, r.ValorLiquido);
        Assert.Equal(94.78m, r.TotalRetencoes);
    }

    [Fact]
    public void Calculate_SimplesNacional_NoFederalRetention_AppliesIssOnlyWhenRetido()
    {
        var req = BaseLucroPresumidoRequest();
        req.RegimeTributario = RegimeTributarioNfseSp.SimplesNacional;
        req.ReterPis = true;
        req.AliquotaPis = 0.0065m;
        req.IssRetido = true;

        var r = _sut.Calculate(req);

        Assert.Equal(0m, r.ValorPis);
        Assert.Equal(0m, r.TotalRetencoesFederais);
        Assert.Equal(42.50m, r.ValorIss);
        Assert.Equal(42.50m, r.TotalRetencoes);
    }

    [Fact]
    public void Calculate_LucroReal_UsesRequestFederalRates()
    {
        var req = BaseLucroPresumidoRequest();
        req.RegimeTributario = RegimeTributarioNfseSp.LucroReal;
        req.ReterPis = true;
        req.AliquotaPis = 0.01m;
        req.ReterCofins = req.ReterCsll = req.ReterIr = false;
        req.AliquotaCofins = req.AliquotaCsll = req.AliquotaIr = 0m;

        var r = _sut.Calculate(req);

        Assert.Equal(8.50m, r.ValorPis);
        Assert.Equal(0m, r.ValorCofins);
        Assert.Contains("Lucro Real", string.Join(' ', r.MemoriaCalculo), StringComparison.Ordinal);
    }

    [Fact]
    public void Calculate_IssNotRetido_DoesNotAddIssToTotalRetencoes()
    {
        var req = BaseLucroPresumidoRequest();
        req.IssRetido = false;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(42.50m, r.ValorIss);
        Assert.Equal(52.28m, r.TotalRetencoesFederais);
        Assert.Equal(52.28m, r.TotalRetencoes);
    }

    [Fact]
    public void Calculate_LucroReal_AllFederalRetentionsOff_OnlyIssMayApply()
    {
        var req = BaseLucroPresumidoRequest();
        req.RegimeTributario = RegimeTributarioNfseSp.LucroReal;
        req.ReterPis = req.ReterCofins = req.ReterCsll = req.ReterIr = req.ReterInss = false;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(0m, r.ValorPis);
        Assert.Equal(0m, r.TotalRetencoesFederais);
        Assert.Equal(42.50m, r.TotalRetencoes);
    }

    [Fact]
    public void Calculate_SimplesNacional_NoRetentions_IssNotRetido_LiquidoIsServicoMinusDesconto()
    {
        var req = BaseLucroPresumidoRequest();
        req.RegimeTributario = RegimeTributarioNfseSp.SimplesNacional;
        req.IssRetido = false;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(0m, r.TotalRetencoes);
        Assert.Equal(950.00m, r.ValorLiquido);
    }

    [Fact]
    public void Calculate_WithDeducoes_ReducesBases()
    {
        var req = BaseLucroPresumidoRequest();
        req.ValorDeducoes = 200m;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(750.00m, r.BaseCalculoIss);
        Assert.Equal(750.00m, r.BaseCalculoFederal);
    }

    [Fact]
    public void Calculate_DescontoCondicionalNoLiquido_SubtractsFromLiquido()
    {
        var req = BaseLucroPresumidoRequest();
        req.DescontoCondicional = 10m;
        req.ConsiderarDescontoCondicionalNoLiquido = true;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(845.22m, r.ValorLiquido);
    }

    [Fact]
    public void Calculate_ArredondarFalse_KeepsMorePrecision()
    {
        var req = BaseLucroPresumidoRequest();
        req.ArredondarNaCasaFiscal = false;

        var r = _sut.Calculate(req);

        Assert.Equal(5.525m, r.ValorPis);
        Assert.Contains("desligado", string.Join(' ', r.MemoriaCalculo), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_BaseZerada_WhenServicoFullyOffset()
    {
        var req = BaseLucroPresumidoRequest();
        req.ValorServico = 150m;
        req.ValorDeducoes = 100m;
        req.DescontoIncondicional = 50m;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(0m, r.BaseCalculoIss);
        Assert.Equal(0m, r.ValorIss);
    }

    [Fact]
    public void Calculate_LucroReal_LiquidoNegativo_DocumentedInMemoria()
    {
        var req = BaseLucroPresumidoRequest();
        req.RegimeTributario = RegimeTributarioNfseSp.LucroReal;
        req.ValorServico = 100m;
        req.ValorDeducoes = 0m;
        req.DescontoIncondicional = 0m;
        req.AliquotaIss = 0.5m;
        req.IssRetido = true;
        req.ReterPis = req.ReterCofins = req.ReterCsll = req.ReterIr = true;
        req.AliquotaPis = req.AliquotaCofins = req.AliquotaCsll = req.AliquotaIr = 0.2m;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.True(r.ValorLiquido < 0);
        Assert.Contains("negativo", string.Join(' ', r.MemoriaCalculo), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_BaseFederalDefault_WithoutDeduçõesInBase()
    {
        var req = BaseLucroPresumidoRequest();
        req.BaseFederalSobreValorLiquido = false;
        req.ArredondarNaCasaFiscal = true;

        var r = _sut.Calculate(req);

        Assert.Equal(950.00m, r.BaseCalculoFederal);
    }

    [Fact]
    public void FederalRuleProvider_LucroPresumido_ReturnsExpectedConstants()
    {
        var p = new NfseSpFederalTaxRuleProvider();
        var r = p.GetFederalTaxesByRegime(RegimeTributarioNfseSp.LucroPresumido);
        Assert.True(r.UsesPreset);
        Assert.True(r.Preset.ReterPis && r.Preset.ReterCofins && r.Preset.ReterCsll && r.Preset.ReterIr);
        Assert.Equal(NfseSpFederalTaxRuleProvider.AliquotaPisLucroPresumido, r.Preset.AliquotaPis);
        Assert.Equal(NfseSpFederalTaxRuleProvider.AliquotaCofinsLucroPresumido, r.Preset.AliquotaCofins);
        Assert.Equal(NfseSpFederalTaxRuleProvider.AliquotaCsllLucroPresumido, r.Preset.AliquotaCsll);
        Assert.Equal(NfseSpFederalTaxRuleProvider.AliquotaIrLucroPresumido, r.Preset.AliquotaIr);
    }

    private static NfseSpTaxCalculationRequest BaseLucroPresumidoRequest() => new()
    {
        ValorServico = 1000.00m,
        ValorDeducoes = 100.00m,
        DescontoIncondicional = 50.00m,
        DescontoCondicional = 0.00m,
        AliquotaIss = 0.05m,
        IssRetido = true,
        ReterPis = true,
        AliquotaPis = 0.0065m,
        ReterCofins = true,
        AliquotaCofins = 0.03m,
        ReterCsll = true,
        AliquotaCsll = 0.01m,
        ReterIr = true,
        AliquotaIr = 0.015m,
        ReterInss = false,
        AliquotaInss = 0m,
        ConsiderarDescontoCondicionalNoLiquido = false,
        ArredondarNaCasaFiscal = false,
        CodigoServico = 101,
        RegimeTributario = RegimeTributarioNfseSp.LucroPresumido,
        BaseFederalSobreValorLiquido = true
    };
}
