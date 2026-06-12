using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using DomainNotaFiscal = Microled.Nfe.Service.Domain.Entities.NotaFiscal;

namespace Microled.Nfe.Service.Application.Services;

internal static class NotaFiscalPersistenceHelper
{
    public static async Task<DomainNotaFiscal?> FindNotaAsync(
        INotaFiscalRepository repository,
        PersistNfeAuthorizationItemRequest item,
        CancellationToken cancellationToken)
    {
        if (item.NotaId.HasValue)
        {
            return await repository.GetByIdAsync(item.NotaId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(item.NumeroNota))
        {
            var byNumero = await repository.GetByNumeroNotaAsync(item.NumeroNota, cancellationToken);
            if (byNumero is not null)
            {
                return byNumero;
            }
        }

        if (!string.IsNullOrWhiteSpace(item.Protocolo))
        {
            var byProtocolo = await repository.GetByProtocoloAsync(item.Protocolo, cancellationToken);
            if (byProtocolo is not null)
            {
                return byProtocolo;
            }
        }

        if (!string.IsNullOrWhiteSpace(item.InscricaoPrestador)
            && !string.IsNullOrWhiteSpace(item.NumeroRps))
        {
            return await repository.GetByRpsAsync(
                item.InscricaoPrestador,
                item.SerieRps ?? string.Empty,
                item.NumeroRps,
                cancellationToken);
        }

        return null;
    }

    public static async Task ApplyAuthorizationItemAsync(
        INotaFiscalRepository repository,
        PersistNfeAuthorizationItemRequest item,
        string alteradoPor,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        logger?.LogInformation(
            "ApplyAuthorizationItem: NotaId={NotaId} Status={Status} NumeroNota={NumeroNota} Protocolo={Protocolo} NumeroRps={NumeroRps} HasXml={HasXml}",
            item.NotaId,
            item.Status,
            item.NumeroNota ?? "(null)",
            item.Protocolo ?? "(null)",
            item.NumeroRps ?? "(null)",
            !string.IsNullOrWhiteSpace(item.Xml));

        var nota = await FindNotaAsync(repository, item, cancellationToken);

        if (nota is null)
        {
            logger?.LogInformation(
                "ApplyAuthorizationItem: Nota nao encontrada, criando nova. Protocolo={Protocolo} NumeroRps={NumeroRps}",
                item.Protocolo ?? "(null)",
                item.NumeroRps ?? "(null)");

            nota = NotaFiscal.Create(
                criadoPor: alteradoPor,
                protocolo: item.Protocolo,
                numeroRps: item.NumeroRps,
                serieRps: item.SerieRps,
                inscricaoPrestador: item.InscricaoPrestador);
            await repository.AddAsync(nota, cancellationToken);

            logger?.LogInformation("ApplyAuthorizationItem: Nova nota criada NotaId={NotaId}", nota.Id);
        }
        else
        {
            logger?.LogInformation(
                "ApplyAuthorizationItem: Nota encontrada NotaId={NotaId} StatusAtual={StatusAtual}",
                nota.Id,
                nota.Status);
        }

        if (item.Status == NotaFiscalStatus.Cancelled)
        {
            logger?.LogInformation("ApplyAuthorizationItem: Aplicando SetCancelled NotaId={NotaId}", nota.Id);
            nota.SetCancelled(alteradoPor, item.Xml);
        }
        else if (item.Status == NotaFiscalStatus.Rejected)
        {
            logger?.LogInformation("ApplyAuthorizationItem: Aplicando SetRejected NotaId={NotaId}", nota.Id);
            nota.SetRejected(alteradoPor, item.Xml);
        }
        else if (item.Status == NotaFiscalStatus.Error)
        {
            logger?.LogInformation("ApplyAuthorizationItem: Aplicando SetError NotaId={NotaId}", nota.Id);
            nota.SetError(alteradoPor, item.Xml);
        }
        else if (item.Status == NotaFiscalStatus.Processing)
        {
            if (!string.IsNullOrWhiteSpace(item.Protocolo))
            {
                logger?.LogInformation("ApplyAuthorizationItem: Aplicando SetProcessing NotaId={NotaId}", nota.Id);
                nota.SetProcessing(item.Protocolo, alteradoPor, item.Xml);
            }
            else
            {
                nota.SetStatus(NotaFiscalStatus.Processing, alteradoPor);
            }
        }
        else if (!string.IsNullOrWhiteSpace(item.NumeroNota))
        {
            logger?.LogInformation(
                "ApplyAuthorizationItem: Aplicando SetAuthorized NotaId={NotaId} NumeroNota={NumeroNota} DataEmissao={DataEmissao}",
                nota.Id,
                item.NumeroNota,
                item.DataEmissao);
            nota.SetAuthorized(
                item.NumeroNota,
                alteradoPor,
                item.CodigoVerificacao,
                item.NumeroLote,
                item.DataEmissao,
                item.Xml);
        }
        else if (!string.IsNullOrWhiteSpace(item.Xml))
        {
            logger?.LogInformation("ApplyAuthorizationItem: Aplicando UpdateXml NotaId={NotaId}", nota.Id);
            nota.UpdateXml(item.Xml, alteradoPor);
        }
        else
        {
            logger?.LogWarning(
                "ApplyAuthorizationItem: Nenhuma mutacao aplicada na nota NotaId={NotaId} — Status={Status} NumeroNota=(null) Xml=(null)",
                nota.Id,
                item.Status);
        }

        logger?.LogInformation("ApplyAuthorizationItem: Chamando UpdateAsync NotaId={NotaId}", nota.Id);
        await repository.UpdateAsync(nota, cancellationToken);
        logger?.LogInformation("ApplyAuthorizationItem: UpdateAsync concluido NotaId={NotaId}", nota.Id);
    }
}
