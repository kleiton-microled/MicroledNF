using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Api.Controllers;

[ApiController]
[Route("api/v1/notas-fiscais")]
[Produces("application/json")]
public class NotasFiscaisController : ControllerBase
{
    private readonly ICreateNotaFiscalUseCase _createUseCase;
    private readonly IGetNotaFiscalByIdUseCase _getByIdUseCase;
    private readonly ISearchNotasFiscaisUseCase _searchUseCase;
    private readonly IUpdateNotaFiscalAuthorizationUseCase _authorizationUseCase;
    private readonly IUpdateNotaFiscalStatusUseCase _statusUseCase;
    private readonly IUpdateNotaFiscalPagamentoUseCase _pagamentoUseCase;
    private readonly IAttachNotaFiscalPdfUseCase _attachPdfUseCase;
    private readonly IGetNotaFiscalXmlUseCase _getXmlUseCase;
    private readonly IGetNotaFiscalPdfUseCase _getPdfUseCase;
    private readonly IPersistRpsSendResultUseCase _persistSendResultUseCase;
    private readonly IPersistBatchStatusDataUseCase _persistBatchStatusUseCase;
    private readonly IPersistConsultNfeResultUseCase _persistConsultResultUseCase;
    private readonly IPersistCancelNfeResultUseCase _persistCancelResultUseCase;
    private readonly ILogger<NotasFiscaisController> _logger;

    public NotasFiscaisController(
        ICreateNotaFiscalUseCase createUseCase,
        IGetNotaFiscalByIdUseCase getByIdUseCase,
        ISearchNotasFiscaisUseCase searchUseCase,
        IUpdateNotaFiscalAuthorizationUseCase authorizationUseCase,
        IUpdateNotaFiscalStatusUseCase statusUseCase,
        IUpdateNotaFiscalPagamentoUseCase pagamentoUseCase,
        IAttachNotaFiscalPdfUseCase attachPdfUseCase,
        IGetNotaFiscalXmlUseCase getXmlUseCase,
        IGetNotaFiscalPdfUseCase getPdfUseCase,
        IPersistRpsSendResultUseCase persistSendResultUseCase,
        IPersistBatchStatusDataUseCase persistBatchStatusUseCase,
        IPersistConsultNfeResultUseCase persistConsultResultUseCase,
        IPersistCancelNfeResultUseCase persistCancelResultUseCase,
        ILogger<NotasFiscaisController> logger)
    {
        _createUseCase = createUseCase;
        _getByIdUseCase = getByIdUseCase;
        _searchUseCase = searchUseCase;
        _authorizationUseCase = authorizationUseCase;
        _statusUseCase = statusUseCase;
        _pagamentoUseCase = pagamentoUseCase;
        _attachPdfUseCase = attachPdfUseCase;
        _getXmlUseCase = getXmlUseCase;
        _getPdfUseCase = getPdfUseCase;
        _persistSendResultUseCase = persistSendResultUseCase;
        _persistBatchStatusUseCase = persistBatchStatusUseCase;
        _persistConsultResultUseCase = persistConsultResultUseCase;
        _persistCancelResultUseCase = persistCancelResultUseCase;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> Create(
        [FromBody] CreateNotaFiscalRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _createUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _getByIdUseCase.ExecuteAsync(id, cancellationToken);
        return ToActionResult(response, StatusCodes.Status404NotFound);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedNotaFiscalResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedNotaFiscalResponse>>> Search(
        [FromQuery] SearchNotasFiscaisRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _searchUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("authorization")]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> UpdateAuthorization(
        [FromBody] UpdateNotaFiscalAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authorizationUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response, StatusCodes.Status404NotFound);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateNotaFiscalStatusBody body,
        CancellationToken cancellationToken)
    {
        var response = await _statusUseCase.ExecuteAsync(new UpdateNotaFiscalStatusRequest
        {
            Id = id,
            Status = body.Status,
            AlteradoPor = body.AlteradoPor,
            Reason = body.Reason
        }, cancellationToken);

        return ToActionResult(response, StatusCodes.Status404NotFound);
    }

    [HttpPut("{id:guid}/pdf")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> AttachPdf(
        Guid id,
        IFormFile file,
        [FromForm] string alteradoPor,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<NotaFiscalResponse>.Fail("PDF file is required."));
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var response = await _attachPdfUseCase.ExecuteAsync(new AttachNotaFiscalPdfRequest
        {
            Id = id,
            Pdf = stream.ToArray(),
            AlteradoPor = alteradoPor
        }, cancellationToken);

        return ToActionResult(response, StatusCodes.Status404NotFound);
    }

    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var (found, pdf) = await _getPdfUseCase.ExecuteAsync(id, cancellationToken);
        if (!found)
        {
            return NotFound();
        }

        if (pdf is null or { Length: 0 })
        {
            return NotFound();
        }

        return File(pdf, "application/pdf", $"nota-fiscal-{id}.pdf");
    }

    [HttpGet("{id:guid}/xml")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadXml(Guid id, CancellationToken cancellationToken)
    {
        var response = await _getXmlUseCase.ExecuteAsync(id, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            return NotFound(response);
        }

        return Content(response.Data.Xml, "application/xml; charset=utf-8");
    }

    [HttpPatch("{id:guid}/pagamento")]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> UpdatePagamento(
        Guid id,
        [FromBody] UpdateNotaFiscalPagamentoBody body,
        CancellationToken cancellationToken)
    {
        var response = await _pagamentoUseCase.ExecuteAsync(new UpdateNotaFiscalPagamentoRequest
        {
            Id = id,
            Pago = body.Pago,
            DataPagamento = body.DataPagamento,
            ValorDepositado = body.ValorDepositado,
            AlteradoPor = body.AlteradoPor
        }, cancellationToken);

        return ToActionResult(response, StatusCodes.Status404NotFound);
    }

    [HttpPost("persist/send-result")]
    [ProducesResponseType(typeof(ApiResponse<PersistNotaFiscalBatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PersistNotaFiscalBatchResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PersistNotaFiscalBatchResponse>>> PersistSendResult(
        [FromBody] PersistRpsSendResultRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _persistSendResultUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response);
    }

    [HttpPost("persist/batch-status")]
    [ProducesResponseType(typeof(ApiResponse<PersistNotaFiscalBatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PersistNotaFiscalBatchResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PersistNotaFiscalBatchResponse>>> PersistBatchStatus(
        [FromBody] PersistBatchStatusDataRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _persistBatchStatusUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response);
    }

    [HttpPost("persist/consult-result")]
    [ProducesResponseType(typeof(ApiResponse<PersistNotaFiscalBatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PersistNotaFiscalBatchResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PersistNotaFiscalBatchResponse>>> PersistConsultResult(
        [FromBody] PersistConsultNfeResultRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _persistConsultResultUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response);
    }

    [HttpPost("persist/cancel-result")]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotaFiscalResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotaFiscalResponse>>> PersistCancelResult(
        [FromBody] PersistCancelNfeResultRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _persistCancelResultUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response, StatusCodes.Status404NotFound);
    }

    private ActionResult<ApiResponse<T>> ToActionResult<T>(
        ApiResponse<T> response,
        int notFoundStatus = StatusCodes.Status404NotFound)
    {
        if (response.Success)
        {
            return Ok(response);
        }

        if (response.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return StatusCode(notFoundStatus, response);
        }

        return BadRequest(response);
    }

    public sealed class UpdateNotaFiscalStatusBody
    {
        public NotaFiscalStatus Status { get; init; }
        public string AlteradoPor { get; init; } = string.Empty;
        public string? Reason { get; init; }
    }

    public sealed class UpdateNotaFiscalPagamentoBody
    {
        public bool Pago { get; init; }
        public DateTimeOffset? DataPagamento { get; init; }
        public decimal? ValorDepositado { get; init; }
        public string AlteradoPor { get; init; } = "frontend";
    }
}
