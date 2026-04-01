namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request DTO for downloading XML content as a file.
/// </summary>
public class DownloadXmlRequestDto
{
    public string XmlContent { get; set; } = string.Empty;
    public string? FileName { get; set; }
}
