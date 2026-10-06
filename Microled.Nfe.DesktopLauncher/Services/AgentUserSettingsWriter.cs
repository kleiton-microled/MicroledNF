using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microled.Nfe.DesktopLauncher.Services;

public static class JsonFile
{
    public static void WriteAtomic(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    public static bool CanWriteDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probe = Path.Combine(directory, ".write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}

public static class AgentUserSettingsWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    public static void WriteAccessAndApi(string accessDatabasePath, string mainApiBaseUrl)
    {
        LauncherPaths.EnsureDataDirectories();
        var path = LauncherPaths.AgentSettingsFile;
        JsonObject root;
        if (File.Exists(path))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        if (!string.IsNullOrWhiteSpace(accessDatabasePath))
        {
            var access = root["AccessDatabase"] as JsonObject ?? new JsonObject();
            access["DatabasePath"] = Path.GetFullPath(accessDatabasePath.Trim());
            access["RpsTableName"] = "RPS";
            root["AccessDatabase"] = access;
        }

        var integration = root["NfeIntegration"] as JsonObject ?? new JsonObject();
        integration["MainApiBaseUrl"] = mainApiBaseUrl.TrimEnd('/');
        root["NfeIntegration"] = integration;

        var origins = new JsonArray
        {
            "http://localhost:5249",
            "http://127.0.0.1:5249",
            "http://localhost:4200",
            "http://127.0.0.1:4200",
            "https://app.amktechsistemas.com.br"
        };
        var localAgent = root["LocalAgent"] as JsonObject ?? new JsonObject();
        localAgent["Port"] = 5278;
        localAgent["AllowedOrigins"] = origins;
        root["LocalAgent"] = localAgent;

        JsonFile.WriteAtomic(path, JsonSerializer.Serialize(root, JsonOptions));
    }
}
