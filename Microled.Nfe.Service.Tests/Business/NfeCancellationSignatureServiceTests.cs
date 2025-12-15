using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Business.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Tests.Helpers;
using Moq;
using Xunit;

namespace Microled.Nfe.Service.Tests.Business;

public class NfeCancellationSignatureServiceTests
{
    private readonly Mock<ILogger<NfeCancellationSignatureService>> _loggerMock;
    private readonly NfeCancellationSignatureService _service;

    public NfeCancellationSignatureServiceTests()
    {
        _loggerMock = new Mock<ILogger<NfeCancellationSignatureService>>();
        _service = new NfeCancellationSignatureService(_loggerMock.Object);
    }

    [Fact]
    public void BuildSignatureString_ShouldReturn20Characters_WhenValidNfeKey()
    {
        // Arrange
        var nfeKey = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123");

        // Act
        var signatureString = _service.BuildSignatureString(nfeKey);

        // Assert
        signatureString.Should().HaveLength(20);
    }

    [Fact]
    public void BuildSignatureString_ShouldHaveCorrectFormat_WhenValidNfeKey()
    {
        // Arrange
        var nfeKey = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123");
        // Expected: 8 (InscricaoMunicipal) + 12 (NumeroNFe) = 20

        // Act
        var signatureString = _service.BuildSignatureString(nfeKey);

        // Assert
        signatureString.Should().HaveLength(20);
        // InscricaoMunicipal (8 chars, zeros à esquerda)
        signatureString.Substring(0, 8).Should().Be("12345678");
        // NumeroNFe (12 chars, zeros à esquerda)
        signatureString.Substring(8, 12).Should().Be("000000012345");
    }

    [Fact]
    public void BuildSignatureString_ShouldPadInscricaoMunicipalWithZeros()
    {
        // Arrange
        var nfeKey = new NfeKey(123, 12345, "CODIGO123", "CHAVE123");

        // Act
        var signatureString = _service.BuildSignatureString(nfeKey);

        // Assert
        signatureString.Substring(0, 8).Should().Be("00000123"); // Padded with zeros
    }

    [Fact]
    public void BuildSignatureString_ShouldPadNumeroNFeWithZeros()
    {
        // Arrange
        var nfeKey = new NfeKey(12345678, 5, "CODIGO123", "CHAVE123");

        // Act
        var signatureString = _service.BuildSignatureString(nfeKey);

        // Assert
        signatureString.Substring(8, 12).Should().Be("000000000005"); // Padded with zeros
    }

    [Fact]
    public void SignCancellation_ShouldReturnBase64Signature_WhenValidCertificate()
    {
        // Arrange
        var nfeKey = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123");
        var certificate = TestCertificateHelper.CreateTestCertificateWithPrivateKey();

        // Act
        var signature = _service.SignCancellation(nfeKey, certificate);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        signature.Should().MatchRegex("^[A-Za-z0-9+/=]+$"); // Base64 pattern
    }

    [Fact]
    public void SignCancellation_ShouldThrowException_WhenCertificateHasNoPrivateKey()
    {
        // Arrange
        var nfeKey = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123");
        var certificateWithoutKey = TestCertificateHelper.CreateTestCertificateWithoutPrivateKey();

        // Act & Assert
        var act = () => _service.SignCancellation(nfeKey, certificateWithoutKey);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*private key*");
    }

    [Fact]
    public void SignCancellation_ShouldThrowException_WhenCertificateIsNull()
    {
        // Arrange
        var nfeKey = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123");

        // Act & Assert
        var act = () => _service.SignCancellation(nfeKey, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildSignatureString_ShouldThrowException_WhenNfeKeyIsNull()
    {
        // Act & Assert
        var act = () => _service.BuildSignatureString(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SignCancellation_ShouldGenerateDifferentSignatures_ForDifferentNfeKeys()
    {
        // Arrange
        var nfeKey1 = new NfeKey(12345678, 12345, "CODIGO123", "CHAVE123");
        var nfeKey2 = new NfeKey(12345678, 12346, "CODIGO123", "CHAVE123");
        var certificate = TestCertificateHelper.CreateTestCertificateWithPrivateKey();

        // Act
        var signature1 = _service.SignCancellation(nfeKey1, certificate);
        var signature2 = _service.SignCancellation(nfeKey2, certificate);

        // Assert
        signature1.Should().NotBe(signature2, "different NFe keys should produce different signatures");
    }
}

