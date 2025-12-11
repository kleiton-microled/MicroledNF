using FluentValidation;
using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Validators;

public class SendRpsRequestDtoValidator : AbstractValidator<SendRpsRequestDto>
{
    public SendRpsRequestDtoValidator()
    {
        RuleFor(x => x.Prestador).NotNull().SetValidator(new ServiceProviderDtoValidator());
        RuleFor(x => x.RpsList).NotEmpty().WithMessage("RPS list cannot be empty");
        RuleFor(x => x.RpsList).Must(x => x.Count <= 50).WithMessage("RPS list cannot contain more than 50 items");
        RuleFor(x => x.DataInicio).NotEmpty();
        RuleFor(x => x.DataFim).NotEmpty();
        RuleFor(x => x.DataFim).GreaterThanOrEqualTo(x => x.DataInicio).WithMessage("DataFim must be greater than or equal to DataInicio");
    }
}

public class ServiceProviderDtoValidator : AbstractValidator<ServiceProviderDto>
{
    public ServiceProviderDtoValidator()
    {
        RuleFor(x => x.CpfCnpj).NotEmpty().WithMessage("CPF/CNPJ is required");
        RuleFor(x => x.InscricaoMunicipal).GreaterThan(0).WithMessage("InscricaoMunicipal must be greater than zero");
        RuleFor(x => x.RazaoSocial).NotEmpty().WithMessage("RazaoSocial is required");
        RuleFor(x => x.RazaoSocial).MaximumLength(75).WithMessage("RazaoSocial cannot exceed 75 characters");
    }
}

public class RpsDtoValidator : AbstractValidator<RpsDto>
{
    public RpsDtoValidator()
    {
        RuleFor(x => x.InscricaoPrestador).GreaterThan(0);
        RuleFor(x => x.NumeroRps).GreaterThan(0);
        RuleFor(x => x.DataEmissao).NotEmpty();
        RuleFor(x => x.Item).NotNull().SetValidator(new RpsItemDtoValidator());
    }
}

public class RpsItemDtoValidator : AbstractValidator<RpsItemDto>
{
    public RpsItemDtoValidator()
    {
        RuleFor(x => x.CodigoServico).GreaterThan(0);
        RuleFor(x => x.Discriminacao).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ValorServicos).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ValorDeducoes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AliquotaServicos).InclusiveBetween(0, 100);
    }
}

