using System.Text.Json;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.LocalAgent.Api.Services;

public sealed class PendingRpsQueueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public void Save(Guid notaId, SendRpsRequestDto request)
    {
        Directory.CreateDirectory(QueueDirectory);
        var path = GetPath(notaId);
        File.WriteAllText(path, JsonSerializer.Serialize(request, JsonOptions));
    }

    public SendRpsRequestDto? Load(Guid notaId)
    {
        var path = GetPath(notaId);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<SendRpsRequestDto>(File.ReadAllText(path), JsonOptions);
    }

    private static string QueueDirectory => Path.Combine(LocalAgentDataPaths.BaseDirectory, "pending-queue");

    private static string GetPath(Guid notaId) => Path.Combine(QueueDirectory, $"{notaId:N}.json");
}
