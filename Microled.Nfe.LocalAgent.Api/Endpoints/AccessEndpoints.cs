using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.LocalAgent.Api.Contracts;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Services;

namespace Microled.Nfe.LocalAgent.Api.Endpoints;

public static class AccessEndpoints
{
    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/local/access")
            .WithTags("Local Access");

        group.MapGet("/settings", (
            IOptionsMonitor<AccessDatabaseOptions> accessOptions) =>
        {
            var path = accessOptions.CurrentValue.DatabasePath ?? string.Empty;
            return TypedResults.Ok(new LocalAccessSettingsResponse
            {
                DatabasePath = path,
                FileExists = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            });
        });

        group.MapPost("/configure", Results<Ok<LocalAccessSettingsResponse>, ValidationProblem> (
            ConfigureAccessRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.DatabasePath))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["databasePath"] = ["Informe o caminho do arquivo Access (.mdb ou .accdb)."]
                });
            }

            var fullPath = Path.GetFullPath(request.DatabasePath.Trim());
            var section = new JsonObject
            {
                ["DatabasePath"] = fullPath
            };
            LocalAgentUserSettingsStore.UpsertSection(AccessDatabaseOptions.SectionName, section);

            return TypedResults.Ok(new LocalAccessSettingsResponse
            {
                DatabasePath = fullPath,
                FileExists = File.Exists(fullPath)
            });
        });

        group.MapGet("/pending-rps", async Task<Ok<LocalAccessPendingRpsResponse>> (
            int? batchSize,
            IOptions<AccessDatabaseOptions> accessOptions,
            IAccessRpsRepository accessRpsRepository,
            AccessRpsPayloadMapper payloadMapper,
            CancellationToken cancellationToken) =>
        {
            var effectiveBatchSize = batchSize.GetValueOrDefault(accessOptions.Value.BatchSize);
            if (effectiveBatchSize <= 0)
            {
                effectiveBatchSize = accessOptions.Value.BatchSize;
            }

            var records = await accessRpsRepository.GetPendingRpsAsync(effectiveBatchSize, cancellationToken);

            var response = new LocalAccessPendingRpsResponse
            {
                Count = records.Count,
                RecordIds = records.Select(x => x.Id).ToList(),
                Request = records.Count > 0 ? payloadMapper.MapToSendRpsRequest(records) : null
            };

            return TypedResults.Ok(response);
        });

        return endpoints;
    }
}
