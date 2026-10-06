using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microled.Nfe.LocalAgent.Api.Configuration;

public static class LocalAgentUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    public static JsonObject Load()
    {
        LocalAgentDataPaths.EnsureDirectoriesExist();
        foreach (var path in new[] { LocalAgentDataPaths.LocalUserSettingsFile, LocalAgentDataPaths.UserSettingsFile })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                // try next
            }
        }

        return new JsonObject();
    }

    public static void Save(JsonObject root)
    {
        LocalAgentDataPaths.EnsureDirectoriesExist();
        var path = File.Exists(LocalAgentDataPaths.UserSettingsFile) || CanWrite(LocalAgentDataPaths.UserSettingsFile)
            ? LocalAgentDataPaths.UserSettingsFile
            : LocalAgentDataPaths.LocalUserSettingsFile;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(root, JsonOptions));
    }

    private static bool CanWrite(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void UpsertSection(string sectionName, JsonObject section)
    {
        var root = Load();
        root[sectionName] = section;
        Save(root);
    }
}
