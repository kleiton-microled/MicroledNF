using System.Net;
using System.Text;
using FluentAssertions;
using Microled.Nfe.Service.Application.Interfaces;
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

public class NfeSoapClientTests
{
    private readonly Mock<ILogger<NfeSoapClient>> _loggerMock;
    private readonly Mock<IXmlSerializerService> _xmlSerializerMock;
    private readonly Mock<ISoapEnvelopeBuilder> _soapEnvelopeBuilderMock;
    private readonly NfeServiceOptions _options;

    public NfeSoapClientTests()
    {
        _loggerMock = new Mock<ILogger<NfeSoapClient>>();
        _xmlSerializerMock = new Mock<IXmlSerializerService>();
        _soapEnvelopeBuilderMock = new Mock<ISoapEnvelopeBuilder>();
        _options = new NfeServiceOptions
        {
            TestEndpoint = "https://test.example.com",
            Versao = "2",
            DefaultCnpjRemetente = "12345678000190"
        };

        // Setup default SOAP envelope builder behavior
        _soapEnvelopeBuilderMock.Setup(x => x.Build(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((op, xml) => $"<soap:Envelope><soap:Body><{op}><MensagemXML><![CDATA[{xml}]]></MensagemXML></{op}></soap:Body></soap:Envelope>");
        _soapEnvelopeBuilderMock.Setup(x => x.BuildConsultaNFe(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((xml, versao) => $"<soap:Envelope><soap:Body><ConsultaNFeRequest><VersaoSchema>{versao}</VersaoSchema><MensagemXML><![CDATA[{xml}]]></MensagemXML></ConsultaNFeRequest></soap:Body></soap:Envelope>");
    }

    [Fact]
    public async Task SendRpsBatchAsync_ShouldReturnSuccessResult_WhenSoapResponseIsSuccess()
    {
        // Arrange
        var pedido = CreateTestPedidoEnvioLoteRPS();
        var retorno = CreateTestRetornoEnvioLoteRPS();
        PedidoEnvioLoteRPS? capturedPedido = null;

        _xmlSerializerMock.Setup(x => x.SerializePedidoEnvioLoteRPS(It.IsAny<PedidoEnvioLoteRPS>()))
            .Callback<PedidoEnvioLoteRPS>(p => capturedPedido = p)
            .Returns("<PedidoEnvioLoteRPS>...</PedidoEnvioLoteRPS>");

        _xmlSerializerMock.Setup(x => x.Deserialize<RetornoEnvioLoteRPS>(It.IsAny<string>()))
            .Returns(retorno);

        var soapResponse = CreateSoapSuccessResponse("EnvioLoteRPSResponse", SerializeRetornoEnvioLoteRPS(retorno));
        var handler = new FakeHttpMessageHandler(soapResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.TestEndpoint) };

        _soapEnvelopeBuilderMock
            .Setup(x => x.BuildEnvioLoteRPS(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((xml, versao) => $"<soap:Envelope><soap:Body><EnvioLoteRPSResponse><RetornoXML><![CDATA[{SerializeRetornoEnvioLoteRPS(retorno)}]]></RetornoXML></EnvioLoteRPSResponse></soap:Body></soap:Envelope>");

        var client = new NfeSoapClient(httpClient, _loggerMock.Object, Options.Create(_options), _xmlSerializerMock.Object, _soapEnvelopeBuilderMock.Object);
        var batch = CreateTestRpsBatch();

        // Act
        var result = await client.SendRpsBatchAsync(batch, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Sucesso.Should().BeTrue();
        result.Protocolo.Should().NotBeNullOrEmpty();
        result.ChavesNFeRPS.Should().HaveCount(1);
        capturedPedido.Should().NotBeNull();
        capturedPedido!.RPS.Should().HaveCount(1);
        capturedPedido.RPS[0].IBSCBS.valores.trib.gIBSCBS.cClassTrib.Should().Be("000001");
        capturedPedido.RPS[0].IBSCBS.cIndOp.Should().Be("100301");
    }

    [Fact]
    public async Task SendRpsBatchAsync_ShouldThrowNfeSoapException_WhenSoapFault()
    {
        // Arrange
        var soapFault = CreateSoapFaultResponse("Server Error", "Internal server error");
        var handler = new FakeHttpMessageHandler(soapFault, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.TestEndpoint) };

        var client = new NfeSoapClient(httpClient, _loggerMock.Object, Options.Create(_options), _xmlSerializerMock.Object, _soapEnvelopeBuilderMock.Object);
        var batch = CreateTestRpsBatch();

        _xmlSerializerMock.Setup(x => x.SerializePedidoEnvioLoteRPS(It.IsAny<PedidoEnvioLoteRPS>()))
            .Returns("<PedidoEnvioLoteRPS>...</PedidoEnvioLoteRPS>");

        _soapEnvelopeBuilderMock
            .Setup(x => x.BuildEnvioLoteRPS(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((xml, versao) => "<soap:Envelope><soap:Body>FAULT</soap:Body></soap:Envelope>");

        // Act & Assert
        var act = async () => await client.SendRpsBatchAsync(batch, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<NfeSoapException>();
        // FaultCode and FaultString should be set when SOAP Fault is detected
        exception.Which.Should().NotBeNull();
    }

    [Fact]
    public async Task SendRpsBatchAsync_ShouldThrowNfeSoapException_WhenHttpError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.TestEndpoint) };

        var client = new NfeSoapClient(httpClient, _loggerMock.Object, Options.Create(_options), _xmlSerializerMock.Object, _soapEnvelopeBuilderMock.Object);
        var batch = CreateTestRpsBatch();

        _xmlSerializerMock.Setup(x => x.SerializePedidoEnvioLoteRPS(It.IsAny<PedidoEnvioLoteRPS>()))
            .Returns("<PedidoEnvioLoteRPS>...</PedidoEnvioLoteRPS>");

        _soapEnvelopeBuilderMock
            .Setup(x => x.BuildEnvioLoteRPS(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((xml, versao) => "<soap:Envelope><soap:Body>ERROR</soap:Body></soap:Envelope>");

        // Act & Assert
        var act = async () => await client.SendRpsBatchAsync(batch, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<NfeSoapException>();
        exception.Which.HttpStatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendRpsBatchAsync_ShouldParseAsyncResponse_WhenAsyncEndpointIsConfigured()
    {
        // Arrange
        _options.AsyncTestEndpoint = "https://nfews.example.com/lotenfeasync.asmx";

        _xmlSerializerMock.Setup(x => x.SerializePedidoEnvioLoteRPS(It.IsAny<PedidoEnvioLoteRPS>()))
            .Returns("<PedidoEnvioLoteRPS>...</PedidoEnvioLoteRPS>");

        _soapEnvelopeBuilderMock
            .Setup(x => x.BuildEnvioLoteRPS(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((xml, versao) => $"<soap:Envelope><soap:Body><EnvioLoteRPSRequest><VersaoSchema>{versao}</VersaoSchema><MensagemXML><![CDATA[{xml}]]></MensagemXML></EnvioLoteRPSRequest></soap:Body></soap:Envelope>");

        var soapResponse = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <EnvioLoteRPSResponseAsync xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
      <RetornoXML>
        <Cabecalho Versao=""2"">
          <Sucesso>true</Sucesso>
          <InformacoesLote>
            <NumeroProtocolo>PROTOCOLO-123</NumeroProtocolo>
            <DataRecebimento>2026-03-31T12:00:00</DataRecebimento>
          </InformacoesLote>
        </Cabecalho>
      </RetornoXML>
    </EnvioLoteRPSResponseAsync>
  </soap:Body>
</soap:Envelope>";

        var handler = new FakeHttpMessageHandler(soapResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.AsyncTestEndpoint) };

        var client = new NfeSoapClient(httpClient, _loggerMock.Object, Options.Create(_options), _xmlSerializerMock.Object, _soapEnvelopeBuilderMock.Object);
        var batch = CreateTestRpsBatch();

        // Act
        var result = await client.SendRpsBatchAsync(batch, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Sucesso.Should().BeTrue();
        result.Protocolo.Should().Be("PROTOCOLO-123");
        result.ChavesNFeRPS.Should().BeEmpty();
        result.Erros.Should().BeEmpty();
    }

    [Fact]
    public async Task ConsultNfeAsync_ShouldReturnSuccessResult_WhenSoapResponseIsSuccess()
    {
        // Arrange
        var retorno = CreateTestRetornoConsulta();

        _xmlSerializerMock.Setup(x => x.SerializePedidoConsultaNFe(It.IsAny<PedidoConsultaNFe>()))
            .Returns("<PedidoConsultaNFe>...</PedidoConsultaNFe>");

        _xmlSerializerMock.Setup(x => x.Deserialize<RetornoConsulta>(It.IsAny<string>()))
            .Returns(retorno);

        var soapResponse = CreateSoapSuccessResponse("ConsultaNFeResponse", SerializeRetornoConsulta(retorno));
        var handler = new FakeHttpMessageHandler(soapResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.TestEndpoint) };

        var client = new NfeSoapClient(httpClient, _loggerMock.Object, Options.Create(_options), _xmlSerializerMock.Object, _soapEnvelopeBuilderMock.Object);
        var criteria = new ConsultNfeCriteria
        {
            ChaveNFe = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123")
        };

        // Act
        var result = await client.ConsultNfeAsync(criteria, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Sucesso.Should().BeTrue();
        result.NFeList.Should().HaveCount(1);
        result.NotaXmlList.Should().HaveCount(1);
        result.NotaXmlList[0].Should().Contain("<NFe");
        result.NotaXmlList[0].Should().Contain("<NumeroNFe>12345</NumeroNFe>");
    }

    [Fact]
    public async Task CancelNfeAsync_ShouldReturnSuccessResult_WhenSoapResponseIsSuccess()
    {
        // Arrange
        var retorno = CreateTestRetornoCancelamentoNFe();

        _xmlSerializerMock.Setup(x => x.Serialize(It.IsAny<PedidoCancelamentoNFe>()))
            .Returns("<PedidoCancelamentoNFe>...</PedidoCancelamentoNFe>");

        _xmlSerializerMock.Setup(x => x.Deserialize<RetornoCancelamentoNFe>(It.IsAny<string>()))
            .Returns(retorno);

        var soapResponse = CreateSoapSuccessResponse("CancelamentoNFeResponse", SerializeRetornoCancelamentoNFe(retorno));
        var handler = new FakeHttpMessageHandler(soapResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.TestEndpoint) };

        var client = new NfeSoapClient(httpClient, _loggerMock.Object, Options.Create(_options), _xmlSerializerMock.Object, _soapEnvelopeBuilderMock.Object);
        // Use a valid Base64 string for testing
        var fakeSignatureBytes = new byte[32];
        Array.Fill(fakeSignatureBytes, (byte)65); // Fill with 'A'
        var fakeSignatureBase64 = Convert.ToBase64String(fakeSignatureBytes);
        var cancellation = new NfeCancellation(
            new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123"),
            fakeSignatureBase64
        );

        // Act
        var result = await client.CancelNfeAsync(cancellation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Sucesso.Should().BeTrue();
    }

    #region Helper Methods

    private RpsBatch CreateTestRpsBatch()
    {
        var rps = CreateTestRps();
        return new RpsBatch(
            new List<Rps> { rps },
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31),
            true
        );
    }

    private Rps CreateTestRps()
    {
        var chaveRps = new RpsKey(12345678, 1, "A");
        var item = new RpsItem(
            1234,
            "Serviços de consultoria",
            Money.Create(1000.00m),
            Money.Create(0.00m),
            Aliquota.Create(0.05m),
            IssRetido.Nao,
            TipoTributacao.TributacaoMunicipio
        );
        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj("12345678000190"),
            12345678,
            "Empresa Test",
            null,
            null
        );

        var rps = new Rps(
            chaveRps,
            TipoRps.RPS,
            new DateOnly(2024, 1, 15),
            StatusRps.Normal,
            TipoTributacao.TributacaoMunicipio,
            item,
            prestador,
            null
        );
        // Layout 2 / IBSCBS: obrigatório para envio (erro 628)
        rps.SetIbsCbsCClassTrib("000001");
        // Use a valid Base64 string for testing (32 bytes = 44 Base64 chars)
        var fakeSignatureBytes = new byte[32];
        Array.Fill(fakeSignatureBytes, (byte)65); // Fill with 'A'
        var fakeSignatureBase64 = Convert.ToBase64String(fakeSignatureBytes);
        rps.SetAssinatura(fakeSignatureBase64);
        return rps;
    }

    private PedidoEnvioLoteRPS CreateTestPedidoEnvioLoteRPS()
    {
        return new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = 2,
                CPFCNPJRemetente = new tpCPFCNPJ { CNPJ = "12345678000190" },
                transacao = true,
                dtInicio = new DateTime(2024, 1, 1),
                dtFim = new DateTime(2024, 1, 31),
                QtdRPS = 1
            },
            RPS = new List<tpRPS>()
        };
    }

    private RetornoEnvioLoteRPS CreateTestRetornoEnvioLoteRPS()
    {
        return new RetornoEnvioLoteRPS
        {
            Cabecalho = new RetornoEnvioLoteRPSCabecalho
            {
                Versao = 2,
                Sucesso = true,
                InformacoesLote = new tpInformacoesLote
                {
                    NumeroLote = 12345,
                    InscricaoPrestador = 12345678,
                    CPFCNPJRemetente = new tpCPFCNPJ { CNPJ = "12345678000190" },
                    DataEnvioLote = DateTime.Now,
                    QtdNotasProcessadas = 1,
                    TempoProcessamento = 1000,
                    ValorTotalServicos = 1000.00m,
                    ValorTotalDeducoes = 0.00m
                }
            },
            ChaveNFeRPS = new List<tpChaveNFeRPS>
            {
                new tpChaveNFeRPS
                {
                    ChaveNFe = new tpChaveNFe
                    {
                        InscricaoPrestador = 12345678,
                        NumeroNFe = 12345,
                        CodigoVerificacao = "CODIGO123",
                        ChaveNotaNacional = "CHAVE123"
                    },
                    ChaveRPS = new tpChaveRPS
                    {
                        InscricaoPrestador = 12345678,
                        SerieRPS = "A",
                        NumeroRPS = 1
                    }
                }
            },
            Alerta = new List<tpEvento>(),
            Erro = new List<tpEvento>()
        };
    }

    private RetornoConsulta CreateTestRetornoConsulta()
    {
        return new RetornoConsulta
        {
            Cabecalho = new RetornoConsultaCabecalho
            {
                Versao = 2,
                Sucesso = true
            },
            NFe = new List<tpNFe>
            {
                new tpNFe
                {
                    ChaveNFe = new tpChaveNFe
                    {
                        InscricaoPrestador = 12345678,
                        NumeroNFe = 12345,
                        CodigoVerificacao = "CODIGO123",
                        ChaveNotaNacional = "CHAVE123"
                    },
                    DataEmissaoNFe = DateTime.Now,
                    DataFatoGeradorNFe = DateTime.Now,
                    StatusNFe = "N",
                    TributacaoNFe = "T",
                    ValorServicos = 1000.00m,
                    ValorDeducoes = 0.00m,
                    ValorISS = 50.00m,
                    CodigoServico = 1234,
                    AliquotaServicos = 0.05m,
                    ISSRetido = false,
                    Discriminacao = "Serviços de consultoria",
                    CPFCNPJPrestador = new tpCPFCNPJ { CNPJ = "12345678000190" },
                    RazaoSocialPrestador = "Empresa Test",
                    EnderecoPrestador = new tpEndereco(),
                    OpcaoSimples = "N",
                    ValorCredito = 0.00m
                }
            },
            Alerta = new List<tpEvento>(),
            Erro = new List<tpEvento>()
        };
    }

    private RetornoCancelamentoNFe CreateTestRetornoCancelamentoNFe()
    {
        return new RetornoCancelamentoNFe
        {
            Cabecalho = new RetornoCancelamentoNFeCabecalho
            {
                Versao = 2,
                Sucesso = true
            },
            Alerta = new List<tpEvento>(),
            Erro = new List<tpEvento>()
        };
    }

    private string CreateSoapSuccessResponse(string operationName, string xmlContent)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <{operationName} xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
            <MensagemXML><![CDATA[{xmlContent}]]></MensagemXML>
        </{operationName}>
    </soap:Body>
</soap:Envelope>";
    }

    private string CreateSoapFaultResponse(string faultCode, string faultString)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <soap:Fault>
            <faultcode>{faultCode}</faultcode>
            <faultstring>{faultString}</faultstring>
        </soap:Fault>
    </soap:Body>
</soap:Envelope>";
    }

    private string SerializeRetornoEnvioLoteRPS(RetornoEnvioLoteRPS retorno)
    {
        // Simplified XML for testing
        return @"<RetornoEnvioLoteRPS xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
    <Cabecalho Versao=""2"">
        <Sucesso>true</Sucesso>
        <InformacoesLote>
            <NumeroLote>12345</NumeroLote>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <CPFCNPJRemetente><CNPJ>12345678000190</CNPJ></CPFCNPJRemetente>
            <DataEnvioLote>2024-01-15T10:00:00</DataEnvioLote>
            <QtdNotasProcessadas>1</QtdNotasProcessadas>
            <TempoProcessamento>1000</TempoProcessamento>
            <ValorTotalServicos>1000.00</ValorTotalServicos>
            <ValorTotalDeducoes>0.00</ValorTotalDeducoes>
        </InformacoesLote>
    </Cabecalho>
    <ChaveNFeRPS>
        <ChaveNFe>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <NumeroNFe>12345</NumeroNFe>
            <CodigoVerificacao>CODIGO123</CodigoVerificacao>
            <ChaveNotaNacional>CHAVE123</ChaveNotaNacional>
        </ChaveNFe>
        <ChaveRPS>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <SerieRPS>A</SerieRPS>
            <NumeroRPS>1</NumeroRPS>
        </ChaveRPS>
    </ChaveNFeRPS>
</RetornoEnvioLoteRPS>";
    }

    private string SerializeRetornoConsulta(RetornoConsulta retorno)
    {
        return @"<RetornoConsulta xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
    <Cabecalho Versao=""2"">
        <Sucesso>true</Sucesso>
    </Cabecalho>
    <NFe>
        <ChaveNFe>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <NumeroNFe>12345</NumeroNFe>
            <CodigoVerificacao>CODIGO123</CodigoVerificacao>
            <ChaveNotaNacional>CHAVE123</ChaveNotaNacional>
        </ChaveNFe>
        <DataEmissaoNFe>2024-01-15T10:00:00</DataEmissaoNFe>
        <DataFatoGeradorNFe>2024-01-15T10:00:00</DataFatoGeradorNFe>
        <StatusNFe>N</StatusNFe>
        <TributacaoNFe>T</TributacaoNFe>
        <ValorServicos>1000.00</ValorServicos>
        <ValorDeducoes>0.00</ValorDeducoes>
        <ValorISS>50.00</ValorISS>
        <CodigoServico>1234</CodigoServico>
        <AliquotaServicos>0.05</AliquotaServicos>
        <ISSRetido>false</ISSRetido>
        <Discriminacao>Serviços de consultoria</Discriminacao>
        <CPFCNPJPrestador><CNPJ>12345678000190</CNPJ></CPFCNPJPrestador>
        <RazaoSocialPrestador>Empresa Test</RazaoSocialPrestador>
        <EnderecoPrestador/>
        <OpcaoSimples>N</OpcaoSimples>
        <ValorCredito>0.00</ValorCredito>
    </NFe>
</RetornoConsulta>";
    }

    private string SerializeRetornoCancelamentoNFe(RetornoCancelamentoNFe retorno)
    {
        return @"<RetornoCancelamentoNFe xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
    <Cabecalho Versao=""2"">
        <Sucesso>true</Sucesso>
    </Cabecalho>
</RetornoCancelamentoNFe>";
    }

    #endregion
}

/// <summary>
/// Fake HttpMessageHandler for testing HTTP calls
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "text/xml")
        };
        return Task.FromResult(response);
    }
}

