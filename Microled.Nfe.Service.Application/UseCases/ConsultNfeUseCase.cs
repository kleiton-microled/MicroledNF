using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case implementation for consulting NFe
/// </summary>
public class ConsultNfeUseCase : IConsultNfeUseCase
{
    private readonly INfeGateway _nfeGateway;

    public ConsultNfeUseCase(INfeGateway nfeGateway)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
    }

    public async Task<ConsultNfeResponseDto> ExecuteAsync(ConsultNfeRequestDto request, CancellationToken cancellationToken)
    {
        var criteria = new ConsultNfeCriteria
        {
            ChaveNFe = request.ChaveNFe != null
                ? new NfeKey(request.ChaveNFe.InscricaoPrestador, request.ChaveNFe.NumeroNFe, request.ChaveNFe.CodigoVerificacao, request.ChaveNFe.ChaveNotaNacional)
                : null,
            ChaveRps = request.ChaveRps != null
                ? new RpsKey(request.ChaveRps.InscricaoPrestador, request.ChaveRps.NumeroRps, request.ChaveRps.SerieRps)
                : null
        };

        var result = await _nfeGateway.ConsultNfeAsync(criteria, cancellationToken);

        return new ConsultNfeResponseDto
        {
            Sucesso = result.Sucesso,
            NFeList = result.NFeList.Select((nfe, index) => new NfeDto
            {
                ChaveNFe = new NfeKeyDto
                {
                    InscricaoPrestador = nfe.ChaveNFe.InscricaoPrestador,
                    NumeroNFe = nfe.ChaveNFe.NumeroNFe,
                    CodigoVerificacao = nfe.ChaveNFe.CodigoVerificacao,
                    ChaveNotaNacional = nfe.ChaveNFe.ChaveNotaNacional
                },
                DataEmissao = nfe.DataEmissao,
                DataFatoGerador = nfe.DataFatoGerador,
                Status = nfe.Status,
                ValorServicos = nfe.ValorServicos.Value,
                ValorDeducoes = nfe.ValorDeducoes.Value,
                ValorISS = nfe.ValorISS.Value,
                CodigoVerificacao = nfe.CodigoVerificacao,
                NotaXml = index < result.NotaXmlList.Count ? result.NotaXmlList[index] : null
            }).ToList(),
            Alertas = result.Alertas.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList(),
            Erros = result.Erros.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList()
        };
    }
}

