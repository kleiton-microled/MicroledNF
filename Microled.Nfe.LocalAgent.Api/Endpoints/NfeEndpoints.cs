using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microled.Nfe.LocalAgent.Api.Infrastructure;
using Microled.Nfe.LocalAgent.Api.Services;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.LocalAgent.Api.Endpoints;

public static class NfeEndpoints
{
    public static IEndpointRouteBuilder MapNfeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/local/nfe")
            .WithTags("Local NFe");

        group.MapPost("/consult", async Task<Results<Ok<ConsultNfeResponseDto>, ValidationProblem>> (
            ConsultNfeRequestDto request,
            IValidator<ConsultNfeRequestDto> validator,
            CertificateUnlockService unlockService,
            IConsultNfeUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            await unlockService.UnlockAsync(cancellationToken);
            var response = await useCase.ExecuteAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        });

        group.MapPost("/cancel", async Task<Results<Ok<CancelNfeResponseDto>, ValidationProblem>> (
            CancelNfeRequestDto request,
            IValidator<CancelNfeRequestDto> validator,
            CertificateUnlockService unlockService,
            ICancelNfeUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            await unlockService.UnlockAsync(cancellationToken);
            var response = await useCase.ExecuteAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        });

        return endpoints;
    }
}
