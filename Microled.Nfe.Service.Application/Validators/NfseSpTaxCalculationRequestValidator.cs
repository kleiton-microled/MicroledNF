using FluentValidation;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Enums;

namespace Microled.Nfe.Service.Application.Validators;

public sealed class NfseSpTaxCalculationRequestValidator : AbstractValidator<NfseSpTaxCalculationRequest>
{
    public NfseSpTaxCalculationRequestValidator()
    {
        RuleFor(x => x.ValorServico)
            .GreaterThanOrEqualTo(0)
            .WithMessage("valorServico não pode ser negativo.");

        RuleFor(x => x.ValorServico)
            .GreaterThan(0)
            .WithMessage("valorServico é obrigatório e deve ser maior que zero.");

        RuleFor(x => x.AliquotaIss)
            .InclusiveBetween(0, 1)
            .WithMessage("aliquotaIss deve estar entre 0 e 1 (ex.: 0,05 para 5%).");

        RuleFor(x => x.CodigoServico)
            .GreaterThan(0)
            .WithMessage("codigoServico é obrigatório e deve ser maior que zero.");

        RuleFor(x => x.RegimeTributario)
            .IsInEnum()
            .WithMessage("regimeTributario inválido. Use SimplesNacional, LucroPresumido ou LucroReal.");

        RuleFor(x => x.ValorDeducoes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DescontoIncondicional).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DescontoCondicional).GreaterThanOrEqualTo(0);

        When(
            x => x.RegimeTributario == RegimeTributarioNfseSp.LucroReal,
            () =>
            {
                RuleFor(x => x.AliquotaPis).InclusiveBetween(0, 1);
                RuleFor(x => x.AliquotaCofins).InclusiveBetween(0, 1);
                RuleFor(x => x.AliquotaCsll).InclusiveBetween(0, 1);
                RuleFor(x => x.AliquotaIr).InclusiveBetween(0, 1);
            });

        RuleFor(x => x.AliquotaInss).InclusiveBetween(0, 1);
    }
}
