using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using System.Text;

namespace Microled.Nfe.Service.Api.Controllers;

/// <summary>
/// Controller for NFe operations
/// </summary>
[ApiController]
[Route("api/v1/nfe")]
[Produces("application/json")]
public class NfeController : ControllerBase
{
    private readonly IConsultNfeUseCase _consultNfeUseCase;
    private readonly ICancelNfeUseCase _cancelNfeUseCase;
    private readonly ILogger<NfeController> _logger;

    public NfeController(
        IConsultNfeUseCase consultNfeUseCase,
        ICancelNfeUseCase cancelNfeUseCase,
        ILogger<NfeController> logger)
    {
        _consultNfeUseCase = consultNfeUseCase ?? throw new ArgumentNullException(nameof(consultNfeUseCase));
        _cancelNfeUseCase = cancelNfeUseCase ?? throw new ArgumentNullException(nameof(cancelNfeUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Consults an NFe by its key or RPS key
    /// </summary>
    /// <remarks>
    /// Sample request (by NFe key):
    /// 
    ///     POST /api/v1/nfe/consult
    ///     {
    ///       "chaveNFe": {
    ///         "inscricaoPrestador": 12345678,
    ///         "numeroNFe": 12345,
    ///         "codigoVerificacao": "ABCD1234",
    ///         "chaveNotaNacional": "12345678901234567890123456789012345678901234"
    ///       }
    ///     }
    /// 
    /// Sample request (by RPS key):
    /// 
    ///     POST /api/v1/nfe/consult
    ///     {
    ///       "chaveRps": {
    ///         "inscricaoPrestador": 12345678,
    ///         "serieRps": "A",
    ///         "numeroRps": 1
    ///       }
    ///     }
    /// </remarks>
    /// <param name="request">Consultation request with ChaveNFe or ChaveRps</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NFe information</returns>
    [HttpPost("consult")]
    [ProducesResponseType(typeof(ConsultNfeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConsultNfeResponseDto>> ConsultNfe(
        [FromBody] ConsultNfeRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to consult NFe: {ChaveNFe} or {ChaveRps}", 
            request.ChaveNFe, request.ChaveRps);

        try
        {
            var response = await _consultNfeUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consulting NFe");
            throw;
        }
    }

    /// <summary>
    /// Returns the provided NF-e XML as a downloadable file.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/nfe/download-xml
    ///     {
    ///       "xmlContent": "&lt;NFe&gt;...&lt;/NFe&gt;",
    ///       "fileName": "nfse-2466.xml"
    ///     }
    /// </remarks>
    /// <param name="request">XML content and optional file name</param>
    /// <returns>XML file download</returns>
    [HttpPost("download-xml")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult DownloadXml([FromBody] DownloadXmlRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.XmlContent))
        {
            ModelState.AddModelError(nameof(request.XmlContent), "XmlContent is required.");
            return ValidationProblem(ModelState);
        }

        var fileName = SanitizeFileName(request.FileName);
        var bytes = Encoding.UTF8.GetBytes(request.XmlContent);

        return File(bytes, "application/xml; charset=utf-8", fileName);
    }

    /// <summary>
    /// Cancels an NFe
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/nfe/cancel
    ///     {
    ///       "chaveNFe": {
    ///         "inscricaoPrestador": 12345678,
    ///         "numeroNFe": 12345,
    ///         "codigoVerificacao": "ABCD1234",
    ///         "chaveNotaNacional": "12345678901234567890123456789012345678901234"
    ///       },
    ///       "transacao": true
    ///     }
    /// </remarks>
    /// <param name="request">Cancellation request with ChaveNFe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(CancelNfeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancelNfeResponseDto>> CancelNfe(
        [FromBody] CancelNfeRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to cancel NFe: {ChaveNFe}", request.ChaveNFe);

        try
        {
            var response = await _cancelNfeUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling NFe");
            throw;
        }
    }

    private static string SanitizeFileName(string? fileName)
    {
        var fallback = $"nfse-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xml";
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return fallback;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return fallback;
        }

        return sanitized.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            ? sanitized
            : $"{sanitized}.xml";
    }
}

