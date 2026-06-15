using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Application.DTOs.NotasFiscais;

public sealed class CreateNotaFiscalRequest
{
    public string? Protocolo { get; init; }
    public string? NumeroRps { get; init; }
    public string? SerieRps { get; init; }
    public string? InscricaoPrestador { get; init; }
    public string? CnpjPrestador { get; init; }
    public string? CpfCnpjTomador { get; init; }
    public string? Xml { get; init; }
    public string CriadoPor { get; init; } = string.Empty;
}

public sealed class UpdateNotaFiscalAuthorizationRequest
{
    public Guid? Id { get; init; }
    public string? Protocolo { get; init; }
    public string? NumeroNota { get; init; }
    public string? CodigoVerificacao { get; init; }
    public string? NumeroLote { get; init; }
    public DateTimeOffset? DataEmissao { get; init; }
    public string? Xml { get; init; }
    public string AlteradoPor { get; init; } = string.Empty;
}

public sealed class UpdateNotaFiscalStatusRequest
{
    public Guid Id { get; init; }
    public NotaFiscalStatus Status { get; init; }
    public string AlteradoPor { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

public sealed class UpdateNotaFiscalPagamentoRequest
{
    public Guid Id { get; init; }
    public bool Pago { get; init; }
    public DateTimeOffset? DataPagamento { get; init; }
    public decimal? ValorDepositado { get; init; }
    public string AlteradoPor { get; init; } = "frontend";
}

public sealed class AttachNotaFiscalPdfRequest
{
    public Guid Id { get; init; }
    public byte[] Pdf { get; init; } = [];
    public string AlteradoPor { get; init; } = string.Empty;
}

public sealed class SearchNotasFiscaisRequest
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

public sealed class NotaFiscalEventoDto
{
    public string Codigo { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
}

public sealed class NotaFiscalResponse
{
    public Guid Id { get; init; }
    public string? Protocolo { get; init; }
    public string? NumeroNota { get; init; }
    public string? CodigoVerificacao { get; init; }
    public string? NumeroLote { get; init; }
    public string? NumeroRps { get; init; }
    public string? SerieRps { get; init; }
    public string? InscricaoPrestador { get; init; }
    public string? CnpjPrestador { get; init; }
    public string? CpfCnpjTomador { get; init; }
    public string? NomeTomador { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool Pago { get; init; }
    public DateTimeOffset? DataPagamento { get; init; }
    public decimal? ValorDepositado { get; init; }
    public DateTimeOffset? DataEmissao { get; init; }
    public DateTimeOffset? DataCancelamento { get; init; }
    public string CriadoPor { get; init; } = string.Empty;
    public string? AlteradoPor { get; init; }
    public DateTimeOffset CriadoEm { get; init; }
    public DateTimeOffset? AlteradoEm { get; init; }
    public bool HasPdf { get; init; }
    public bool HasXml { get; init; }
    public IReadOnlyList<NotaFiscalEventoDto> Erros { get; init; } = [];
    public IReadOnlyList<NotaFiscalEventoDto> Alertas { get; init; } = [];

    // Dados RPS extraídos do XML
    public string? TipoRps { get; init; }
    public string? StatusRps { get; init; }
    public string? TributacaoRps { get; init; }
    public string? Discriminacao { get; init; }
    public string? CodigoMunicipio { get; init; }
    public string? ExigibilidadeISS { get; init; }
    public string? MunicipioIncidencia { get; init; }

    // Dados do Tomador extraídos do XML
    public string? TomadorEmail { get; init; }
    public string? TomadorInscricaoMunicipal { get; init; }
    public string? TomadorCep { get; init; }
    public string? TomadorLogradouro { get; init; }
    public string? TomadorNumero { get; init; }
    public string? TomadorComplemento { get; init; }
    public string? TomadorBairro { get; init; }
    public string? TomadorCodigoMunicipio { get; init; }
    public string? TomadorUf { get; init; }

    // Dados do Serviço extraídos do XML
    public string? CodigoServico { get; init; }
    public string? ValorServicos { get; init; }
    public string? AliquotaServicos { get; init; }
    public string? IssRetido { get; init; }
    public string? ValorIss { get; init; }
    public string? ValorDeducoes { get; init; }
    public string? ValorPis { get; init; }
    public string? ValorCofins { get; init; }
    public string? ValorInss { get; init; }
    public string? ValorIr { get; init; }
    public string? ValorCsll { get; init; }
    public string? OutrasRetencoes { get; init; }
    public string? DescontoCondicionado { get; init; }
    public string? DescontoIncondicionado { get; init; }

    // Dados IBS/CBS extraídos do XML
    public string? IbsIndDest { get; init; }
    public string? IbsCstIbs { get; init; }
    public string? IbsAliqEstadual { get; init; }
    public string? IbsAliqMunicipal { get; init; }
    public string? IbsCstCbs { get; init; }
    public string? IbsAliqCbs { get; init; }
}

public sealed class PagedNotaFiscalResponse
{
    public IReadOnlyList<NotaFiscalResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class NotaFiscalXmlResponse
{
    public Guid Id { get; init; }
    public string Xml { get; init; } = string.Empty;
}
