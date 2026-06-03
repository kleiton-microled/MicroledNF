namespace Microled.Nfe.LocalAgent.Api.Configuration;

/// <summary>
/// Default data directories under %ProgramData%\Microled\Nfe\localagent.
/// </summary>
public static class LocalAgentDataPaths
{
    public const string RelativeRoot = "Microled\\Nfe\\localagent";

    public static string BaseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microled",
            "Nfe",
            "localagent");

    public static string RpsOutputDirectory => Path.Combine(BaseDirectory, "RpsOut");

    public static string ValidationOutputDirectory => Path.Combine(BaseDirectory, "Validate");

    public static string LogsDirectory => Path.Combine(BaseDirectory, "logs");

    public static string UserSettingsFile => Path.Combine(BaseDirectory, "settings.json");

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(RpsOutputDirectory);
        Directory.CreateDirectory(ValidationOutputDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
