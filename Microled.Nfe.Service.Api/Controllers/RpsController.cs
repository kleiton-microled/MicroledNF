using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.Service.Api.Controllers;

/// <summary>
/// Controller for RPS operations
/// </summary>
[ApiController]
[Route("api/v1/rps")]
[Produces("application/json")]
public class RpsController : ControllerBase
{
    private readonly ISendRpsUseCase _sendRpsUseCase;
    private readonly ILogger<RpsController> _logger;

    public RpsController(ISendRpsUseCase sendRpsUseCase, ILogger<RpsController> logger)
    {
        _sendRpsUseCase = sendRpsUseCase ?? throw new ArgumentNullException(nameof(sendRpsUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a batch of RPS to be converted to NFe
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/rps/send
    ///     {
    ///       "prestador": {
    ///         "cpfCnpj": "12345678000190",
    ///         "inscricaoMunicipal": 12345678,
    ///         "razaoSocial": "Empresa Exemplo Ltda",
    ///         "endereco": {
    ///           "tipoLogradouro": "Rua",
    ///           "logradouro": "Exemplo",
    ///           "numero": "123",
    ///           "bairro": "Centro",
    ///           "codigoMunicipio": 3550308,
    ///           "uf": "SP",
    ///           "cep": 12345678
    ///         },
    ///         "email": "contato@exemplo.com.br"
    ///       },
    ///       "rpsList": [
    ///         {
    ///           "inscricaoPrestador": 12345678,
    ///           "serieRps": "A",
    ///           "numeroRps": 1,
    ///           "tipoRPS": "RPS",
    ///           "dataEmissao": "2024-01-15",
    ///           "statusRPS": "N",
    ///           "tributacaoRPS": "T",
    ///           "item": {
    ///             "codigoServico": 1234,
    ///             "discriminacao": "Serviços de consultoria em TI",
    ///             "valorServicos": 1000.00,
    ///             "valorDeducoes": 0.00,
    ///             "aliquotaServicos": 0.05,
    ///             "issRetido": false
    ///           },
    ///           "tomador": {
    ///             "cpfCnpj": "98765432000111",
    ///             "razaoSocial": "Cliente Exemplo Ltda",
    ///             "endereco": {
    ///               "tipoLogradouro": "Avenida",
    ///               "logradouro": "Cliente",
    ///               "numero": "456",
    ///               "bairro": "Jardim",
    ///               "codigoMunicipio": 3550308,
    ///               "uf": "SP",
    ///               "cep": 87654321
    ///             }
    ///           }
    ///         }
    ///       ],
    ///       "dataInicio": "2024-01-01",
    ///       "dataFim": "2024-01-31",
    ///       "transacao": true
    ///     }
    /// </remarks>
    /// <param name="request">RPS batch request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with protocol and generated NFe keys</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(SendRpsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendRpsResponseDto>> SendRps(
        [FromBody] SendRpsRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to send RPS batch with {Count} RPS", request.RpsList.Count);

        try
        {
            var response = await _sendRpsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending RPS batch");
            throw;
        }
    }
}

