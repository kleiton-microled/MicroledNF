using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microled.Nfe.DesktopLauncher.Services;

public sealed class OnPremOptions
{
    public int PostgresPort { get; set; } = 5435;

    public string PostgresUser { get; set; } = "nfe";

    public string PostgresPassword { get; set; } = "nfe-local";

    public string PostgresDatabase { get; set; } = "DB_NFE";

    public string AccessDatabasePath { get; set; } = string.Empty;

    public bool KeepRunningOnClose { get; set; }

    public string ApiUrl { get; set; } = "http://localhost:5249";

    public string AgentUrl { get; set; } = "http://localhost:5278";

    public string ConnectionString =>
        $"Host=127.0.0.1;Port={PostgresPort};Database={PostgresDatabase};Username={PostgresUser};Password={PostgresPassword}";
}

public static class OnPremOptionsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static OnPremOptions Load()
    {
        try
        {
            LauncherPaths.EnsureDataDirectories();
            var path = LauncherPaths.OnPremOptionsFile;
            if (!File.Exists(path))
            {
                var created = new OnPremOptions();
                Save(created);
                return created;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<OnPremOptions>(json, JsonOptions) ?? new OnPremOptions();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new OnPremOptions();
        }
    }

    public static void Save(OnPremOptions options)
    {
        LauncherPaths.EnsureDataDirectories();
        JsonFile.WriteAtomic(
            LauncherPaths.OnPremOptionsFile,
            JsonSerializer.Serialize(options, JsonOptions));
    }
}
