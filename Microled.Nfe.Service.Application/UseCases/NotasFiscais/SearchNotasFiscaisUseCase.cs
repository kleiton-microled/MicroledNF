using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class SearchNotasFiscaisUseCase : ISearchNotasFiscaisUseCase
{
    private readonly INotaFiscalRepository _repository;

    public SearchNotasFiscaisUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<PagedNotaFiscalResponse>> ExecuteAsync(
        SearchNotasFiscaisRequest request,
        CancellationToken cancellationToken)
    {
        var filter = new NotaFiscalSearchFilter
        {
            Protocolo = request.Protocolo,
            NumeroNota = request.NumeroNota,
            NumeroRps = request.NumeroRps,
            SerieRps = request.SerieRps,
            InscricaoPrestador = request.InscricaoPrestador,
            CnpjPrestador = request.CnpjPrestador,
            CpfCnpjTomador = request.CpfCnpjTomador,
            Status = request.Status,
            DataEmissaoInicio = request.DataEmissaoInicio,
            DataEmissaoFim = request.DataEmissaoFim,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var (items, totalCount) = await _repository.SearchAsync(filter, cancellationToken);
        var pageSize = filter.PageSize;
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

        var response = new PagedNotaFiscalResponse
        {
            Items = items.Select(NotaFiscalMapper.ToResponse).ToList(),
            Page = filter.Page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return ApiResponse<PagedNotaFiscalResponse>.Ok(response);
    }
}
