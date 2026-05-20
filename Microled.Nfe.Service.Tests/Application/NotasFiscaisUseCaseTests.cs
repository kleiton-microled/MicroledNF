using FluentAssertions;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.UseCases.NotasFiscais;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;
using Moq;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class NotasFiscaisUseCaseTests
{
    private readonly Mock<INotaFiscalRepository> _repository = new();

    [Fact]
    public async Task CreateNotaFiscalUseCase_ShouldPersistNota()
    {
        NotaFiscal? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()))
            .Callback<NotaFiscal, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);

        var sut = new CreateNotaFiscalUseCase(_repository.Object);
        var result = await sut.ExecuteAsync(new CreateNotaFiscalRequest
        {
            CriadoPor = "api",
            NumeroRps = "1",
            SerieRps = "A"
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.NumeroRps.Should().Be("1");
        _repository.Verify(r => r.AddAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAuthorization_ById_ShouldUpdate()
    {
        var nota = NotaFiscal.Create(criadoPor: "api");
        _repository.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var sut = new UpdateNotaFiscalAuthorizationUseCase(_repository.Object);
        var result = await sut.ExecuteAsync(new UpdateNotaFiscalAuthorizationRequest
        {
            Id = nota.Id,
            NumeroNota = "999",
            AlteradoPor = "api"
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        nota.NumeroNota.Should().Be("999");
        _repository.Verify(r => r.UpdateAsync(nota, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAuthorization_ByProtocolo_WhenIdMissing_ShouldUpdate()
    {
        var nota = NotaFiscal.Create(criadoPor: "api", protocolo: "PROTO-1");
        _repository.Setup(r => r.GetByProtocoloAsync("PROTO-1", It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var sut = new UpdateNotaFiscalAuthorizationUseCase(_repository.Object);
        var result = await sut.ExecuteAsync(new UpdateNotaFiscalAuthorizationRequest
        {
            Protocolo = "PROTO-1",
            NumeroNota = "100",
            AlteradoPor = "api"
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        nota.NumeroNota.Should().Be("100");
    }

    [Fact]
    public async Task AttachPdf_ShouldPersistPdf()
    {
        var nota = NotaFiscal.Create(criadoPor: "api");
        _repository.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var sut = new AttachNotaFiscalPdfUseCase(_repository.Object);
        var result = await sut.ExecuteAsync(new AttachNotaFiscalPdfRequest
        {
            Id = nota.Id,
            Pdf = [0x25, 0x50, 0x44, 0x46],
            AlteradoPor = "api"
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        nota.Pdf.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_ShouldApplyFiltersAndPagination()
    {
        var filter = new NotaFiscalSearchFilter { Page = 2, PageSize = 10, Status = NotaFiscalStatus.Authorized };
        _repository
            .Setup(r => r.SearchAsync(It.IsAny<NotaFiscalSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NotaFiscal>(), 25));

        var sut = new SearchNotasFiscaisUseCase(_repository.Object);
        var result = await sut.ExecuteAsync(new SearchNotasFiscaisRequest
        {
            Page = 2,
            PageSize = 10,
            Status = NotaFiscalStatus.Authorized
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(25);
        result.Data.TotalPages.Should().Be(3);
        _repository.Verify(r => r.SearchAsync(
            It.Is<NotaFiscalSearchFilter>(f => f.Page == 2 && f.PageSize == 10 && f.Status == NotaFiscalStatus.Authorized),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
