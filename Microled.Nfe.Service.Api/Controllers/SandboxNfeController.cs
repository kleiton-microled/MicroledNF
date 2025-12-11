using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Configuration;

namespace Microled.Nfe.Service.Api.Controllers;

/// <summary>
/// Sandbox controller for testing NFS-e operations with sample data
/// Only available in Development and Homologation environments
/// </summary>
[ApiController]
[Route("api/v1/sandbox/nfe")]
[Produces("application/json")]
[ApiExplorerSettings(IgnoreApi = false)] // Show in Swagger for testing
public class SandboxNfeController : ControllerBase
{
    private readonly ISendRpsUseCase _sendRpsUseCase;
    private readonly IConsultNfeUseCase _consultNfeUseCase;
    private readonly ICancelNfeUseCase _cancelNfeUseCase;
    private readonly ILogger<SandboxNfeController> _logger;
    private readonly NfeServiceOptions _options;

    public SandboxNfeController(
        ISendRpsUseCase sendRpsUseCase,
        IConsultNfeUseCase consultNfeUseCase,
        ICancelNfeUseCase cancelNfeUseCase,
        ILogger<SandboxNfeController> logger,
        Microsoft.Extensions.Options.IOptions<NfeServiceOptions> options)
    {
        _sendRpsUseCase = sendRpsUseCase ?? throw new ArgumentNullException(nameof(sendRpsUseCase));
        _consultNfeUseCase = consultNfeUseCase ?? throw new ArgumentNullException(nameof(consultNfeUseCase));
        _cancelNfeUseCase = cancelNfeUseCase ?? throw new ArgumentNullException(nameof(cancelNfeUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
    }

    /// <summary>
    /// Sends a sample RPS batch for testing
    /// </summary>
    /// <returns>Response with protocol and generated NFe keys</returns>
    /// <response code="200">Returns the success response with protocol and NFe keys</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("rps/send-sample")]
    [ProducesResponseType(typeof(SendRpsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendRpsResponseDto>> SendSampleRps(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sandbox: Received request to send sample RPS");

        try
        {
            // Create sample RPS using default configuration values
            var request = CreateSampleRpsRequest();

            var response = await _sendRpsUseCase.ExecuteAsync(request, cancellationToken);

            _logger.LogInformation("Sandbox: Sample RPS sent successfully. Protocol: {Protocol}", response.Protocolo);

            return Ok(new
            {
                response.Sucesso,
                response.Protocolo,
                response.ChavesNFeRPS,
                response.Alertas,
                response.Erros,
                Raw = new
                {
                    Protocolo = response.Protocolo,
                    Environment = _options.Environment,
                    Timestamp = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sandbox: Error sending sample RPS");
            throw;
        }
    }

    /// <summary>
    /// Consults a sample NFe by key
    /// </summary>
    /// <param name="numeroNFe">NFe number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NFe information</returns>
    /// <response code="200">Returns the NFe consultation result</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("consult-sample")]
    [ProducesResponseType(typeof(ConsultNfeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConsultNfeResponseDto>> ConsultSampleNfe(
        [FromQuery] long numeroNFe,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sandbox: Received request to consult sample NFe: {NumeroNFe}", numeroNFe);

        try
        {
            var inscricaoPrestador = long.Parse(_options.DefaultIssuerIm ?? "0");

            var request = new ConsultNfeRequestDto
            {
                ChaveNFe = new NfeKeyDto
                {
                    InscricaoPrestador = inscricaoPrestador,
                    NumeroNFe = numeroNFe,
                    CodigoVerificacao = "SAMPLE",
                    ChaveNotaNacional = null
                }
            };

            var response = await _consultNfeUseCase.ExecuteAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sandbox: Error consulting sample NFe");
            throw;
        }
    }

    /// <summary>
    /// Cancels a sample NFe
    /// </summary>
    /// <param name="numeroNFe">NFe number to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    /// <response code="200">Returns the NFe cancellation result</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("cancel-sample")]
    [ProducesResponseType(typeof(CancelNfeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancelNfeResponseDto>> CancelSampleNfe(
        [FromQuery] long numeroNFe,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sandbox: Received request to cancel sample NFe: {NumeroNFe}", numeroNFe);

        try
        {
            var inscricaoPrestador = long.Parse(_options.DefaultIssuerIm ?? "0");

            var request = new CancelNfeRequestDto
            {
                ChaveNFe = new NfeKeyDto
                {
                    InscricaoPrestador = inscricaoPrestador,
                    NumeroNFe = numeroNFe,
                    CodigoVerificacao = "SAMPLE",
                    ChaveNotaNacional = null
                },
                Transacao = true
            };

            var response = await _cancelNfeUseCase.ExecuteAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sandbox: Error canceling sample NFe");
            throw;
        }
    }

    private SendRpsRequestDto CreateSampleRpsRequest()
    {
        var cnpj = _options.DefaultIssuerCnpj ?? "00000000000100";
        var im = long.Parse(_options.DefaultIssuerIm ?? "12345678");

        return new SendRpsRequestDto
        {
            Prestador = new ServiceProviderDto
            {
                CpfCnpj = cnpj,
                InscricaoMunicipal = im,
                RazaoSocial = "PRESTADOR DE SERVICOS TESTE LTDA",
                Endereco = new AddressDto
                {
                    TipoLogradouro = "R",
                    Logradouro = "Rua Teste",
                    Numero = "123",
                    Bairro = "Centro",
                    CodigoMunicipio = 3550308, // São Paulo
                    UF = "SP",
                    CEP = 01000000
                },
                Email = "teste@prestador.com.br"
            },
            RpsList = new List<RpsDto>
            {
                new RpsDto
                {
                    InscricaoPrestador = im,
                    SerieRps = "A",
                    NumeroRps = DateTime.Now.Ticks % 1000000, // Use timestamp to generate unique number
                    TipoRPS = "RPS",
                    DataEmissao = DateOnly.FromDateTime(DateTime.Now),
                    StatusRPS = "N",
                    TributacaoRPS = "T",
                    Item = new RpsItemDto
                    {
                        CodigoServico = 1234,
                        Discriminacao = "Serviços de teste para homologação NFS-e São Paulo",
                        ValorServicos = 100.00m,
                        ValorDeducoes = 0.00m,
                        AliquotaServicos = 0.05m,
                        IssRetido = false
                    },
                    Tomador = new ServiceCustomerDto
                    {
                        CpfCnpj = "11111111111111",
                        RazaoSocial = "TOMADOR DE SERVICOS TESTE S.A.",
                        Endereco = new AddressDto
                        {
                            TipoLogradouro = "AV",
                            Logradouro = "Avenida Teste",
                            Numero = "456",
                            Bairro = "Vila Teste",
                            CodigoMunicipio = 3550308,
                            UF = "SP",
                            CEP = 02000000
                        },
                        Email = "teste@tomador.com.br"
                    }
                }
            },
            DataInicio = DateOnly.FromDateTime(DateTime.Now.AddDays(-7)),
            DataFim = DateOnly.FromDateTime(DateTime.Now),
            Transacao = true
        };
    }
}

