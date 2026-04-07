using FluentAssertions;
using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Tests.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class RpsBatchPreparationServiceTests
{
    /// <summary>
    /// Data local fixa para vencimento = data + 19 dias corridos (ex.: 23/03/2026 + 19 = 11/04/2026).
    /// </summary>
    private sealed class FixedLocalDateTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _instant;

        public FixedLocalDateTimeProvider(DateOnly localDate)
        {
            _instant = new DateTimeOffset(localDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _instant;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Fact]
    public void PrepareSignedBatch_ShouldAppendTaxSummaryAndDueDateToDiscriminacao()
    {
        var signatureMock = new Mock<IRpsSignatureService>();
        signatureMock
            .Setup(x => x.SignRps(It.IsAny<Microled.Nfe.Service.Domain.Entities.Rps>(), It.IsAny<X509Certificate2>()))
            .Returns("ASSINATURA_TESTE");

        var certProviderMock = new Mock<ICertificateProvider>();
        certProviderMock.Setup(x => x.GetCertificate()).Returns(TestCertificateHelper.CreateTestCertificateWithPrivateKey());

        var hojeFixo = new DateOnly(2026, 3, 23);
        var sut = new RpsBatchPreparationService(
            signatureMock.Object,
            certProviderMock.Object,
            Options.Create(new IbptCargaTributariaOptions { PreencherQuandoAusente = false }),
            new FixedLocalDateTimeProvider(hojeFixo));
        var request = BuildRequest(
            dataEmissao: new DateOnly(2026, 4, 1),
            discriminacao: "Servico mensal",
            valorServico: 24000.00m,
            valorPis: 26.00m,
            valorCofins: 120.00m,
            valorCsll: 40.00m,
            valorIr: 342.66m,
            valorInss: 0.00m,
            valorTotalRecebido: 21439.61m);

        var batch = sut.PrepareSignedBatch(request);
        var texto = batch.RpsList.Single().Item.Discriminacao;

        texto.Should().Contain("Servico mensal");
        texto.Should().Contain("IRRF: R$342,66");
        texto.Should().Contain("PIS/COFINS/CSLL: R$186,00");
        texto.Should().Contain("Valor liquido: R$21.439,61");
        texto.Should().Contain("Vencimento: 11/04/2026");
    }

    [Fact]
    public void PrepareSignedBatch_ShouldCalculateLiquidoWhenValorTotalRecebidoNotProvided()
    {
        var signatureMock = new Mock<IRpsSignatureService>();
        signatureMock
            .Setup(x => x.SignRps(It.IsAny<Microled.Nfe.Service.Domain.Entities.Rps>(), It.IsAny<X509Certificate2>()))
            .Returns("ASSINATURA_TESTE");

        var certProviderMock = new Mock<ICertificateProvider>();
        certProviderMock.Setup(x => x.GetCertificate()).Returns(TestCertificateHelper.CreateTestCertificateWithPrivateKey());

        var sut = new RpsBatchPreparationService(
            signatureMock.Object,
            certProviderMock.Object,
            Options.Create(new IbptCargaTributariaOptions { PreencherQuandoAusente = false }),
            new FixedLocalDateTimeProvider(new DateOnly(2026, 4, 1)));
        var request = BuildRequest(
            dataEmissao: new DateOnly(2026, 4, 1),
            discriminacao: "Servico mensal",
            valorServico: 4000.00m,
            valorPis: 26.00m,
            valorCofins: 120.00m,
            valorCsll: 40.00m,
            valorIr: 60.00m,
            valorInss: 0.00m,
            valorTotalRecebido: null);

        var batch = sut.PrepareSignedBatch(request);
        var texto = batch.RpsList.Single().Item.Discriminacao;

        texto.Should().Contain("IRRF: R$60,00");
        texto.Should().Contain("PIS/COFINS/CSLL: R$186,00");
        texto.Should().Contain("Valor liquido: R$3.754,00");
        texto.Should().Contain("Vencimento: 20/04/2026");
    }

    [Fact]
    public void PrepareSignedBatch_ShouldCalculateLiquidoWhenValorTotalRecebidoIsZeroAndBrutoIsPositive()
    {
        var signatureMock = new Mock<IRpsSignatureService>();
        signatureMock
            .Setup(x => x.SignRps(It.IsAny<Microled.Nfe.Service.Domain.Entities.Rps>(), It.IsAny<X509Certificate2>()))
            .Returns("ASSINATURA_TESTE");

        var certProviderMock = new Mock<ICertificateProvider>();
        certProviderMock.Setup(x => x.GetCertificate()).Returns(TestCertificateHelper.CreateTestCertificateWithPrivateKey());

        var sut = new RpsBatchPreparationService(
            signatureMock.Object,
            certProviderMock.Object,
            Options.Create(new IbptCargaTributariaOptions { PreencherQuandoAusente = false }),
            new FixedLocalDateTimeProvider(new DateOnly(2026, 4, 6)));
        var request = BuildRequest(
            dataEmissao: new DateOnly(2026, 4, 6),
            discriminacao: "teste",
            valorServico: 4000.00m,
            valorPis: 47.66m,
            valorCofins: 120.00m,
            valorCsll: 40.00m,
            valorIr: 60.00m,
            valorInss: 0.00m,
            valorTotalRecebido: 0.00m);

        var batch = sut.PrepareSignedBatch(request);
        var texto = batch.RpsList.Single().Item.Discriminacao;

        texto.Should().Contain("IRRF: R$60,00");
        texto.Should().Contain("PIS/COFINS/CSLL: R$207,66");
        texto.Should().Contain("Valor liquido: R$3.732,34");
        texto.Should().Contain("Vencimento: 25/04/2026");
    }

    [Fact]
    public void PrepareSignedBatch_ShouldComputeIbptCargaWhenClientOmitsCargaFields()
    {
        var signatureMock = new Mock<IRpsSignatureService>();
        signatureMock
            .Setup(x => x.SignRps(It.IsAny<Microled.Nfe.Service.Domain.Entities.Rps>(), It.IsAny<X509Certificate2>()))
            .Returns("ASSINATURA_TESTE");

        var certProviderMock = new Mock<ICertificateProvider>();
        certProviderMock.Setup(x => x.GetCertificate()).Returns(TestCertificateHelper.CreateTestCertificateWithPrivateKey());

        var sut = new RpsBatchPreparationService(
            signatureMock.Object,
            certProviderMock.Object,
            Options.Create(new IbptCargaTributariaOptions
            {
                PreencherQuandoAusente = true,
                PercentualFracaoPadrao = 0.1645m,
                FontePadrao = "IBPT"
            }),
            new FixedLocalDateTimeProvider(new DateOnly(2026, 1, 1)));

        var request = BuildRequest(
            dataEmissao: new DateOnly(2026, 1, 1),
            discriminacao: "Servico",
            valorServico: 10_000.00m,
            valorPis: 0m,
            valorCofins: 0m,
            valorCsll: 0m,
            valorIr: 0m,
            valorInss: 0m,
            valorTotalRecebido: 10_000.00m);
        request.RpsList[0].Tributos!.ValorCargaTributaria = null;
        request.RpsList[0].Tributos!.PercentualCargaTributaria = null;
        request.RpsList[0].Tributos!.FonteCargaTributaria = null;

        var batch = sut.PrepareSignedBatch(request);
        var rps = batch.RpsList.Single();

        rps.Tributos!.ValorCargaTributaria!.Value.Should().Be(1645.00m);
        rps.Tributos.PercentualCargaTributaria.Should().Be(0.1645m);
        rps.Tributos.FonteCargaTributaria.Should().Be("IBPT");

        var texto = rps.Item.Discriminacao;
        texto.Should().Contain("R$ 1.645,00 (16,45%) / IBPT");
    }

    private static SendRpsRequestDto BuildRequest(
        DateOnly dataEmissao,
        string discriminacao,
        decimal valorServico,
        decimal valorPis,
        decimal valorCofins,
        decimal valorCsll,
        decimal valorIr,
        decimal valorInss,
        decimal? valorTotalRecebido)
    {
        return new SendRpsRequestDto
        {
            Prestador = new ServiceProviderDto
            {
                CpfCnpj = "12345678000190",
                InscricaoMunicipal = 12345678,
                RazaoSocial = "Prestador Teste"
            },
            DataInicio = dataEmissao,
            DataFim = dataEmissao,
            RpsList =
            [
                new RpsDto
                {
                    InscricaoPrestador = 12345678,
                    SerieRps = "A",
                    NumeroRps = 1,
                    TipoRPS = "RPS",
                    DataEmissao = dataEmissao,
                    StatusRPS = "N",
                    TributacaoRPS = "T",
                    Item = new RpsItemDto
                    {
                        CodigoServico = 101,
                        Discriminacao = discriminacao,
                        ValorServicos = valorServico,
                        ValorDeducoes = 0m,
                        AliquotaServicos = 0.05m,
                        IssRetido = false
                    },
                    Tributos = new RpsTributosDto
                    {
                        ValorPIS = valorPis,
                        ValorCOFINS = valorCofins,
                        ValorCSLL = valorCsll,
                        ValorIR = valorIr,
                        ValorINSS = valorInss,
                        ValorTotalRecebido = valorTotalRecebido
                    }
                }
            ]
        };
    }
}
