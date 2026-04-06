using FluentAssertions;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Tests.Helpers;
using Moq;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class RpsBatchPreparationServiceTests
{
    [Fact]
    public void PrepareSignedBatch_ShouldAppendTaxSummaryAndDueDateToDiscriminacao()
    {
        var signatureMock = new Mock<IRpsSignatureService>();
        signatureMock
            .Setup(x => x.SignRps(It.IsAny<Microled.Nfe.Service.Domain.Entities.Rps>(), It.IsAny<X509Certificate2>()))
            .Returns("ASSINATURA_TESTE");

        var certProviderMock = new Mock<ICertificateProvider>();
        certProviderMock.Setup(x => x.GetCertificate()).Returns(TestCertificateHelper.CreateTestCertificateWithPrivateKey());

        var sut = new RpsBatchPreparationService(signatureMock.Object, certProviderMock.Object);
        var request = BuildRequest(
            dataEmissao: new DateOnly(2026, 4, 1),
            discriminacao: "Servico mensal",
            valorServico: 24000.00m,
            valorPis: 26.00m,
            valorCofins: 120.00m,
            valorCsll: 40.00m,
            valorIr: 60.00m,
            valorInss: 0.00m,
            valorTotalRecebido: 21439.61m);

        var batch = sut.PrepareSignedBatch(request);
        var texto = batch.RpsList.Single().Item.Discriminacao;

        texto.Should().Contain("Servico mensal");
        texto.Should().Contain("PIS/COFINS/CSLL: R$186,00");
        texto.Should().Contain("Valor liquido: R$21.439,61");
        texto.Should().Contain("Vencimento: 30/04/2026");
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

        var sut = new RpsBatchPreparationService(signatureMock.Object, certProviderMock.Object);
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

        texto.Should().Contain("PIS/COFINS/CSLL: R$186,00");
        texto.Should().Contain("Valor liquido: R$3.754,00");
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
