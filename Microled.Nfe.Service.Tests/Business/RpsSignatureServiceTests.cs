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
    public void BuildSignatureString_ShouldReturn90Characters_WhenValidRps()
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
        signatureString.Should().HaveLength(90);
    }

    [Fact]
    public void BuildSignatureString_ShouldHaveCorrectFormat_WhenValidRps()
    {
        // Arrange
        var rps = CreateTestRps();
        // Expected: 12 (InscricaoMunicipal) + 5 (SerieRPS) + 12 (NumeroRPS) + 8 (DataEmissao) + 3 (Trib/Status/ISS) +
        // 15 (ValorServicos) + 15 (ValorDeducoes) + 5 (CodigoServico) + 1 (IndicadorTomador) + 14 (CPF/CNPJTomador) = 90

        // Act
        var signatureString = _service.BuildSignatureString(rps);

        // Assert
        signatureString.Should().HaveLength(90);
        // InscricaoMunicipal (12 chars, zeros à esquerda) - usa ChaveRPS.InscricaoPrestador
        signatureString.Substring(0, 12).Should().Be("000012345678");
        // SerieRPS (5 chars, espaços à direita)
        signatureString.Substring(12, 5).Should().Be("A    ");
        // NumeroRPS (12 chars, zeros à esquerda)
        signatureString.Substring(17, 12).Should().Be("000000000001");
        // DataEmissao (8 chars, formato AAAAMMDD)
        signatureString.Substring(29, 8).Should().Be("20240115");
        // Trib/Status/ISS (3 chars)
        signatureString.Substring(37, 1).Should().Be("T");
        signatureString.Substring(38, 1).Should().Be("N");
        signatureString.Substring(39, 1).Should().Be("N");
        // IndicadorTomador (1 char) - should be '2' for CNPJ
        signatureString.Substring(75, 1).Should().Be("2");
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
        signatureString.Substring(0, 12).Should().Be("000012345678"); // usa ChaveRPS.InscricaoPrestador (não Prestador.IM)
    }

    [Fact]
    public void BuildSignatureString_ShouldPadNumeroRpsWithZeros()
    {
        // Arrange
        var chaveRps = new RpsKey(12345678, 5, "A");
        // Add tomador to ensure 90 characters
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
        signatureString.Should().HaveLength(90);
        signatureString.Substring(17, 12).Should().Be("000000000005"); // Padded with zeros
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
        signatureString.Should().HaveLength(90);
        // IndicadorTomador should be '3' (não informado)
        signatureString.Substring(75, 1).Should().Be("3");
        // CPF/CNPJ Tomador should be all zeros (14 chars)
        signatureString.Substring(76, 14).Should().Be("00000000000000");
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

    [Fact]
    public void BuildSignatureString_ShouldIncludeIndicadorTomador_WhenTomadorHasCpf()
    {
        // Arrange
        var rps = CreateTestRps();
        var tomadorComCpf = new ServiceCustomer(
            CpfCnpj.CreateFromCpf("12345678901"),
            null,
            null,
            "Tomador CPF",
            null,
            null
        );
        var rpsComCpf = new Rps(
            rps.ChaveRPS,
            rps.TipoRPS,
            rps.DataEmissao,
            rps.StatusRPS,
            rps.TributacaoRPS,
            rps.Item,
            rps.Prestador,
            tomadorComCpf
        );

        // Act
        var signatureString = _service.BuildSignatureString(rpsComCpf);

        // Assert
        signatureString.Should().HaveLength(90);
        // IndicadorTomador should be '1' for CPF (position 71)
        signatureString.Substring(75, 1).Should().Be("1");
    }

    [Fact]
    public void BuildSignatureString_ShouldIncludeIndicadorTomador_WhenTomadorHasCnpj()
    {
        // Arrange
        var rps = CreateTestRps();

        // Act
        var signatureString = _service.BuildSignatureString(rps);

        // Assert
        signatureString.Should().HaveLength(90);
        // IndicadorTomador should be '2' for CNPJ (position 71)
        signatureString.Substring(75, 1).Should().Be("2");
    }

    [Fact]
    public void BuildSignatureString_ShouldIncludeIndicadorTomador_WhenTomadorIsNull()
    {
        // Arrange
        var rps = CreateTestRps();
        var rpsSemTomador = new Rps(
            rps.ChaveRPS,
            rps.TipoRPS,
            rps.DataEmissao,
            rps.StatusRPS,
            rps.TributacaoRPS,
            rps.Item,
            rps.Prestador,
            null // No tomador
        );

        // Act
        var signatureString = _service.BuildSignatureString(rpsSemTomador);

        // Assert
        signatureString.Should().HaveLength(90);
        // IndicadorTomador should be '3' for não informado (position 71)
        signatureString.Substring(75, 1).Should().Be("3");
    }

    [Fact]
    public void CompareSignatureStrings_ShouldReturnTrue_WhenStringsMatch()
    {
        // Arrange
        var rps = CreateTestRps();
        var ourString = _service.BuildSignatureString(rps);
        var prefeituraString = ourString; // Same string

        // Act
        var result = _service.CompareSignatureStrings(ourString, prefeituraString, rps);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompareSignatureStrings_ShouldReturnFalse_WhenStringsDiffer()
    {
        // Arrange
        var rps = CreateTestRps();
        var ourString = _service.BuildSignatureString(rps);
        var prefeituraString = ourString.Substring(0, 85) + "X"; // Different last character

        // Act
        var result = _service.CompareSignatureStrings(ourString, prefeituraString, rps);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExtractSignatureStringFromError_ShouldExtractString_WhenPatternMatches()
    {
        // Arrange
        var errorMessage = "Erro 1206: Assinatura Digital do RPS incorreta - String verificada (12345678A    00000000000120240115TNN00000000001000000000000000012342100000000000000)";

        // Act
        var extracted = _service.ExtractSignatureStringFromError(errorMessage);

        // Assert
        extracted.Should().NotBeNull();
        extracted.Should().Be("12345678A    00000000000120240115TNN00000000001000000000000000012342100000000000000");
    }

    [Fact]
    public void ExtractSignatureStringFromError_ShouldReturnNull_WhenPatternNotFound()
    {
        // Arrange
        var errorMessage = "Erro genérico sem padrão";

        // Act
        var extracted = _service.ExtractSignatureStringFromError(errorMessage);

        // Assert
        extracted.Should().BeNull();
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

