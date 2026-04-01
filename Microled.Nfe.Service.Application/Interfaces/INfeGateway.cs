using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Gateway interface for communicating with São Paulo City Hall NFS-e Web Service
/// </summary>
public interface INfeGateway
{
    /// <summary>
    /// Sends a batch of RPS to be converted to NFe
    /// </summary>
    Task<RetornoEnvioLoteRpsResult> SendRpsBatchAsync(DomainEntities.RpsBatch batch, CancellationToken cancellationToken);

    /// <summary>
    /// Consults an NFe by its key or RPS key
    /// </summary>
    Task<ConsultaNfeResult> ConsultNfeAsync(ConsultNfeCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// Consults async batch status by protocol number
    /// </summary>
    Task<ConsultaSituacaoLoteResult> ConsultBatchStatusAsync(string numeroProtocolo, string cnpjRemetente, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels an NFe
    /// </summary>
    Task<CancelNfeResult> CancelNfeAsync(DomainEntities.NfeCancellation cancellation, CancellationToken cancellationToken);
}

/// <summary>
/// Result of sending RPS batch
/// </summary>
public class RetornoEnvioLoteRpsResult
{
    public bool Sucesso { get; set; }
    public string? Protocolo { get; set; }
    public List<NfeRpsKeyPair> ChavesNFeRPS { get; set; } = new();
    public List<Evento> Alertas { get; set; } = new();
    public List<Evento> Erros { get; set; } = new();
}

public class NfeRpsKeyPair
{
    public DomainEntities.NfeKey ChaveNFe { get; set; } = null!;
    public DomainEntities.RpsKey ChaveRPS { get; set; } = null!;
}

public class Evento
{
    public int Codigo { get; set; }
    public string? Descricao { get; set; }
    public DomainEntities.RpsKey? ChaveRPS { get; set; }
    public DomainEntities.NfeKey? ChaveNFe { get; set; }
}

/// <summary>
/// Criteria for consulting NFe
/// </summary>
public class ConsultNfeCriteria
{
    public DomainEntities.NfeKey? ChaveNFe { get; set; }
    public DomainEntities.RpsKey? ChaveRps { get; set; }
}

/// <summary>
/// Result of consulting NFe
/// </summary>
public class ConsultaNfeResult
{
    public bool Sucesso { get; set; }
    public List<DomainEntities.Nfe> NFeList { get; set; } = new();
    public List<string> NotaXmlList { get; set; } = new();
    public List<Evento> Alertas { get; set; } = new();
    public List<Evento> Erros { get; set; } = new();
}

public class ConsultaSituacaoLoteResult
{
    public bool Sucesso { get; set; }
    public int? SituacaoCodigo { get; set; }
    public string? SituacaoNome { get; set; }
    public long? NumeroLote { get; set; }
    public DateTime? DataRecebimento { get; set; }
    public DateTime? DataProcessamento { get; set; }
    public string? ResultadoOperacao { get; set; }
    public List<Evento> Erros { get; set; } = new();
}

/// <summary>
/// Result of canceling NFe
/// </summary>
public class CancelNfeResult
{
    public bool Sucesso { get; set; }
    public List<Evento> Alertas { get; set; } = new();
    public List<Evento> Erros { get; set; } = new();
}

