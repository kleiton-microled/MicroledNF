namespace Microled.Nfe.LocalAgent.Api.Configuration;

public static class LocalAgentCors
{
    public const string PolicyName = "LocalAgentCors";

    public static bool IsAllowedOrigin(string? origin, IReadOnlyCollection<string> configuredOrigins)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        if (configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.IsLoopback;
    }

    public static void AddLocalAgentCors(this IServiceCollection services, string[] configuredOrigins)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .SetIsOriginAllowed(origin => IsAllowedOrigin(origin, configuredOrigins))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    public static IApplicationBuilder UseLocalAgentPrivateNetworkCors(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (HttpMethods.IsOptions(context.Request.Method)
                && context.Request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
            {
                context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
            }

            await next();
        });
    }
}
