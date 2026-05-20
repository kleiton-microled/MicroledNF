using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case implementation for sending RPS batch
/// </summary>
public class SendRpsUseCase : ISendRpsUseCase
{
    private const string PersistenceActor = "api:rps/send";

    private readonly INfeGateway _nfeGateway;
    private readonly IRpsBatchPreparationService _rpsBatchPreparationService;
    private readonly INotaFiscalFlowPersistenceService _notaFiscalPersistence;

    public SendRpsUseCase(
        INfeGateway nfeGateway,
        IRpsBatchPreparationService rpsBatchPreparationService,
        INotaFiscalFlowPersistenceService notaFiscalPersistence)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
        _rpsBatchPreparationService = rpsBatchPreparationService ?? throw new ArgumentNullException(nameof(rpsBatchPreparationService));
        _notaFiscalPersistence = notaFiscalPersistence ?? throw new ArgumentNullException(nameof(notaFiscalPersistence));
    }

    public async Task<SendRpsResponseDto> ExecuteAsync(SendRpsRequestDto request, CancellationToken cancellationToken)
    {
        var batch = _rpsBatchPreparationService.PrepareSignedBatch(request);
        await _notaFiscalPersistence.PersistRpsBatchBeforeSendAsync(batch, PersistenceActor, cancellationToken);
        var result = await _nfeGateway.SendRpsBatchAsync(batch, cancellationToken);
        await _notaFiscalPersistence.PersistRpsBatchAfterSendAsync(batch, result, PersistenceActor, cancellationToken);

        return new SendRpsResponseDto
        {
            Sucesso = result.Sucesso,
            Protocolo = result.Protocolo,
            ChavesNFeRPS = result.ChavesNFeRPS.Select(k => new NfeRpsKeyDto
            {
                ChaveNFe = new NfeKeyDto
                {
                    InscricaoPrestador = k.ChaveNFe.InscricaoPrestador,
                    NumeroNFe = k.ChaveNFe.NumeroNFe,
                    CodigoVerificacao = k.ChaveNFe.CodigoVerificacao,
                    ChaveNotaNacional = k.ChaveNFe.ChaveNotaNacional
                },
                ChaveRPS = new RpsKeyDto
                {
                    InscricaoPrestador = k.ChaveRPS.InscricaoPrestador,
                    SerieRps = k.ChaveRPS.SerieRps,
                    NumeroRps = k.ChaveRPS.NumeroRps
                }
            }).ToList(),
            Alertas = result.Alertas.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList(),
            Erros = result.Erros.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList()
        };
    }
}

