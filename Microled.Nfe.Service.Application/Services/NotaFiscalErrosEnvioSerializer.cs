using System.Text.Json;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>Grava/le os erros de envio da prefeitura na coluna erros_envio (JSON).</summary>
public static class NotaFiscalErrosEnvioSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string? Serialize(IReadOnlyCollection<NotaFiscalEventoDto>? erros)
    {
        return erros is { Count: > 0 } ? JsonSerializer.Serialize(erros, Options) : null;
    }

    public static IReadOnlyList<NotaFiscalEventoDto> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<NotaFiscalEventoDto>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
