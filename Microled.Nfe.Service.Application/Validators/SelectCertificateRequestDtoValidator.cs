using FluentValidation;
using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Validators;

/// <summary>
/// Validates the certificate selection request.
/// </summary>
public class SelectCertificateRequestDtoValidator : AbstractValidator<SelectCertificateRequestDto>
{
    public SelectCertificateRequestDtoValidator()
    {
        RuleFor(x => x.Thumbprint)
            .NotEmpty()
            .WithMessage("Thumbprint is required.");

        RuleFor(x => x.StoreLocation)
            .NotEmpty()
            .WithMessage("StoreLocation is required.");

        RuleFor(x => x.StoreName)
            .NotEmpty()
            .WithMessage("StoreName is required.");
    }
}
