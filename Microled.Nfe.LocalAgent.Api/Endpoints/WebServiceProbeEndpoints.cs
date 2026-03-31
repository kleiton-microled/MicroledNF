using Microsoft.AspNetCore.Http.HttpResults;
using Microled.Nfe.LocalAgent.Api.Contracts;
using Microled.Nfe.Service.Infra.Interfaces;

namespace Microled.Nfe.LocalAgent.Api.Endpoints;

public static class WebServiceProbeEndpoints
{
    public static IEndpointRouteBuilder MapWebServiceProbeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/local/webservice/probe", async Task<Ok<WebServiceProbeResponse>> (
            HttpRequest httpRequest,
            IWebServiceProbeService probeService,
            CancellationToken cancellationToken) =>
        {
            WebServiceProbeRequest? request = null;

            if (httpRequest.ContentLength is > 0)
            {
                request = await httpRequest.ReadFromJsonAsync<WebServiceProbeRequest>(cancellationToken);
            }

            var response = await probeService.ProbeAsync(request?.CandidateUrls, cancellationToken);
            return TypedResults.Ok(response);
        })
        .WithTags("Local WebService");

        return endpoints;
    }
}
