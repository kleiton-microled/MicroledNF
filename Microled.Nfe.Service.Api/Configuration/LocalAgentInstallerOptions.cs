namespace Microled.Nfe.Service.Api.Configuration;

/// <summary>
/// Location of the Local Agent Windows installer served to the frontend.
/// </summary>
public sealed class LocalAgentInstallerOptions
{
    public const string SectionName = "LocalAgentInstaller";

    /// <summary>
    /// Directory containing the setup.exe (absolute or relative to ContentRoot).
    /// </summary>
    public string Directory { get; set; } = "App_Data/installers";

    /// <summary>
    /// Exact file name to serve. When empty, the newest *.exe matching <see cref="FileNamePattern"/> is used.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Glob pattern when <see cref="FileName"/> is not set (default: Microled-NFe-LocalAgent-*.exe).
    /// </summary>
    public string FileNamePattern { get; set; } = "Microled-NFe-LocalAgent-*.exe";

    /// <summary>
    /// Friendly name returned in installer info (for UI).
    /// </summary>
    public string DisplayName { get; set; } = "Microled NFe Local Agent";

    public bool Enabled { get; set; } = true;
}
