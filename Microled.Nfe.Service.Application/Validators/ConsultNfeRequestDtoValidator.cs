using FluentValidation;
using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Validators;

public class ConsultNfeRequestDtoValidator : AbstractValidator<ConsultNfeRequestDto>
{
    public ConsultNfeRequestDtoValidator()
    {
        RuleFor(x => x)
            .Must(request => request.ChaveNFe != null || request.ChaveRps != null)
            .WithMessage("Informe ChaveNFe ou ChaveRps para consultar a NFe.");
    }
}
