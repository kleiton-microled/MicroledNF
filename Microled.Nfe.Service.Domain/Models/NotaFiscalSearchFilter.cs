using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Domain.Models;

public sealed class NotaFiscalSearchFilter
{
    public string? Protocolo { get; init; }
    public string? NumeroNota { get; init; }
    public string? NumeroRps { get; init; }
    public string? SerieRps { get; init; }
    public string? InscricaoPrestador { get; init; }
    public string? CnpjPrestador { get; init; }
    public string? CpfCnpjTomador { get; init; }
    public NotaFiscalStatus? Status { get; init; }
    public DateTimeOffset? DataEmissaoInicio { get; init; }
    public DateTimeOffset? DataEmissaoFim { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
