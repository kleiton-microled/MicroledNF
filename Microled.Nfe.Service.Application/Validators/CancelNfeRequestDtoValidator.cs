using FluentValidation;
using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Validators;

public class CancelNfeRequestDtoValidator : AbstractValidator<CancelNfeRequestDto>
{
    public CancelNfeRequestDtoValidator()
    {
        RuleFor(x => x.ChaveNFe)
            .NotNull()
            .WithMessage("Informe a ChaveNFe para cancelar a nota.");

        When(x => x.ChaveNFe != null, () =>
        {
            RuleFor(x => x.ChaveNFe.InscricaoPrestador)
                .GreaterThan(0)
                .WithMessage("InscricaoPrestador deve ser maior que zero.");

            RuleFor(x => x.ChaveNFe.NumeroNFe)
                .GreaterThan(0)
                .WithMessage("NumeroNFe deve ser maior que zero.");

            RuleFor(x => x.ChaveNFe.CodigoVerificacao)
                .NotEmpty()
                .WithMessage("CodigoVerificacao is required.");
        });
    }
}
