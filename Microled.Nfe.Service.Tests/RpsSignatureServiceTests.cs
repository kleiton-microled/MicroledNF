using Microsoft.Extensions.Logging.Abstractions;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Xunit;

namespace Microled.Nfe.Service.Tests;

public class RpsSignatureServiceTests
{
    [Fact]
    public void BuildSignatureString_ShouldPlaceValorServicosInCentavosAtExpectedSlice()
    {
        var svc = new RpsSignatureService(NullLogger<RpsSignatureService>.Instance);

        // Arrange: ValorServicos = 6321.00 => 632100 centavos => "000000000632100" (15 chars)
        var rps = CreateRps(
            valorServicos: 6321.00m,
            valorDeducoes: 0m,
            codigoServico: 2919);

        // Act
        var sig = svc.BuildSignatureString(rps);

        // Assert
        Assert.Equal(90, sig.Length);
        var valorServicosSlice = sig.Substring(40, 15);
        Assert.Equal("000000000632100", valorServicosSlice);
    }

    [Fact]
    public void BuildSignatureString_ShouldRoundCentavos_AwayFromZero()
    {
        var svc = new RpsSignatureService(NullLogger<RpsSignatureService>.Instance);

        // 1.005 * 100 = 100.5 => round AwayFromZero => 101 => "000000000000101"
        var rps = CreateRps(
            valorServicos: 1.005m,
            valorDeducoes: 0m,
            codigoServico: 2919);

        var sig = svc.BuildSignatureString(rps);

        Assert.Equal(90, sig.Length);
        Assert.Equal("000000000000101", sig.Substring(40, 15));
    }

    /// <summary>
    /// Mesma regra que MapRpsToTpRPS (ValorFinalCobrado); evita 1206 quando item.valorServicos ≠ tributos.valorFinalCobrado.
    /// </summary>
    [Fact]
    public void BuildSignatureString_UsesValorFinalCobradoFromTributos_WhenProvided()
    {
        var svc = new RpsSignatureService(NullLogger<RpsSignatureService>.Instance);

        var rps = CreateRps(
            valorServicos: 4465.80m,
            valorDeducoes: 0m,
            codigoServico: 2919);
        rps.SetTributos(new RpsTaxInfo(valorFinalCobrado: Money.Create(4191.15m)));

        var sig = svc.BuildSignatureString(rps);

        Assert.Equal("000000000419115", sig.Substring(40, 15));
    }

    /// <summary>
    /// ValorFinalCobrado = 0 no tributo não pode "vencer" Item.ValorServicos (bug do ?? com zero — DANFSE com R$ 0,00).
    /// </summary>
    [Fact]
    public void BuildSignatureString_WhenValorFinalCobradoIsZero_UsesValorServicos()
    {
        var svc = new RpsSignatureService(NullLogger<RpsSignatureService>.Instance);

        var rps = CreateRps(
            valorServicos: 4465.80m,
            valorDeducoes: 0m,
            codigoServico: 2919);
        rps.SetTributos(new RpsTaxInfo(valorFinalCobrado: Money.Create(0m)));

        var sig = svc.BuildSignatureString(rps);

        Assert.Equal("000000000446580", sig.Substring(40, 15));
    }

    private static Rps CreateRps(decimal valorServicos, decimal valorDeducoes, int codigoServico)
    {
        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj("02126914000129"),
            inscricaoMunicipal: 37684280,
            razaoSocial: "Prestador Teste",
            endereco: null,
            email: null);

        var chave = new RpsKey(inscricaoPrestador: 37684280, numeroRps: 3712, serieRps: "A");

        var item = new RpsItem(
            codigoServico: codigoServico,
            discriminacao: "Teste",
            valorServicos: Money.Create(valorServicos),
            valorDeducoes: Money.Create(valorDeducoes),
            aliquotaServicos: Aliquota.Create(0.05m),
            issRetido: IssRetido.Nao,
            tributacaoRPS: TipoTributacao.TributacaoMunicipio);

        return new Rps(
            chaveRPS: chave,
            tipoRPS: TipoRps.RPS,
            dataEmissao: new DateOnly(2025, 12, 01),
            statusRPS: StatusRps.Normal,
            tributacaoRPS: TipoTributacao.TributacaoMunicipio,
            item: item,
            prestador: prestador,
            tomador: null);
    }
}


