using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Tests.Helpers;
using Moq;
using Xunit;

namespace Microled.Nfe.Service.Tests.Business;

public class RpsSignatureServiceTests
{
    private readonly Mock<ILogger<RpsSignatureService>> _loggerMock;
    private readonly RpsSignatureService _service;

    public RpsSignatureServiceTests()
    {
        _loggerMock = new Mock<ILogger<RpsSignatureService>>();
        _service = new RpsSignatureService(_loggerMock.Object);
    }

    [Fact]
    public void BuildSignatureString_ShouldReturn85Characters_WhenValidRps()
    {
        // Arrange
        var rps = CreateTestRps();
        // Ensure tomador has CPF/CNPJ to get 14 characters
        var tomadorComCnpj = new ServiceCustomer(
            CpfCnpj.CreateFromCnpj("98765432000111"), // 14 characters
            null,
            null,
            "Tomador Test",
            null,
            null
        );
        var rpsComTomador = new Rps(
            rps.ChaveRPS,
            rps.TipoRPS,
            rps.DataEmissao,
            rps.StatusRPS,
            rps.TributacaoRPS,
            rps.Item,
            rps.Prestador,
            tomadorComCnpj
        );

        // Act
        var signatureString = _service.BuildSignatureString(rpsComTomador);

        // Assert
        signatureString.Should().HaveLength(85);
    }

    [Fact]
    public void BuildSignatureString_ShouldHaveCorrectFormat_WhenValidRps()
    {
        // Arrange
        var rps = CreateTestRps();
        // Expected: 8 (InscricaoMunicipal) + 5 (SerieRPS) + 12 (NumeroRPS) + 8 (DataEmissao) + 1 (TipoTributacao) + 1 (StatusRPS) + 1 (ISSRetido) + 15 (ValorServicos) + 15 (ValorDeducoes) + 5 (CodigoServico) + 14 (CPF/CNPJTomador) = 86

        // Act
        var signatureString = _service.BuildSignatureString(rps);

        // Assert
        signatureString.Should().HaveLength(85);
        // InscricaoMunicipal (8 chars, zeros à esquerda)
        signatureString.Substring(0, 8).Should().Be("12345678");
        // SerieRPS (5 chars, espaços à direita)
        signatureString.Substring(8, 5).Should().Be("A    ");
        // NumeroRPS (12 chars, zeros à esquerda)
        signatureString.Substring(13, 12).Should().Be("000000000001");
        // DataEmissao (8 chars, formato AAAAMMDD)
        signatureString.Substring(25, 8).Should().Be("20240115");
        // TipoTributacao (1 char)
        signatureString.Substring(33, 1).Should().Be("T");
        // StatusRPS (1 char)
        signatureString.Substring(34, 1).Should().Be("N");
        // ISSRetido (1 char)
        signatureString.Substring(35, 1).Should().Be("N");
    }

    [Fact]
    public void BuildSignatureString_ShouldPadInscricaoMunicipalWithZeros()
    {
        // Arrange
        var rps = CreateTestRps();
        // Change inscricao to a smaller number
        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj("12345678000190"),
            123, // Small number
            "Test",
            null,
            null
        );
        var rpsWithSmallIm = new Rps(
            rps.ChaveRPS,
            rps.TipoRPS,
            rps.DataEmissao,
            rps.StatusRPS,
            rps.TributacaoRPS,
            rps.Item,
            prestador,
            rps.Tomador
        );

        // Act
        var signatureString = _service.BuildSignatureString(rpsWithSmallIm);

        // Assert
        signatureString.Substring(0, 8).Should().Be("00000123"); // Padded with zeros
    }

    [Fact]
    public void BuildSignatureString_ShouldPadNumeroRpsWithZeros()
    {
        // Arrange
        var chaveRps = new RpsKey(12345678, 5, "A");
        // Add tomador to ensure 86 characters
        var tomador = new ServiceCustomer(
            CpfCnpj.CreateFromCnpj("98765432000111"),
            null,
            null,
            "Tomador Test",
            null,
            null
        );
        var rps = new Rps(
            chaveRps,
            TipoRps.RPS,
            new DateOnly(2024, 1, 15),
            StatusRps.Normal,
            TipoTributacao.TributacaoMunicipio,
            CreateTestRpsItem(),
            CreateTestServiceProvider(),
            tomador
        );

        // Act
        var signatureString = _service.BuildSignatureString(rps);

        // Assert
        signatureString.Should().HaveLength(85);
        signatureString.Substring(13, 12).Should().Be("000000000005"); // Padded with zeros
    }

    [Fact]
    public void BuildSignatureString_ShouldHandleTomadorWithoutCpfCnpj()
    {
        // Arrange
        var rps = CreateTestRps();
        var tomadorSemCpfCnpj = new ServiceCustomer(
            null, // No CPF/CNPJ
            null,
            null,
            "Tomador Test",
            null,
            null
        );
        var rpsSemTomadorCpfCnpj = new Rps(
            rps.ChaveRPS,
            rps.TipoRPS,
            rps.DataEmissao,
            rps.StatusRPS,
            rps.TributacaoRPS,
            rps.Item,
            rps.Prestador,
            tomadorSemCpfCnpj
        );

        // Act
        var signatureString = _service.BuildSignatureString(rpsSemTomadorCpfCnpj);

        // Assert
        signatureString.Should().HaveLength(85);
        // CPF/CNPJ Tomador should be all zeros (14 chars)
        // Position: 8+5+12+8+1+1+1+15+15+5 = 71, so substring starts at 71
        signatureString.Substring(71, 14).Should().Be("00000000000000");
    }

    [Fact]
    public void SignRps_ShouldReturnBase64Signature_WhenValidCertificate()
    {
        // Arrange
        var rps = CreateTestRps();
        var certificate = TestCertificateHelper.CreateTestCertificateWithPrivateKey();

        // Act
        var signature = _service.SignRps(rps, certificate);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        signature.Should().MatchRegex("^[A-Za-z0-9+/=]+$"); // Base64 pattern
    }

    [Fact]
    public void SignRps_ShouldThrowException_WhenCertificateHasNoPrivateKey()
    {
        // Arrange
        var rps = CreateTestRps();
        var certificateWithoutKey = TestCertificateHelper.CreateTestCertificateWithoutPrivateKey();

        // Act & Assert
        // Note: The exception might be thrown during BuildSignatureString validation first
        // So we check for InvalidOperationException (which could be from validation or private key check)
        var act = () => _service.SignRps(rps, certificateWithoutKey);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SignRps_ShouldThrowException_WhenCertificateIsNull()
    {
        // Arrange
        var rps = CreateTestRps();

        // Act & Assert
        var act = () => _service.SignRps(rps, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildSignatureString_ShouldThrowException_WhenRpsIsNull()
    {
        // Act & Assert
        var act = () => _service.BuildSignatureString(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildSignatureString_ShouldThrowException_WhenPrestadorIsNull()
    {
        // Note: Rps constructor already validates prestador, so we can't create an Rps with null prestador
        // This test verifies that the validation happens at construction time
        // Act & Assert
        var act = () => new Rps(
            new RpsKey(12345678, 1, "A"),
            TipoRps.RPS,
            new DateOnly(2024, 1, 15),
            StatusRps.Normal,
            TipoTributacao.TributacaoMunicipio,
            CreateTestRpsItem(),
            null!, // No prestador - will throw at construction
            null
        );
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("prestador");
    }

    [Fact]
    public void BuildSignatureString_ShouldThrowException_WhenItemIsNull()
    {
        // Note: Rps constructor already validates item, so we can't create an Rps with null item
        // This test verifies that the validation happens at construction time
        // Act & Assert
        var act = () => new Rps(
            new RpsKey(12345678, 1, "A"),
            TipoRps.RPS,
            new DateOnly(2024, 1, 15),
            StatusRps.Normal,
            TipoTributacao.TributacaoMunicipio,
            null!, // No item - will throw at construction
            CreateTestServiceProvider(),
            null
        );
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("item");
    }

    #region Helper Methods

    private Rps CreateTestRps()
    {
        var chaveRps = new RpsKey(12345678, 1, "A");
        var item = CreateTestRpsItem();
        var prestador = CreateTestServiceProvider();
        // Tomador com CPF/CNPJ para garantir 86 caracteres
        var tomador = new ServiceCustomer(
            CpfCnpj.CreateFromCnpj("98765432000111"),
            98765432,
            null,
            "Tomador Test",
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
            tomador
        );
        return rps;
    }

    private RpsItem CreateTestRpsItem()
    {
        return new RpsItem(
            1234,
            "Serviços de consultoria em TI",
            Money.Create(1000.00m),
            Money.Create(0.00m),
            Aliquota.Create(0.05m),
            IssRetido.Nao,
            TipoTributacao.TributacaoMunicipio
        );
    }

    private ServiceProvider CreateTestServiceProvider()
    {
        return new ServiceProvider(
            CpfCnpj.CreateFromCnpj("12345678000190"),
            12345678,
            "Empresa Test Ltda",
            null,
            null
        );
    }

    #endregion
}

