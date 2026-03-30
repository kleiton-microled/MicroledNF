using System.Net;
using FluentAssertions;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Client;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Exceptions;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.XmlSchemas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Client;

public class NfeSoapClient_IBSCBSTests
{
    [Fact]
    public async Task SendRpsBatchAsync_ShouldFallbackTo000001_WhenCClassTribIsMissing()
    {
        var loggerMock = new Mock<ILogger<NfeSoapClient>>();
        var xmlSerializerMock = new Mock<IXmlSerializerService>();
        var soapEnvelopeBuilderMock = new Mock<ISoapEnvelopeBuilder>();

        var options = new NfeServiceOptions
        {
            TestEndpoint = "https://test.example.com",
            Versao = "2",
            DefaultCnpjRemetente = "12345678000190"
        };

        // Minimal envelope; should not be reached if fail-fast works, but keep safe.
        soapEnvelopeBuilderMock.Setup(x => x.BuildEnvioLoteRPS(It.IsAny<string>(), It.IsAny<int>()))
            .Returns("<soap:Envelope><soap:Body></soap:Body></soap:Envelope>");

        var handler = new FakeHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.TestEndpoint) };

        var client = new NfeSoapClient(
            httpClient,
            loggerMock.Object,
            Options.Create(options),
            xmlSerializerMock.Object,
            soapEnvelopeBuilderMock.Object);

        var pedidoCaptured = (PedidoEnvioLoteRPS?)null;
        xmlSerializerMock.Setup(x => x.SerializePedidoEnvioLoteRPS(It.IsAny<PedidoEnvioLoteRPS>()))
            .Callback<PedidoEnvioLoteRPS>(p => pedidoCaptured = p)
            .Returns("<PedidoEnvioLoteRPS>...</PedidoEnvioLoteRPS>");

        var rps = CreateRps(cClassTrib: null);
        var batch = new RpsBatch(new List<Rps> { rps }, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31), true);

        var act = async () => await client.SendRpsBatchAsync(batch, CancellationToken.None);
        await act.Should().ThrowAsync<NfeSoapException>();

        pedidoCaptured.Should().NotBeNull();
        pedidoCaptured!.RPS.Should().HaveCount(1);
        pedidoCaptured.RPS[0].IBSCBS.valores.trib.gIBSCBS.cClassTrib.Should().Be("000001");
    }

    private static Rps CreateRps(string? cClassTrib)
    {
        var chaveRps = new RpsKey(12345678, 1, "A");
        var item = new RpsItem(
            2919,
            "Teste",
            Money.Create(100m),
            Money.Create(0m),
            Aliquota.Create(0m),
            IssRetido.Nao,
            TipoTributacao.TributacaoMunicipio);
        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj("12345678000190"),
            12345678,
            "Empresa Test",
            null,
            null);

        var rps = new Rps(
            chaveRps,
            TipoRps.RPS,
            new DateOnly(2024, 1, 15),
            StatusRps.Normal,
            TipoTributacao.TributacaoMunicipio,
            item,
            prestador,
            null);

        // Valid signature placeholder
        var fakeSignatureBytes = new byte[32];
        Array.Fill(fakeSignatureBytes, (byte)65);
        rps.SetAssinatura(Convert.ToBase64String(fakeSignatureBytes));

        rps.SetIbsCbsCClassTrib(cClassTrib);
        return rps;
    }
}


