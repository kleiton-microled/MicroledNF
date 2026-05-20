using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microled.Nfe.Service.Api.HealthChecks;

internal static class HealthCheckResponseWriter
{
    public static Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
