using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microled.Nfe.LocalAgent.Api.Infrastructure;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

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

        var nfseSp = endpoints.MapGroup("/api/local/nfse-sp")
            .WithTags("NFS-e SP tax calculation");

        nfseSp.MapPost("/calculate-taxes", async Task<Results<Ok<NfseSpTaxCalculationResponse>, ValidationProblem>> (
            NfseSpTaxCalculationRequest request,
            IValidator<NfseSpTaxCalculationRequest> validator,
            INfseSpTaxCalculationService calculationService,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
                return validationProblem;

            var response = calculationService.Calculate(request);
            return TypedResults.Ok(response);
        });

        return endpoints;
    }
}
