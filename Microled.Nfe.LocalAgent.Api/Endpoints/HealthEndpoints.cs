using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
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

        nfseSp.MapGet("/ping", () => TypedResults.Ok(new { ok = true, message = "nfse-sp routes are reachable" }))
            .WithName("GetNfseSpPing");

        nfseSp.MapPost("/calculate-taxes", async Task<Results<Ok<NfseSpTaxCalculationResponse>, ValidationProblem>> (
            NfseSpTaxCalculationRequest request,
            IValidator<NfseSpTaxCalculationRequest> validator,
            INfseSpTaxCalculationService calculationService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var log = loggerFactory.CreateLogger("Microled.Nfe.LocalAgent.NfseSpTax");
            log.LogInformation(
                "POST calculate-taxes: valorServico={ValorServico}, codigoServico={CodigoServico}, regime={Regime}",
                request.ValorServico,
                request.CodigoServico,
                request.RegimeTributario);

            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                log.LogWarning("calculate-taxes: validação falhou.");
                return validationProblem;
            }

            var response = calculationService.Calculate(request);
            log.LogInformation(
                "calculate-taxes: ok. valorLiquido={ValorLiquido}, totalRetencoes={TotalRetencoes}",
                response.ValorLiquido,
                response.TotalRetencoes);
            return TypedResults.Ok(response);
        })
        .WithName("PostNfseSpCalculateTaxes");

        return endpoints;
    }
}
