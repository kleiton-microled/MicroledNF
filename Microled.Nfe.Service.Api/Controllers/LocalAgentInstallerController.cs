using Microsoft.AspNetCore.Mvc;
using Microled.Nfe.Service.Api.Services;

namespace Microled.Nfe.Service.Api.Controllers;

/// <summary>
/// Serves the Windows Local Agent installer for download from the web app.
/// </summary>
[ApiController]
[Route("api/v1/local-agent")]
public class LocalAgentInstallerController : ControllerBase
{
    private readonly ILocalAgentInstallerService _installerService;
    private readonly ILogger<LocalAgentInstallerController> _logger;

    public LocalAgentInstallerController(
        ILocalAgentInstallerService installerService,
        ILogger<LocalAgentInstallerController> logger)
    {
        _installerService = installerService;
        _logger = logger;
    }

    /// <summary>
    /// Returns metadata about the available installer (file name, size, last modified).
    /// </summary>
    [HttpGet("installer/info")]
    [ProducesResponseType(typeof(LocalAgentInstallerInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<LocalAgentInstallerInfo> GetInstallerInfo()
    {
        var info = _installerService.GetInstallerInfo();
        if (info is null)
        {
            return NotFound(new
            {
                message = "Local Agent installer is not available on this server.",
                hint = "Place the setup.exe in App_Data/installers (see LocalAgentInstaller configuration)."
            });
        }

        return Ok(info);
    }

    /// <summary>
    /// Downloads the Windows installer (.exe).
    /// </summary>
    [HttpGet("installer")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadInstaller()
    {
        var file = _installerService.OpenInstallerStream();
        if (file is null)
        {
            return NotFound(new
            {
                message = "Local Agent installer is not available on this server.",
                hint = "Place the setup.exe in App_Data/installers (see LocalAgentInstaller configuration)."
            });
        }

        var (stream, fileName, length) = file.Value;
        _logger.LogInformation(
            "Serving Local Agent installer download: {FileName} ({SizeBytes} bytes)",
            fileName,
            length);

        return File(
            stream,
            contentType: "application/octet-stream",
            fileDownloadName: fileName,
            enableRangeProcessing: true);
    }
}
