using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
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
        CancellationToken cancellationToken)
    {
        var nota = await FindNotaAsync(repository, item, cancellationToken);
        if (nota is null)
        {
            nota = NotaFiscal.Create(
                criadoPor: alteradoPor,
                protocolo: item.Protocolo,
                numeroRps: item.NumeroRps,
                serieRps: item.SerieRps,
                inscricaoPrestador: item.InscricaoPrestador);
            await repository.AddAsync(nota, cancellationToken);
        }

        if (item.Status == NotaFiscalStatus.Cancelled)
        {
            nota.SetCancelled(alteradoPor, item.Xml);
        }
        else if (item.Status == NotaFiscalStatus.Rejected)
        {
            nota.SetRejected(alteradoPor, item.Xml);
        }
        else if (item.Status == NotaFiscalStatus.Error)
        {
            nota.SetError(alteradoPor, item.Xml);
        }
        else if (item.Status == NotaFiscalStatus.Processing)
        {
            if (!string.IsNullOrWhiteSpace(item.Protocolo))
            {
                nota.SetProcessing(item.Protocolo, alteradoPor, item.Xml);
            }
            else
            {
                nota.SetStatus(NotaFiscalStatus.Processing, alteradoPor);
            }
        }
        else if (!string.IsNullOrWhiteSpace(item.NumeroNota))
        {
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
            nota.UpdateXml(item.Xml, alteradoPor);
        }

        await repository.UpdateAsync(nota, cancellationToken);
    }
}
