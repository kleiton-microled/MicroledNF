using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class PersistRpsSendResultUseCase : IPersistRpsSendResultUseCase
{
    private readonly INotaFiscalRepository _repository;

    public PersistRpsSendResultUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistRpsSendResultRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CriadoPor))
        {
            return ApiResponse<PersistNotaFiscalBatchResponse>.Fail("CriadoPor is required.");
        }

        var responses = new List<NotaFiscalResponse>();

        foreach (var item in request.Itens)
        {
            var existing = await _repository.GetByRpsAsync(
                item.InscricaoPrestador,
                item.SerieRps ?? string.Empty,
                item.NumeroRps,
                cancellationToken);

            NotaFiscal nota;
            if (existing is null)
            {
                nota = NotaFiscal.Create(
                    criadoPor: request.CriadoPor,
                    protocolo: request.Protocolo,
                    numeroRps: item.NumeroRps,
                    serieRps: item.SerieRps,
                    inscricaoPrestador: item.InscricaoPrestador,
                    cnpjPrestador: request.CnpjPrestador,
                    cpfCnpjTomador: item.CpfCnpjTomador);
                await _repository.AddAsync(nota, cancellationToken);
            }
            else
            {
                nota = existing;
            }

            if (!string.IsNullOrWhiteSpace(request.Protocolo) && request.Autorizacoes.Count == 0)
            {
                nota.SetProcessing(request.Protocolo, request.CriadoPor);
                await _repository.UpdateAsync(nota, cancellationToken);
            }
            else if (!request.Sucesso)
            {
                nota.SetError(request.CriadoPor);
                await _repository.UpdateAsync(nota, cancellationToken);
            }

            responses.Add(NotaFiscalMapper.ToResponse(nota));
        }

        foreach (var auth in request.Autorizacoes)
        {
            var item = MergeProtocolo(auth, request.Protocolo);
            await NotaFiscalPersistenceHelper.ApplyAuthorizationItemAsync(
                _repository,
                item,
                request.CriadoPor,
                cancellationToken);
        }

        if (request.Autorizacoes.Count > 0)
        {
            responses = [];
            foreach (var auth in request.Autorizacoes)
            {
                var nota = await NotaFiscalPersistenceHelper.FindNotaAsync(_repository, auth, cancellationToken);
                if (nota is not null)
                {
                    responses.Add(NotaFiscalMapper.ToResponse(nota));
                }
            }
        }

        return ApiResponse<PersistNotaFiscalBatchResponse>.Ok(new PersistNotaFiscalBatchResponse
        {
            ProcessedCount = Math.Max(request.Itens.Count, request.Autorizacoes.Count),
            Notas = responses
        });
    }

    private static PersistNfeAuthorizationItemRequest MergeProtocolo(
        PersistNfeAuthorizationItemRequest auth,
        string? protocolo)
    {
        if (!string.IsNullOrWhiteSpace(auth.Protocolo) || string.IsNullOrWhiteSpace(protocolo))
        {
            return auth;
        }

        return new PersistNfeAuthorizationItemRequest
        {
            NotaId = auth.NotaId,
            Protocolo = protocolo,
            InscricaoPrestador = auth.InscricaoPrestador,
            SerieRps = auth.SerieRps,
            NumeroRps = auth.NumeroRps,
            NumeroNota = auth.NumeroNota,
            CodigoVerificacao = auth.CodigoVerificacao,
            NumeroLote = auth.NumeroLote,
            DataEmissao = auth.DataEmissao,
            Xml = auth.Xml,
            Status = auth.Status
        };
    }
}
