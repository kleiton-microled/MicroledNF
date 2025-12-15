using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microled.Nfe.Service.Console;

/// <summary>
/// Console runner for processing RPS from Access database
/// </summary>
public interface IRpsConsoleRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of console runner for RPS processing
/// </summary>
public class RpsConsoleRunner : IRpsConsoleRunner
{
    private readonly IAccessRpsRepository _accessRpsRepository;
    private readonly ISendRpsUseCase _sendRpsUseCase;
    private readonly IRpsXmlValidationExportService _validationExportService;
    private readonly IRpsSignatureService _signatureService;
    private readonly ICertificateProvider _certificateProvider;
    private readonly ILogger<RpsConsoleRunner> _logger;
    private readonly AccessDatabaseOptions _accessOptions;
    private readonly NfeServiceOptions _nfeOptions;
    private readonly NfeValidationOptions _validationOptions;

    public RpsConsoleRunner(
        IAccessRpsRepository accessRpsRepository,
        ISendRpsUseCase sendRpsUseCase,
        IRpsXmlValidationExportService validationExportService,
        IRpsSignatureService signatureService,
        ICertificateProvider certificateProvider,
        ILogger<RpsConsoleRunner> logger,
        IOptions<AccessDatabaseOptions> accessOptions,
        IOptions<NfeServiceOptions> nfeOptions,
        IOptions<NfeValidationOptions> validationOptions)
    {
        _accessRpsRepository = accessRpsRepository ?? throw new ArgumentNullException(nameof(accessRpsRepository));
        _sendRpsUseCase = sendRpsUseCase ?? throw new ArgumentNullException(nameof(sendRpsUseCase));
        _validationExportService = validationExportService ?? throw new ArgumentNullException(nameof(validationExportService));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _accessOptions = accessOptions.Value;
        _nfeOptions = nfeOptions.Value;
        _validationOptions = validationOptions.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("================================================");
        _logger.LogInformation("Iniciando processamento de RPS a partir do Access...");
        _logger.LogInformation("Banco de dados: {DatabasePath}", _accessOptions.DatabasePath);
        _logger.LogInformation("Tabela: {TableName}", _accessOptions.RpsTableName);
        _logger.LogInformation("Tamanho do lote: {BatchSize}", _accessOptions.BatchSize);
        _logger.LogInformation("================================================");

        try
        {
            // 1. Ler RPS pendentes do Access
            var pendingRps = await _accessRpsRepository.GetPendingRpsAsync(_accessOptions.BatchSize, cancellationToken);

            if (!pendingRps.Any())
            {
                _logger.LogInformation("Nenhum RPS pendente encontrado no MDB.");
                return;
            }

            _logger.LogInformation("Encontrados {Count} RPS pendentes para processamento", pendingRps.Count);

            // Check if validation/export mode is enabled
            if (_validationOptions.ValidateXmlAndRps)
            {
                await ProcessValidationModeAsync(pendingRps, cancellationToken);
                return;
            }

            // 2. Converter RPS do domínio para SendRpsRequestDto
            var request = MapToSendRpsRequest(pendingRps);

            // 3. Chamar o caso de uso existente
            _logger.LogInformation("Enviando lote de {Count} RPS para o Web Service...", pendingRps.Count);
            var result = await _sendRpsUseCase.ExecuteAsync(request, cancellationToken);

            // 4. Exibir resultado
            _logger.LogInformation("================================================");
            _logger.LogInformation("Resultado do envio:");
            _logger.LogInformation("  Sucesso: {Sucesso}", result.Sucesso);
            _logger.LogInformation("  Protocolo: {Protocolo}", result.Protocolo ?? "N/A");
            _logger.LogInformation("  Quantidade de RPS: {Count}", pendingRps.Count);
            _logger.LogInformation("  NF-e geradas: {Count}", result.ChavesNFeRPS.Count);

            if (result.Alertas.Any())
            {
                _logger.LogWarning("  Alertas ({Count}):", result.Alertas.Count);
                foreach (var alerta in result.Alertas)
                {
                    _logger.LogWarning("    [{Codigo}] {Descricao}", alerta.Codigo, alerta.Descricao);
                }
            }

            if (result.Erros.Any())
            {
                _logger.LogError("  Erros ({Count}):", result.Erros.Count);
                foreach (var erro in result.Erros)
                {
                    _logger.LogError("    [{Codigo}] {Descricao}", erro.Codigo, erro.Descricao);
                }
            }

            // 5. Atualizar MDB apenas se o envio foi bem-sucedido
            if (result.Sucesso)
            {
                _logger.LogInformation("Atualizando status dos RPS no Access database...");
                await _accessRpsRepository.MarkAsSentAsync(pendingRps, cancellationToken);
                _logger.LogInformation("RPS atualizados como enviados no MDB.");
            }
            else
            {
                _logger.LogWarning("Envio não foi bem-sucedido. RPS não serão marcados como enviados.");
            }

            _logger.LogInformation("================================================");
            _logger.LogInformation("Processamento concluído.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o processamento de RPS");
            throw;
        }
    }

    private SendRpsRequestDto MapToSendRpsRequest(IReadOnlyList<RpsRecord> rpsRecords)
    {
        if (!rpsRecords.Any())
            throw new ArgumentException("RPS records list cannot be empty", nameof(rpsRecords));

        // Use first RPS to get prestador info (all RPS in batch should have same prestador)
        var firstRps = rpsRecords[0].Rps;
        var prestador = firstRps.Prestador;

        // Map prestador to DTO
        var prestadorDto = new ServiceProviderDto
        {
            CpfCnpj = prestador.CpfCnpj.GetValue(),
            InscricaoMunicipal = prestador.InscricaoMunicipal,
            RazaoSocial = prestador.RazaoSocial,
            Endereco = prestador.Endereco != null ? MapToAddressDto(prestador.Endereco) : null,
            Email = prestador.Email
        };

        // Map RPS list to DTOs
        var rpsListDto = rpsRecords.Select(record => MapToRpsDto(record.Rps)).ToList();

        // Determine date range from RPS
        var dates = rpsListDto.Select(r => r.DataEmissao).ToList();
        var dataInicio = dates.Min();
        var dataFim = dates.Max();

        return new SendRpsRequestDto
        {
            Prestador = prestadorDto,
            RpsList = rpsListDto,
            DataInicio = dataInicio,
            DataFim = dataFim,
            Transacao = true
        };
    }

    private RpsDto MapToRpsDto(Rps rps)
    {
        return new RpsDto
        {
            InscricaoPrestador = rps.ChaveRPS.InscricaoPrestador,
            SerieRps = rps.ChaveRPS.SerieRps,
            NumeroRps = rps.ChaveRPS.NumeroRps,
            TipoRPS = rps.TipoRPS.ToString().Replace("_", "-"),
            DataEmissao = rps.DataEmissao,
            StatusRPS = ((char)rps.StatusRPS).ToString(),
            TributacaoRPS = ((char)rps.TributacaoRPS).ToString(),
            Item = new RpsItemDto
            {
                CodigoServico = rps.Item.CodigoServico,
                Discriminacao = rps.Item.Discriminacao,
                ValorServicos = rps.Item.ValorServicos.Value,
                ValorDeducoes = rps.Item.ValorDeducoes.Value,
                AliquotaServicos = rps.Item.AliquotaServicos.Value,
                IssRetido = rps.Item.IssRetido == Domain.Enums.IssRetido.Sim
            },
            Tomador = rps.Tomador != null ? new ServiceCustomerDto
            {
                CpfCnpj = rps.Tomador.CpfCnpj?.GetValue(),
                InscricaoMunicipal = rps.Tomador.InscricaoMunicipal,
                InscricaoEstadual = rps.Tomador.InscricaoEstadual,
                RazaoSocial = rps.Tomador.RazaoSocial,
                Endereco = rps.Tomador.Endereco != null ? MapToAddressDto(rps.Tomador.Endereco) : null,
                Email = rps.Tomador.Email
            } : null
        };
    }

    private AddressDto MapToAddressDto(Domain.Entities.Address address)
    {
        return new AddressDto
        {
            TipoLogradouro = address.TipoLogradouro,
            Logradouro = address.Logradouro,
            Numero = address.Numero,
            Complemento = address.Complemento,
            Bairro = address.Bairro,
            CodigoMunicipio = address.CodigoMunicipio,
            UF = address.UF,
            CEP = address.CEP
        };
    }

    private async Task ProcessValidationModeAsync(IReadOnlyList<RpsRecord> rpsRecords, CancellationToken cancellationToken)
    {
        _logger.LogInformation("================================================");
        _logger.LogInformation("Modo validação ativado (ValidateXmlAndRps=true)");
        _logger.LogInformation("Gerando arquivos XML sem integração com WebService");
        _logger.LogInformation("================================================");

        try
        {
            // 1. Get certificate for signing
            var certificate = _certificateProvider.GetCertificate();

            // 2. Map RPS records to domain entities and sign each RPS
            var rpsList = rpsRecords.Select(record =>
            {
                var rps = record.Rps;
                
                // Generate signature for RPS using SHA1 + RSA
                var assinatura = _signatureService.SignRps(rps, certificate);
                rps.SetAssinatura(assinatura);
                
                return rps;
            }).ToList();

            // 3. Determine date range from RPS
            var dates = rpsList.Select(r => r.DataEmissao).ToList();
            var dataInicio = dates.Min();
            var dataFim = dates.Max();

            // 4. Create RpsBatch
            var batch = new RpsBatch(rpsList, dataInicio, dataFim, transacao: true);

            // 5. Export to XML files
            _logger.LogInformation("Gerando arquivos XML para {Count} RPS...", rpsList.Count);
            var exportResult = await _validationExportService.ExportAsync(batch, cancellationToken);

            // 6. Log results
            _logger.LogInformation("================================================");
            _logger.LogInformation("Lote com {Count} RPS processado.", rpsList.Count);
            _logger.LogInformation("Arquivo RPS: {RpsFilePath}", exportResult.RpsFilePath);
            _logger.LogInformation("Arquivo SOAP: {SoapFilePath}", exportResult.SoapFilePath);
            _logger.LogInformation("Pasta de saída: {OutputDirectory}", _validationOptions.OutputDirectory);
            _logger.LogInformation("================================================");
            _logger.LogInformation("Fluxo encerrado (sem integração com WebService).");

            // 7. Mark RPS as generated (optional)
            _logger.LogInformation("Atualizando status dos RPS no Access database como 'Gerado'...");
            await _accessRpsRepository.MarkAsGeneratedAsync(rpsRecords, cancellationToken);
            _logger.LogInformation("RPS atualizados como gerados no MDB.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o processamento em modo validação");
            throw;
        }
    }
}

