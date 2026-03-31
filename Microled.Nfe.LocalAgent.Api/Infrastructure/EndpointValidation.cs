using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;

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

    private static Dictionary<string, string[]> ToDictionary(ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
    }
}
