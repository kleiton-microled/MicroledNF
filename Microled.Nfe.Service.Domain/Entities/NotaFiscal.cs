using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// Persisted fiscal note aggregate.
/// </summary>
public sealed class NotaFiscal
{
    public Guid Id { get; private set; }
    public string? Protocolo { get; private set; }
    public string? NumeroNota { get; private set; }
    public string? CodigoVerificacao { get; private set; }
    public string? NumeroLote { get; private set; }
    public string? NumeroRps { get; private set; }
    public string? SerieRps { get; private set; }
    public string? InscricaoPrestador { get; private set; }
    public string? CnpjPrestador { get; private set; }
    public string? CpfCnpjTomador { get; private set; }
    public NotaFiscalStatus Status { get; private set; }
    public string? Xml { get; private set; }
    public byte[]? Pdf { get; private set; }
    public DateTimeOffset? DataEmissao { get; private set; }
    public DateTimeOffset? DataCancelamento { get; private set; }
    public bool Pago { get; private set; }
    public DateTimeOffset? DataPagamento { get; private set; }
    public decimal? ValorDepositado { get; private set; }
    public string CriadoPor { get; private set; } = string.Empty;
    public string? AlteradoPor { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset? AlteradoEm { get; private set; }

    private NotaFiscal()
    {
    }

    public static NotaFiscal Create(
        string criadoPor,
        string? protocolo = null,
        string? numeroRps = null,
        string? serieRps = null,
        string? inscricaoPrestador = null,
        string? cnpjPrestador = null,
        string? cpfCnpjTomador = null,
        string? xml = null,
        NotaFiscalStatus initialStatus = NotaFiscalStatus.Pending)
    {
        if (string.IsNullOrWhiteSpace(criadoPor))
        {
            throw new ArgumentException("CriadoPor is required.", nameof(criadoPor));
        }

        var now = DateTimeOffset.UtcNow;
        var nota = new NotaFiscal
        {
            Id = Guid.NewGuid(),
            Protocolo = protocolo,
            NumeroRps = numeroRps,
            SerieRps = serieRps,
            InscricaoPrestador = inscricaoPrestador,
            CnpjPrestador = cnpjPrestador,
            CpfCnpjTomador = cpfCnpjTomador,
            Xml = xml,
            Status = string.IsNullOrWhiteSpace(xml) ? initialStatus : NotaFiscalStatus.Generated,
            CriadoPor = criadoPor.Trim(),
            CriadoEm = now
        };

        if (!string.IsNullOrWhiteSpace(xml) && initialStatus == NotaFiscalStatus.Pending)
        {
            nota.Status = NotaFiscalStatus.Generated;
        }
        else if (initialStatus != NotaFiscalStatus.Pending)
        {
            nota.Status = initialStatus;
        }

        return nota;
    }

    public void SetGeneratedXml(string xml, string alteradoPor)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML is required.", nameof(xml));
        }

        Xml = xml;
        Status = NotaFiscalStatus.Generated;
        Touch(alteradoPor);
    }

    public void SetSent(string alteradoPor)
    {
        Status = NotaFiscalStatus.Sent;
        Touch(alteradoPor);
    }

    public void SetProcessing(string protocolo, string alteradoPor, string? xml = null)
    {
        if (string.IsNullOrWhiteSpace(protocolo))
        {
            throw new ArgumentException("Protocolo is required for processing status.", nameof(protocolo));
        }

        Protocolo = protocolo.Trim();
        if (!string.IsNullOrWhiteSpace(xml))
        {
            Xml = xml;
        }

        Status = NotaFiscalStatus.Processing;
        Touch(alteradoPor);
    }

    public void SetAuthorized(
        string numeroNota,
        string alteradoPor,
        string? codigoVerificacao = null,
        string? numeroLote = null,
        DateTimeOffset? dataEmissao = null,
        string? xml = null)
    {
        if (string.IsNullOrWhiteSpace(numeroNota))
        {
            throw new ArgumentException("NumeroNota is required for authorization.", nameof(numeroNota));
        }

        NumeroNota = numeroNota.Trim();
        CodigoVerificacao = codigoVerificacao?.Trim();
        NumeroLote = numeroLote?.Trim();
        DataEmissao = (dataEmissao ?? DateTimeOffset.UtcNow).ToUniversalTime();

        if (!string.IsNullOrWhiteSpace(xml))
        {
            Xml = xml;
        }

        Status = NotaFiscalStatus.Authorized;
        Touch(alteradoPor);
    }

    public void SetRejected(string alteradoPor, string? xml = null)
    {
        if (!string.IsNullOrWhiteSpace(xml))
        {
            Xml = xml;
        }

        Status = NotaFiscalStatus.Rejected;
        Touch(alteradoPor);
    }

    public void SetCancelled(string alteradoPor, string? xml = null)
    {
        DataCancelamento = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(xml))
        {
            Xml = xml;
        }

        Status = NotaFiscalStatus.Cancelled;
        Touch(alteradoPor);
    }

    public void SetError(string alteradoPor, string? xml = null)
    {
        if (!string.IsNullOrWhiteSpace(xml))
        {
            Xml = xml;
        }

        Status = NotaFiscalStatus.Error;
        Touch(alteradoPor);
    }

    public void AttachPdf(byte[] pdf, string alteradoPor)
    {
        if (pdf is null || pdf.Length == 0)
        {
            throw new ArgumentException("PDF content cannot be empty.", nameof(pdf));
        }

        if (Pdf is { Length: > 0 } && string.IsNullOrWhiteSpace(alteradoPor))
        {
            throw new ArgumentException("AlteradoPor is required when replacing an existing PDF.", nameof(alteradoPor));
        }

        Pdf = pdf;
        Touch(alteradoPor);
    }

    public void UpdateXml(string xml, string alteradoPor)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML is required.", nameof(xml));
        }

        Xml = xml;
        Touch(alteradoPor);
    }

    public void SetStatus(NotaFiscalStatus status, string alteradoPor)
    {
        Status = status;
        Touch(alteradoPor);
    }

    public void SetPagamento(bool pago, DateTimeOffset? dataPagamento, decimal? valorDepositado, string alteradoPor)
    {
        Pago = pago;
        DataPagamento = pago ? dataPagamento : null;
        ValorDepositado = pago ? valorDepositado : null;
        Touch(alteradoPor);
    }

    private void Touch(string alteradoPor)
    {
        if (string.IsNullOrWhiteSpace(alteradoPor))
        {
            throw new ArgumentException("AlteradoPor is required.", nameof(alteradoPor));
        }

        AlteradoPor = alteradoPor.Trim();
        AlteradoEm = DateTimeOffset.UtcNow;
    }
}
