using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Application.DTOs.NotasFiscais;

public sealed class PersistRpsSendResultRequest
{
    public string CriadoPor { get; init; } = string.Empty;
    public string? Protocolo { get; init; }
    public bool Sucesso { get; init; }
    public string? CnpjPrestador { get; init; }
    public List<PersistRpsItemRequest> Itens { get; init; } = [];
    public List<PersistNfeAuthorizationItemRequest> Autorizacoes { get; init; } = [];
}

public sealed class PersistRpsItemRequest
{
    public string NumeroRps { get; init; } = string.Empty;
    public string? SerieRps { get; init; }
    public string InscricaoPrestador { get; init; } = string.Empty;
    public string? CpfCnpjTomador { get; init; }
}

public sealed class PersistBatchStatusDataRequest
{
    public string NumeroProtocolo { get; init; } = string.Empty;
    public bool Sucesso { get; init; }
    public int? SituacaoCodigo { get; init; }
    public long? NumeroLote { get; init; }
    public DateTimeOffset? DataProcessamento { get; init; }
    public string AlteradoPor { get; init; } = string.Empty;
    /// <summary>Raw RetornoEnvioLoteRPS XML from ConsultaSituacaoLote (used when situacao is invalidado).</summary>
    public string? ResultadoOperacao { get; init; }
    public List<PersistNfeAuthorizationItemRequest> Autorizacoes { get; init; } = [];
}

public sealed class PersistNfeAuthorizationItemRequest
{
    public Guid? NotaId { get; init; }
    public string? Protocolo { get; init; }
    public string? InscricaoPrestador { get; init; }
    public string? SerieRps { get; init; }
    public string? NumeroRps { get; init; }
    public string? NumeroNota { get; init; }
    public string? CodigoVerificacao { get; init; }
    public string? NumeroLote { get; init; }
    public DateTimeOffset? DataEmissao { get; init; }
    public string? Xml { get; init; }
    public NotaFiscalStatus? Status { get; init; }
}

public sealed class PersistConsultNfeResultRequest
{
    public string AlteradoPor { get; init; } = string.Empty;
    public List<PersistNfeAuthorizationItemRequest> Autorizacoes { get; init; } = [];
}

public sealed class PersistCancelNfeResultRequest
{
    public string NumeroNota { get; init; } = string.Empty;
    public string AlteradoPor { get; init; } = string.Empty;
    public string? Xml { get; init; }
}

public sealed class PersistNotaFiscalBatchResponse
{
    public int ProcessedCount { get; init; }
    public IReadOnlyList<NotaFiscalResponse> Notas { get; init; } = [];
}
