using FluentValidation;
using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Validators;

/// <summary>
/// Validates the company profile request linked to a certificate.
/// </summary>
public class UpsertCompanyCertificateProfileRequestDtoValidator : AbstractValidator<UpsertCompanyCertificateProfileRequestDto>
{
    public UpsertCompanyCertificateProfileRequestDtoValidator()
    {
        RuleFor(x => x.Thumbprint)
            .NotEmpty()
            .WithMessage("Thumbprint is required.");

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("CompanyName is required.");

        RuleFor(x => x.Cnpj)
            .NotEmpty()
            .WithMessage("Cnpj is required.");

        RuleFor(x => x.MunicipalRegistration)
            .NotEmpty()
            .WithMessage("MunicipalRegistration is required.");
    }
}
