using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.DTOs.Tomadores;
using Microled.Nfe.Service.Application.Interfaces.Tomadores;

namespace Microled.Nfe.Service.Api.Controllers;

[ApiController]
[Route("api/v1/tomadores")]
[Produces("application/json")]
public class TomadoresController : ControllerBase
{
    private readonly ISearchTomadoresUseCase _searchUseCase;
    private readonly IGetTomadorByCpfCnpjUseCase _getByCpfCnpjUseCase;
    private readonly ICreateTomadorUseCase _createUseCase;
    private readonly IUpdateTomadorUseCase _updateUseCase;
    private readonly IDeleteTomadorUseCase _deleteUseCase;

    public TomadoresController(
        ISearchTomadoresUseCase searchUseCase,
        IGetTomadorByCpfCnpjUseCase getByCpfCnpjUseCase,
        ICreateTomadorUseCase createUseCase,
        IUpdateTomadorUseCase updateUseCase,
        IDeleteTomadorUseCase deleteUseCase)
    {
        _searchUseCase = searchUseCase;
        _getByCpfCnpjUseCase = getByCpfCnpjUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedTomadorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedTomadorResponse>>> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _searchUseCase.ExecuteAsync(q, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("cpf-cnpj/{cpfCnpj}")]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TomadorResponse>>> GetByCpfCnpj(
        string cpfCnpj,
        CancellationToken cancellationToken)
    {
        var response = await _getByCpfCnpjUseCase.ExecuteAsync(cpfCnpj, cancellationToken);
        return ToActionResult(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TomadorResponse>>> Create(
        [FromBody] TomadorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _createUseCase.ExecuteAsync(request, cancellationToken);
        return ToActionResult(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TomadorResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TomadorResponse>>> Update(
        Guid id,
        [FromBody] TomadorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _updateUseCase.ExecuteAsync(id, request, cancellationToken);
        return ToActionResult(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var response = await _deleteUseCase.ExecuteAsync(id, cancellationToken);
        return ToActionResult(response);
    }

    private ActionResult<ApiResponse<T>> ToActionResult<T>(ApiResponse<T> response)
    {
        if (response.Success)
        {
            return Ok(response);
        }

        if (response.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return NotFound(response);
        }

        return BadRequest(response);
    }
}
