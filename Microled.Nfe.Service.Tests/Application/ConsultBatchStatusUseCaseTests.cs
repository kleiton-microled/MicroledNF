using FluentAssertions;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.UseCases;
using Moq;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class ConsultBatchStatusUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldConsultGatewayAndPersistViaOrchestrator()
    {
        var gateway = new Mock<INfeGateway>();
        var orchestrator = new Mock<IAsyncRpsProtocolPersistenceOrchestrator>();

        var statusResult = new ConsultaSituacaoLoteResult
        {
            Sucesso = true,
            SituacaoCodigo = 3,
            SituacaoNome = "processado",
            NumeroLote = 2466
        };

        gateway.Setup(g => g.ConsultBatchStatusAsync("PROTO-1", "12345678000190", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusResult);

        var sut = new ConsultBatchStatusUseCase(gateway.Object, orchestrator.Object);

        var response = await sut.ExecuteAsync(new ConsultBatchStatusRequestDto
        {
            NumeroProtocolo = "PROTO-1",
            CnpjRemetente = "12345678000190"
        }, CancellationToken.None);

        response.Sucesso.Should().BeTrue();
        response.SituacaoCodigo.Should().Be(3);
        response.NumeroLote.Should().Be(2466);

        orchestrator.Verify(
            o => o.PersistFromBatchStatusResultAsync(
                "PROTO-1",
                statusResult,
                "12345678000190",
                "api:rps/status",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
