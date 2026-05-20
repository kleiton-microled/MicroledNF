using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case implementation for consulting async batch status by protocol number.
/// </summary>
public class ConsultBatchStatusUseCase : IConsultBatchStatusUseCase
{
    private const string PersistenceActor = "api:rps/status";

    private readonly INfeGateway _nfeGateway;
    private readonly IAsyncRpsProtocolPersistenceOrchestrator _asyncProtocolOrchestrator;

    public ConsultBatchStatusUseCase(
        INfeGateway nfeGateway,
        IAsyncRpsProtocolPersistenceOrchestrator asyncProtocolOrchestrator)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
        _asyncProtocolOrchestrator = asyncProtocolOrchestrator ?? throw new ArgumentNullException(nameof(asyncProtocolOrchestrator));
    }

    public async Task<ConsultBatchStatusResponseDto> ExecuteAsync(
        ConsultBatchStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _nfeGateway.ConsultBatchStatusAsync(
            request.NumeroProtocolo,
            request.CnpjRemetente,
            cancellationToken);

        await _asyncProtocolOrchestrator.PersistFromBatchStatusResultAsync(
            request.NumeroProtocolo,
            result,
            request.CnpjRemetente,
            PersistenceActor,
            cancellationToken);

        return new ConsultBatchStatusResponseDto
        {
            Sucesso = result.Sucesso,
            SituacaoCodigo = result.SituacaoCodigo,
            SituacaoNome = result.SituacaoNome,
            NumeroLote = result.NumeroLote,
            DataRecebimento = result.DataRecebimento,
            DataProcessamento = result.DataProcessamento,
            ResultadoOperacao = result.ResultadoOperacao,
            Erros = result.Erros
                .Select(erro => new EventoDto
                {
                    Codigo = erro.Codigo,
                    Descricao = erro.Descricao
                })
                .ToList()
        };
    }
}
