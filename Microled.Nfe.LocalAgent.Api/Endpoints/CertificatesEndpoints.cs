using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microled.Nfe.LocalAgent.Api.Infrastructure;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.LocalAgent.Api.Endpoints;

public static class CertificatesEndpoints
{
    public static IEndpointRouteBuilder MapCertificatesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/local/certificates")
            .WithTags("Local Certificates");

        group.MapGet(string.Empty, async Task<Ok<IReadOnlyList<CertificateListItemDto>>> (
            IListCertificatesUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var response = await useCase.ExecuteAsync(cancellationToken);
            return TypedResults.Ok(response);
        });

        group.MapPost("/select", async Task<Results<Ok<SelectCertificateResponseDto>, ValidationProblem>> (
            SelectCertificateRequestDto request,
            IValidator<SelectCertificateRequestDto> validator,
            ISelectCertificateUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var response = await useCase.ExecuteAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        });

        group.MapGet("/active-profile", async Task<Results<Ok<CompanyCertificateProfileResponseDto>, NotFound>> (
            IGetActiveCertificateProfileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var response = await useCase.ExecuteAsync(cancellationToken);
            return response is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(response);
        });

        group.MapPost("/company-profile", async Task<Results<Ok<CompanyCertificateProfileResponseDto>, ValidationProblem>> (
            UpsertCompanyCertificateProfileRequestDto request,
            IValidator<UpsertCompanyCertificateProfileRequestDto> validator,
            IUpsertCompanyCertificateProfileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var response = await useCase.ExecuteAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        });

        return endpoints;
    }
}
