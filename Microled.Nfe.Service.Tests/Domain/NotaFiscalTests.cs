using FluentAssertions;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Xunit;

namespace Microled.Nfe.Service.Tests.Domain;

public class NotaFiscalTests
{
    [Fact]
    public void Create_WithoutXml_ShouldStartAsPending()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester");

        nota.Status.Should().Be(NotaFiscalStatus.Pending);
        nota.CriadoPor.Should().Be("tester");
        nota.CriadoEm.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithXml_ShouldStartAsGenerated()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester", xml: "<rps/>");

        nota.Status.Should().Be(NotaFiscalStatus.Generated);
        nota.Xml.Should().Be("<rps/>");
    }

    [Fact]
    public void Create_WithoutCriadoPor_ShouldThrow()
    {
        var act = () => NotaFiscal.Create(criadoPor: " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetAuthorized_ShouldFillNumeroDataAndXml()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester");
        var emissao = new DateTimeOffset(2026, 5, 19, 10, 0, 0, TimeSpan.FromHours(-3));

        nota.SetAuthorized("12345", "tester", "ABCD", "99", emissao, "<nfe/>");

        nota.Status.Should().Be(NotaFiscalStatus.Authorized);
        nota.NumeroNota.Should().Be("12345");
        nota.CodigoVerificacao.Should().Be("ABCD");
        nota.NumeroLote.Should().Be("99");
        nota.DataEmissao.Should().Be(emissao);
        nota.Xml.Should().Be("<nfe/>");
        nota.AlteradoPor.Should().Be("tester");
        nota.AlteradoEm.Should().NotBeNull();
    }

    [Fact]
    public void SetCancelled_ShouldSetDataCancelamento()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester");

        nota.SetCancelled("tester");

        nota.Status.Should().Be(NotaFiscalStatus.Cancelled);
        nota.DataCancelamento.Should().NotBeNull();
    }

    [Fact]
    public void AttachPdf_ShouldStoreBytes()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester");
        var pdf = new byte[] { 1, 2, 3 };

        nota.AttachPdf(pdf, "tester");

        nota.Pdf.Should().Equal(pdf);
        nota.AlteradoPor.Should().Be("tester");
    }

    [Fact]
    public void AttachPdf_Empty_ShouldThrow()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester");

        var act = () => nota.AttachPdf([], "tester");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Touch_WithoutAlteradoPor_ShouldThrow()
    {
        var nota = NotaFiscal.Create(criadoPor: "tester");

        var act = () => nota.SetSent(" ");

        act.Should().Throw<ArgumentException>();
    }
}
