using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Api.Configuration;

namespace Microled.Nfe.Service.Api.Middleware;

public static class LocalAgentProxyExtensions
{
    private static readonly HashSet<string> HopByHop = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailers",
        "Transfer-Encoding",
        "Upgrade",
        "Host"
    };

    public static void MapLocalAgentProxy(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<LocalAgentProxyOptions>>().Value;
        if (!options.Enabled && !app.Environment.IsEnvironment("OnPrem"))
        {
            return;
        }

        app.Map("/api/local/{**path}", async (
            HttpContext context,
            IHttpClientFactory httpClientFactory,
            CancellationToken cancellationToken) =>
        {
            var client = httpClientFactory.CreateClient("LocalAgentProxy");
            var target = context.Request.Path + context.Request.QueryString;
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

            if (context.Request.ContentLength is > 0
                || HttpMethods.IsPost(context.Request.Method)
                || HttpMethods.IsPut(context.Request.Method)
                || HttpMethods.IsPatch(context.Request.Method))
            {
                request.Content = new StreamContent(context.Request.Body);
                if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
                }
            }

            foreach (var header in context.Request.Headers)
            {
                // Content-Type ja foi definido acima; repetir gera "application/json, application/json"
                // e o LocalAgent responde 415. Content-Length e calculado pelo HttpClient.
                if (HopByHop.Contains(header.Key)
                    || string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())
                    && request.Content is not null)
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers.Concat(response.Content.Headers))
            {
                if (HopByHop.Contains(header.Key)
                    || string.Equals(header.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
        });
    }
}
