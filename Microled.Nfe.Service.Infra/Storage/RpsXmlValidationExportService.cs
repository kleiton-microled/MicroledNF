using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.XmlSchemas;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Infra.Storage;

/// <summary>
/// Service for exporting RPS batch to XML files for validation (without calling WebService)
/// </summary>
public class RpsXmlValidationExportService : IRpsXmlValidationExportService
{
    private readonly NfeValidationOptions _options;
    private readonly NfeServiceOptions _nfeOptions;
    private readonly IXmlSerializerService _xmlSerializer;
    private readonly ISoapEnvelopeBuilder _soapEnvelopeBuilder;
    private readonly ILogger<RpsXmlValidationExportService> _logger;

    public RpsXmlValidationExportService(
        IOptions<NfeValidationOptions> options,
        IOptions<NfeServiceOptions> nfeOptions,
        IXmlSerializerService xmlSerializer,
        ISoapEnvelopeBuilder soapEnvelopeBuilder,
        ILogger<RpsXmlValidationExportService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _nfeOptions = nfeOptions.Value ?? throw new ArgumentNullException(nameof(nfeOptions));
        _xmlSerializer = xmlSerializer ?? throw new ArgumentNullException(nameof(xmlSerializer));
        _soapEnvelopeBuilder = soapEnvelopeBuilder ?? throw new ArgumentNullException(nameof(soapEnvelopeBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationExportResult> ExportAsync(RpsBatch batch, CancellationToken cancellationToken)
    {
        if (batch == null)
            throw new ArgumentNullException(nameof(batch));

        if (batch.RpsList.Count == 0)
            throw new ArgumentException("RPS batch cannot be empty", nameof(batch));

        // Ensure output directory exists
        if (string.IsNullOrWhiteSpace(_options.OutputDirectory))
            throw new InvalidOperationException("NfeValidation:OutputDirectory must be configured");

        Directory.CreateDirectory(_options.OutputDirectory);

        // Generate file names
        var timestamp = _options.IncludeTimestamp 
            ? DateTime.Now.ToString("yyyyMMdd_HHmmss") 
            : string.Empty;
        var guid = Guid.NewGuid().ToString("N")[..8];
        var suffix = string.IsNullOrEmpty(timestamp) ? guid : $"{timestamp}_{guid}";

        var rpsFileName = $"{_options.FileNamePrefix}_LoteRps_{suffix}.RPS";
        var soapFileName = $"{_options.FileNamePrefix}_SoapEnvioLoteRps_{suffix}.xml";

        var rpsFilePath = Path.Combine(_options.OutputDirectory, rpsFileName);
        var soapFilePath = Path.Combine(_options.OutputDirectory, soapFileName);

        // 1. Map RpsBatch to PedidoEnvioLoteRPS (XSD-generated class)
        var pedido = MapRpsBatchToPedidoEnvioLoteRPS(batch);

        // 2. Serialize PedidoEnvioLoteRPS to XML
        var xmlContent = _xmlSerializer.Serialize(pedido);
        _logger.LogDebug("Serialized PedidoEnvioLoteRPS XML (length: {Length})", xmlContent.Length);

        // 3. Save .RPS file
        await File.WriteAllTextAsync(rpsFilePath, xmlContent, cancellationToken);
        _logger.LogInformation("Saved RPS XML file: {FilePath}", rpsFilePath);

        // 4. Build SOAP envelope
        var soapEnvelope = _soapEnvelopeBuilder.Build("EnvioLoteRPS", xmlContent);
        _logger.LogDebug("Built SOAP envelope (length: {Length})", soapEnvelope.Length);

        // 5. Save SOAP envelope file
        await File.WriteAllTextAsync(soapFilePath, soapEnvelope, cancellationToken);
        _logger.LogInformation("Saved SOAP envelope file: {FilePath}", soapFilePath);

        return new ValidationExportResult
        {
            RpsFilePath = rpsFilePath,
            SoapFilePath = soapFilePath
        };
    }

    /// <summary>
    /// Maps domain RpsBatch to XSD-generated PedidoEnvioLoteRPS
    /// Reuses the same mapping logic as NfeSoapClient
    /// </summary>
    private PedidoEnvioLoteRPS MapRpsBatchToPedidoEnvioLoteRPS(DomainEntities.RpsBatch batch)
    {
        if (batch.RpsList.Count == 0)
            throw new ArgumentException("RPS batch cannot be empty", nameof(batch));

        var primeiroRps = batch.RpsList[0];
        var prestador = primeiroRps.Prestador;

        var pedido = new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = long.Parse(_nfeOptions.Versao.Replace(".", "")),
                CPFCNPJRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj),
                transacao = batch.Transacao,
                dtInicio = batch.DataInicio.ToDateTime(TimeOnly.MinValue),
                dtFim = batch.DataFim.ToDateTime(TimeOnly.MinValue),
                QtdRPS = batch.RpsList.Count
            },
            RPS = batch.RpsList.Select(MapRpsToTpRPS).ToList()
        };

        return pedido;
    }

    /// <summary>
    /// Maps domain Rps to XSD-generated tpRPS
    /// Reuses the same mapping logic as NfeSoapClient
    /// </summary>
    private tpRPS MapRpsToTpRPS(DomainEntities.Rps rps)
    {
        if (string.IsNullOrEmpty(rps.Assinatura))
            throw new InvalidOperationException($"RPS {rps.ChaveRPS.NumeroRps} does not have a signature. It must be signed before sending.");

        // Convert Base64 signature to byte array
        var assinaturaBytes = Convert.FromBase64String(rps.Assinatura);

        var tpRps = new tpRPS
        {
            Assinatura = assinaturaBytes,
            ChaveRPS = new tpChaveRPS
            {
                InscricaoPrestador = rps.ChaveRPS.InscricaoPrestador,
                SerieRPS = rps.ChaveRPS.SerieRps,
                NumeroRPS = rps.ChaveRPS.NumeroRps
            },
            TipoRPS = MapTipoRpsToString(rps.TipoRPS),
            DataEmissao = rps.DataEmissao.ToDateTime(TimeOnly.MinValue),
            StatusRPS = ((char)rps.StatusRPS).ToString(),
            TributacaoRPS = ((char)rps.TributacaoRPS).ToString(),
            ValorDeducoes = rps.Item.ValorDeducoes.Value,
            ValorPIS = 0.00m,
            ValorCOFINS = 0.00m,
            ValorINSS = 0.00m,
            ValorIR = 0.00m,
            ValorCSLL = 0.00m,
            CodigoServico = rps.Item.CodigoServico,
            AliquotaServicos = rps.Item.AliquotaServicos.Value,
            ISSRetido = rps.Item.IssRetido == IssRetido.Sim,
            Discriminacao = rps.Item.Discriminacao,
            ValorIPI = 0.00m,
            ExigibilidadeSuspensa = 0,
            PagamentoParceladoAntecipado = 0,
            NBS = "123456789", // TODO: Get from configuration or RPS item
            IBSCBS = CreateDefaultIBSCBS()
        };

        // Map tomador if present
        if (rps.Tomador != null)
        {
            tpRps.CPFCNPJTomador = MapCpfCnpjToTpCPFCNPJNIF(rps.Tomador.CpfCnpj);
            tpRps.InscricaoMunicipalTomador = rps.Tomador.InscricaoMunicipal;
            tpRps.InscricaoEstadualTomador = rps.Tomador.InscricaoEstadual;
            tpRps.RazaoSocialTomador = rps.Tomador.RazaoSocial;
            tpRps.EnderecoTomador = MapAddressToTpEndereco(rps.Tomador.Endereco);
            tpRps.EmailTomador = rps.Tomador.Email;
        }

        return tpRps;
    }

    private tpCPFCNPJ MapCpfCnpjToTpCPFCNPJ(CpfCnpj cpfCnpj)
    {
        if (cpfCnpj == null)
            throw new ArgumentNullException(nameof(cpfCnpj));

        var value = cpfCnpj.GetValue();
        if (cpfCnpj.IsCpf)
        {
            return new tpCPFCNPJ { CPF = value };
        }
        else
        {
            return new tpCPFCNPJ { CNPJ = value };
        }
    }

    private tpCPFCNPJNIF? MapCpfCnpjToTpCPFCNPJNIF(CpfCnpj? cpfCnpj)
    {
        if (cpfCnpj == null)
            return null;

        var value = cpfCnpj.GetValue();
        if (cpfCnpj.IsCpf)
        {
            return new tpCPFCNPJNIF { CPF = value };
        }
        else
        {
            return new tpCPFCNPJNIF { CNPJ = value };
        }
    }

    private tpEndereco? MapAddressToTpEndereco(Address? address)
    {
        if (address == null)
            return null;

        return new tpEndereco
        {
            TipoLogradouro = address.TipoLogradouro,
            Logradouro = address.Logradouro,
            NumeroEndereco = address.Numero,
            ComplementoEndereco = address.Complemento,
            Bairro = address.Bairro,
            Cidade = address.CodigoMunicipio,
            UF = address.UF,
            CEP = address.CEP
        };
    }

    private string MapTipoRpsToString(TipoRps tipoRps)
    {
        return tipoRps switch
        {
            TipoRps.RPS => "RPS",
            TipoRps.RPS_M => "RPS-M",
            TipoRps.RPS_C => "RPS-C",
            _ => throw new ArgumentException($"Unknown TipoRps: {tipoRps}")
        };
    }

    private tpIBSCBS CreateDefaultIBSCBS()
    {
        // Create a default IBSCBS structure
        // TODO: This should be configurable or come from RPS item
        return new tpIBSCBS
        {
            finNFSe = 0, // NFS-e regular
            indFinal = 0, // Não é consumidor final
            cIndOp = "123456", // TODO: Get from configuration
            indDest = 0, // Não informado
            valores = new tpValores
            {
                trib = new tpTrib
                {
                    gIBSCBS = new tpGIBSCBS
                    {
                        cClassTrib = "123456" // TODO: Get from configuration
                    }
                }
            }
        };
    }
}

