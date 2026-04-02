using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Microled.Nfe.LocalAgent.Api.Infrastructure;

internal static class EndpointValidation
{
    public static async Task<ValidationProblem?> ValidateAsync<T>(
        T request,
        IValidator<T> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        return validationResult.IsValid
            ? null
            : TypedResults.ValidationProblem(ToDictionary(validationResult));
    }

    public static async Task<ValidationProblem?> ValidateAsync<T>(
        T request,
        IValidator<T> validator,
        ILogger log,
        string operationLabel,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
            return null;

        var detail = string.Join(
            "; ",
            validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
        log.LogWarning("{Operation}: validação falhou — {Detail}", operationLabel, detail);
        return TypedResults.ValidationProblem(ToDictionary(validationResult));
    }

    private static Dictionary<string, string[]> ToDictionary(ValidationResult validationResult)
    {
        var naming = JsonNamingPolicy.CamelCase;
        return validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => naming.ConvertName(group.Key),
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
    }
}
