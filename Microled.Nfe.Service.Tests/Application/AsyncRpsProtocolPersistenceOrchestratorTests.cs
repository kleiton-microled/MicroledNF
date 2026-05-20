using FluentAssertions;
using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainNfe = Microled.Nfe.Service.Domain.Entities.Nfe;

namespace Microled.Nfe.Service.Tests.Application;

public class AsyncRpsProtocolPersistenceOrchestratorTests
{
    [Fact]
    public async Task PersistFromBatchStatusResult_WhenProcessed_ShouldAuthorizeNotesFromProtocol()
    {
        var gateway = new Mock<INfeGateway>();
        var repository = new Mock<INotaFiscalRepository>();
        var flowPersistence = new Mock<INotaFiscalFlowPersistenceService>();
        var authorization = new Mock<IUpdateNotaFiscalAuthorizationUseCase>();

        var nota = NotaFiscal.Create(criadoPor: "test", numeroRps: "1", serieRps: "A", inscricaoPrestador: "123");
        nota.SetProcessing("PROTO-123", "test");

        repository.Setup(r => r.ListByProtocoloAsync("PROTO-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotaFiscal> { nota });

        gateway.Setup(g => g.ConsultNfeAsync(It.IsAny<ConsultNfeCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsultaNfeResult
            {
                Sucesso = true,
                NFeList =
                [
                    new DomainNfe(
                        new NfeKey(123, 999, "ABCD"),
                        new DateTime(2026, 5, 19, 10, 0, 0),
                        new DateTime(2026, 5, 18, 10, 0, 0),
                        "Autorizada",
                        Money.Create(100),
                        Money.Create(0),
                        Money.Create(5),
                        "ABCD")
                ],
                NotaXmlList = ["<NFe/>"]
            });

        authorization.Setup(a => a.ExecuteAsync(It.IsAny<UpdateNotaFiscalAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<NotaFiscalResponse>.Ok(new NotaFiscalResponse { Id = nota.Id, Status = nameof(NotaFiscalStatus.Authorized) }));

        var sut = CreateSut(gateway, repository, flowPersistence, authorization);

        await sut.PersistFromBatchStatusResultAsync(
            "PROTO-123",
            new ConsultaSituacaoLoteResult
            {
                Sucesso = true,
                SituacaoCodigo = LoteSituacaoAsync.Processado,
                NumeroLote = 2466
            },
            "12345678000190",
            "api:rps/status",
            CancellationToken.None);

        authorization.Verify(
            a => a.ExecuteAsync(
                It.Is<UpdateNotaFiscalAuthorizationRequest>(r =>
                    r.Id == nota.Id &&
                    r.NumeroNota == "999" &&
                    r.Protocolo == "PROTO-123" &&
                    r.NumeroLote == "2466"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAndPersistAsync_WhenProcessed_ShouldCallAuthorizationUseCase()
    {
        var gateway = new Mock<INfeGateway>();
        var repository = new Mock<INotaFiscalRepository>();
        var flowPersistence = new Mock<INotaFiscalFlowPersistenceService>();
        var authorization = new Mock<IUpdateNotaFiscalAuthorizationUseCase>();

        var nota = NotaFiscal.Create(criadoPor: "test", numeroRps: "1", serieRps: "A", inscricaoPrestador: "123");
        nota.SetProcessing("PROTO-123", "test");

        gateway.Setup(g => g.ConsultBatchStatusAsync("PROTO-123", "12345678000190", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsultaSituacaoLoteResult
            {
                Sucesso = true,
                SituacaoCodigo = LoteSituacaoAsync.Processado,
                NumeroLote = 2466
            });

        repository.Setup(r => r.ListByProtocoloAsync("PROTO-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotaFiscal> { nota });

        gateway.Setup(g => g.ConsultNfeAsync(It.IsAny<ConsultNfeCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsultaNfeResult
            {
                Sucesso = true,
                NFeList =
                [
                    new DomainNfe(
                        new NfeKey(123, 999, "ABCD"),
                        new DateTime(2026, 5, 19, 10, 0, 0),
                        new DateTime(2026, 5, 18, 10, 0, 0),
                        "Autorizada",
                        Money.Create(100),
                        Money.Create(0),
                        Money.Create(5),
                        "ABCD")
                ],
                NotaXmlList = ["<NFe/>"]
            });

        authorization.Setup(a => a.ExecuteAsync(It.IsAny<UpdateNotaFiscalAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<NotaFiscalResponse>.Ok(new NotaFiscalResponse { Id = nota.Id, Status = nameof(NotaFiscalStatus.Authorized) }));

        var sut = CreateSut(gateway, repository, flowPersistence, authorization);

        await sut.ResolveAndPersistAsync("PROTO-123", "12345678000190", "api:test", CancellationToken.None);

        authorization.Verify(
            a => a.ExecuteAsync(It.IsAny<UpdateNotaFiscalAuthorizationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PersistFromBatchStatusResult_WhenInvalid_ShouldPersistRejectedStatus()
    {
        var gateway = new Mock<INfeGateway>();
        var repository = new Mock<INotaFiscalRepository>();
        var flowPersistence = new Mock<INotaFiscalFlowPersistenceService>();
        var authorization = new Mock<IUpdateNotaFiscalAuthorizationUseCase>();

        var sut = CreateSut(gateway, repository, flowPersistence, authorization);

        await sut.PersistFromBatchStatusResultAsync(
            "PROTO-ERR",
            new ConsultaSituacaoLoteResult { Sucesso = true, SituacaoCodigo = LoteSituacaoAsync.Invalidado },
            "12345678000190",
            "api:test",
            CancellationToken.None);

        flowPersistence.Verify(
            p => p.PersistBatchStatusResultAsync("PROTO-ERR", It.IsAny<ConsultaSituacaoLoteResult>(), "api:test", It.IsAny<CancellationToken>()),
            Times.Once);
        authorization.Verify(
            a => a.ExecuteAsync(It.IsAny<UpdateNotaFiscalAuthorizationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AsyncRpsProtocolPersistenceOrchestrator CreateSut(
        Mock<INfeGateway> gateway,
        Mock<INotaFiscalRepository> repository,
        Mock<INotaFiscalFlowPersistenceService> flowPersistence,
        Mock<IUpdateNotaFiscalAuthorizationUseCase> authorization) =>
        new(
            gateway.Object,
            repository.Object,
            flowPersistence.Object,
            authorization.Object,
            Options.Create(new AsyncBatchPollingOptions { Enabled = true, MaxAttempts = 1, DelaySeconds = 0 }),
            NullLogger<AsyncRpsProtocolPersistenceOrchestrator>.Instance);
}
