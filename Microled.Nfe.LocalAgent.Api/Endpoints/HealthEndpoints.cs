using System.Reflection;

namespace Microled.Nfe.LocalAgent.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/local/health", () =>
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

            return TypedResults.Ok(new
            {
                status = "ok",
                service = "Microled.Nfe.LocalAgent.Api",
                machineName = Environment.MachineName,
                version
            });
        })
        .WithTags("Local Health")
        .WithName("GetLocalHealth");

        return endpoints;
    }
}
