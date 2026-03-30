using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Application.UseCases;

internal static class CompanyCertificateProfileMapper
{
    public static CompanyCertificateProfileResponseDto ToResponseDto(CompanyCertificateProfile profile)
    {
        return new CompanyCertificateProfileResponseDto
        {
            Thumbprint = profile.Thumbprint,
            Subject = profile.Subject,
            CompanyName = profile.CompanyName,
            Cnpj = profile.Cnpj,
            MunicipalRegistration = profile.MunicipalRegistration,
            DefaultRemetenteCnpj = profile.DefaultRemetenteCnpj,
            Environment = profile.Environment,
            Notes = profile.Notes,
            IsActive = profile.IsActive
        };
    }
}
