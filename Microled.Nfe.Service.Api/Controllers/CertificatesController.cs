using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.Service.Api.Controllers;

/// <summary>
/// Endpoints para descoberta, selecao e configuracao de certificados digitais.
/// </summary>
[Obsolete("Use os endpoints locais do Microled.Nfe.LocalAgent.Api para descoberta e selecao de certificados da maquina do usuario.")]
[ApiController]
[Route("api/v1/certificates")]
[Produces("application/json")]
public class CertificatesController : ControllerBase
{
    private readonly IListCertificatesUseCase _listCertificatesUseCase;
    private readonly ISelectCertificateUseCase _selectCertificateUseCase;
    private readonly IUpsertCompanyCertificateProfileUseCase _upsertCompanyCertificateProfileUseCase;
    private readonly IGetActiveCertificateProfileUseCase _getActiveCertificateProfileUseCase;
    private readonly ILogger<CertificatesController> _logger;

    public CertificatesController(
        IListCertificatesUseCase listCertificatesUseCase,
        ISelectCertificateUseCase selectCertificateUseCase,
        IUpsertCompanyCertificateProfileUseCase upsertCompanyCertificateProfileUseCase,
        IGetActiveCertificateProfileUseCase getActiveCertificateProfileUseCase,
        ILogger<CertificatesController> logger)
    {
        _listCertificatesUseCase = listCertificatesUseCase ?? throw new ArgumentNullException(nameof(listCertificatesUseCase));
        _selectCertificateUseCase = selectCertificateUseCase ?? throw new ArgumentNullException(nameof(selectCertificateUseCase));
        _upsertCompanyCertificateProfileUseCase = upsertCompanyCertificateProfileUseCase ?? throw new ArgumentNullException(nameof(upsertCompanyCertificateProfileUseCase));
        _getActiveCertificateProfileUseCase = getActiveCertificateProfileUseCase ?? throw new ArgumentNullException(nameof(getActiveCertificateProfileUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lista os certificados digitais disponiveis na maquina.
    /// </summary>
    /// <remarks>
    /// Retorna certificados encontrados principalmente em `CurrentUser/My` e `LocalMachine/My`,
    /// incluindo certificados A1 e candidatos a A3/token instalados e visiveis para o processo.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lista de certificados disponiveis para selecao.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CertificateListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<CertificateListItemDto>>> GetCertificates(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to list available certificates");
        var response = await _listCertificatesUseCase.ExecuteAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Seleciona o certificado ativo usado pelo sistema.
    /// </summary>
    /// <param name="request">Thumbprint e store do certificado escolhido pelo usuario.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resultado da selecao do certificado.</returns>
    [HttpPost("select")]
    [ProducesResponseType(typeof(SelectCertificateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SelectCertificateResponseDto>> SelectCertificate(
        [FromBody] SelectCertificateRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to select certificate {Thumbprint}", request.Thumbprint);
        var response = await _selectCertificateUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Retorna o perfil atualmente ativo.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Perfil ativo vinculado ao certificado selecionado.</returns>
    [HttpGet("active-profile")]
    [ProducesResponseType(typeof(CompanyCertificateProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompanyCertificateProfileResponseDto>> GetActiveProfile(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get the active certificate profile");

        var response = await _getActiveCertificateProfileUseCase.ExecuteAsync(cancellationToken);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    /// <summary>
    /// Cria ou atualiza os dados da empresa vinculados a um certificado.
    /// </summary>
    /// <remarks>
    /// Use este endpoint para salvar a empresa emissora/consultante relacionada ao thumbprint selecionado.
    /// </remarks>
    /// <param name="request">Dados da empresa vinculada ao certificado.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Perfil persistido apos a atualizacao.</returns>
    [HttpPost("company-profile")]
    [ProducesResponseType(typeof(CompanyCertificateProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompanyCertificateProfileResponseDto>> UpsertCompanyProfile(
        [FromBody] UpsertCompanyCertificateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to upsert company profile for certificate {Thumbprint}", request.Thumbprint);
        var response = await _upsertCompanyCertificateProfileUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(response);
    }
}
