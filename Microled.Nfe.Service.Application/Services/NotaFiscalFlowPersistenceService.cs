using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Services;

public sealed class NotaFiscalFlowPersistenceService : INotaFiscalFlowPersistenceService
{
    private readonly INotaFiscalRepository _repository;
    private readonly ILogger<NotaFiscalFlowPersistenceService> _logger;

    public NotaFiscalFlowPersistenceService(
        INotaFiscalRepository repository,
        ILogger<NotaFiscalFlowPersistenceService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PersistRpsBatchBeforeSendAsync(
        DomainEntities.RpsBatch batch,
        string actor,
        CancellationToken cancellationToken)
    {
        foreach (var rps in batch.RpsList)
        {
            var existing = await _repository.GetByRpsAsync(
                rps.ChaveRPS.InscricaoPrestador.ToString(),
                rps.ChaveRPS.SerieRps,
                rps.ChaveRPS.NumeroRps.ToString(),
                cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            var prestadorDoc = rps.Prestador.CpfCnpj.GetValue();
            var cnpjPrestador = prestadorDoc.Length == 14 ? prestadorDoc : null;
            var tomador = rps.Tomador?.CpfCnpj?.GetValue();

            var nota = NotaFiscal.Create(
                criadoPor: actor,
                numeroRps: rps.ChaveRPS.NumeroRps.ToString(),
                serieRps: rps.ChaveRPS.SerieRps,
                inscricaoPrestador: rps.ChaveRPS.InscricaoPrestador.ToString(),
                cnpjPrestador: cnpjPrestador,
                cpfCnpjTomador: tomador);

            await _repository.AddAsync(nota, cancellationToken);
        }
    }

    public async Task PersistRpsBatchAfterSendAsync(
        DomainEntities.RpsBatch batch,
        RetornoEnvioLoteRpsResult result,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(result.Protocolo) && result.ChavesNFeRPS.Count == 0)
        {
            foreach (var rps in batch.RpsList)
            {
                var nota = await FindByRpsAsync(rps.ChaveRPS, cancellationToken);
                if (nota is null)
                {
                    continue;
                }

                nota.SetProcessing(result.Protocolo!, actor);
                await _repository.UpdateAsync(nota, cancellationToken);
            }

            return;
        }

        if (result.ChavesNFeRPS.Count > 0)
        {
            foreach (var par in result.ChavesNFeRPS)
            {
                var nota = await FindByRpsAsync(par.ChaveRPS, cancellationToken);
                if (nota is null)
                {
                    continue;
                }

                nota.SetSent(actor);
                nota.SetAuthorized(
                    par.ChaveNFe.NumeroNFe.ToString(),
                    actor,
                    par.ChaveNFe.CodigoVerificacao,
                    numeroLote: null,
                    dataEmissao: DateTimeOffset.UtcNow);
                await _repository.UpdateAsync(nota, cancellationToken);
            }

            return;
        }

        var status = result.Erros.Count > 0 ? NotaFiscalStatus.Rejected : NotaFiscalStatus.Error;
        foreach (var rps in batch.RpsList)
        {
            var nota = await FindByRpsAsync(rps.ChaveRPS, cancellationToken);
            if (nota is null)
            {
                continue;
            }

            if (status == NotaFiscalStatus.Rejected)
            {
                nota.SetRejected(actor);
            }
            else
            {
                nota.SetError(actor);
            }

            await _repository.UpdateAsync(nota, cancellationToken);
        }
    }

    public async Task PersistConsultResultAsync(
        ConsultaNfeResult result,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!result.Sucesso)
        {
            return;
        }

        for (var i = 0; i < result.NFeList.Count; i++)
        {
            var nfe = result.NFeList[i];
            var xml = i < result.NotaXmlList.Count ? result.NotaXmlList[i] : null;

            var nota = await _repository.GetByNumeroNotaAsync(nfe.ChaveNFe.NumeroNFe.ToString(), cancellationToken);

            if (nota is null)
            {
                nota = NotaFiscal.Create(
                    criadoPor: actor,
                    inscricaoPrestador: nfe.ChaveNFe.InscricaoPrestador.ToString());
                await _repository.AddAsync(nota, cancellationToken);
            }

            nota.SetAuthorized(
                nfe.ChaveNFe.NumeroNFe.ToString(),
                actor,
                nfe.CodigoVerificacao ?? nfe.ChaveNFe.CodigoVerificacao,
                dataEmissao: new DateTimeOffset(nfe.DataEmissao),
                xml: xml);

            await _repository.UpdateAsync(nota, cancellationToken);
        }
    }

    public async Task PersistCancelResultAsync(
        NfeKey chaveNFe,
        CancelNfeResult result,
        string? cancelXml,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!result.Sucesso)
        {
            return;
        }

        var nota = await _repository.GetByNumeroNotaAsync(chaveNFe.NumeroNFe.ToString(), cancellationToken);
        if (nota is null)
        {
            _logger.LogWarning(
                "Cancel succeeded but no persisted note found for NFe {NumeroNFe}",
                chaveNFe.NumeroNFe);
            return;
        }

        nota.SetCancelled(actor, cancelXml);
        await _repository.UpdateAsync(nota, cancellationToken);
    }

    public async Task PersistBatchStatusResultAsync(
        string protocolo,
        ConsultaSituacaoLoteResult result,
        string actor,
        CancellationToken cancellationToken)
    {
        var notas = await _repository.ListByProtocoloAsync(protocolo, cancellationToken);
        if (notas.Count == 0)
        {
            _logger.LogWarning("No persisted notes found for protocol {Protocolo}", protocolo);
            return;
        }

        var isProcessed = result.Sucesso && result.SituacaoCodigo is 1 or 2;
        var isInvalid = !result.Sucesso || result.SituacaoCodigo is 3 or 4;

        foreach (var nota in notas)
        {
            if (isProcessed && !string.IsNullOrWhiteSpace(nota.NumeroNota))
            {
                nota.SetAuthorized(
                    nota.NumeroNota,
                    actor,
                    numeroLote: result.NumeroLote?.ToString(),
                    dataEmissao: result.DataProcessamento.HasValue
                        ? new DateTimeOffset(result.DataProcessamento.Value)
                        : DateTimeOffset.UtcNow);
            }
            else if (isProcessed)
            {
                // TODO: map NumeroNFe from batch consult response when gateway exposes per-note keys.
                nota.SetStatus(NotaFiscalStatus.Processing, actor);
            }
            else if (isInvalid)
            {
                nota.SetRejected(actor);
            }
            else
            {
                nota.SetError(actor);
            }

            await _repository.UpdateAsync(nota, cancellationToken);
        }
    }

    private Task<NotaFiscal?> FindByRpsAsync(RpsKey chave, CancellationToken cancellationToken) =>
        _repository.GetByRpsAsync(
            chave.InscricaoPrestador.ToString(),
            chave.SerieRps,
            chave.NumeroRps.ToString(),
            cancellationToken);
}
