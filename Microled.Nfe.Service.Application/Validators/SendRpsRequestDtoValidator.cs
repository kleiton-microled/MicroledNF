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

public class ConsultBatchStatusRequestDtoValidator : AbstractValidator<ConsultBatchStatusRequestDto>
{
    public ConsultBatchStatusRequestDtoValidator()
    {
        RuleFor(x => x.NumeroProtocolo)
            .NotEmpty().WithMessage("NumeroProtocolo is required")
            .Length(32).WithMessage("NumeroProtocolo must contain exactly 32 characters");

        RuleFor(x => x.CnpjRemetente)
            .NotEmpty().WithMessage("CnpjRemetente is required")
            .Matches(@"^\d{14}$").WithMessage("CnpjRemetente must contain exactly 14 digits");
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
        When(x => x.Tributos != null, () =>
        {
            RuleFor(x => x.Tributos!).SetValidator(new RpsTributosDtoValidator());
        });
        When(x => x.Tributos == null, () =>
        {
            RuleFor(x => x).SetValidator(new RpsLegacyTributosValidator());
        });
        When(x => x.IbsCbs != null, () =>
        {
            RuleFor(x => x.IbsCbs!).SetValidator(new RpsIbsCbsDtoValidator());
        });
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

public class RpsTributosDtoValidator : AbstractValidator<RpsTributosDto>
{
    public RpsTributosDtoValidator()
    {
        RuleFor(x => x.ValorPIS).GreaterThanOrEqualTo(0).When(x => x.ValorPIS.HasValue);
        RuleFor(x => x.ValorCOFINS).GreaterThanOrEqualTo(0).When(x => x.ValorCOFINS.HasValue);
        RuleFor(x => x.ValorINSS).GreaterThanOrEqualTo(0).When(x => x.ValorINSS.HasValue);
        RuleFor(x => x.ValorIR).GreaterThanOrEqualTo(0).When(x => x.ValorIR.HasValue);
        RuleFor(x => x.ValorCSLL).GreaterThanOrEqualTo(0).When(x => x.ValorCSLL.HasValue);
        RuleFor(x => x.ValorIPI).GreaterThanOrEqualTo(0).When(x => x.ValorIPI.HasValue);
        RuleFor(x => x.ValorCargaTributaria).GreaterThanOrEqualTo(0).When(x => x.ValorCargaTributaria.HasValue);
        RuleFor(x => x.PercentualCargaTributaria).GreaterThanOrEqualTo(0).When(x => x.PercentualCargaTributaria.HasValue);
        RuleFor(x => x.ValorTotalRecebido).GreaterThanOrEqualTo(0).When(x => x.ValorTotalRecebido.HasValue);
        RuleFor(x => x.ValorFinalCobrado).GreaterThanOrEqualTo(0).When(x => x.ValorFinalCobrado.HasValue);
        RuleFor(x => x.ValorMulta).GreaterThanOrEqualTo(0).When(x => x.ValorMulta.HasValue);
        RuleFor(x => x.ValorJuros).GreaterThanOrEqualTo(0).When(x => x.ValorJuros.HasValue);
    }
}

public class RpsIbsCbsDtoValidator : AbstractValidator<RpsIbsCbsDto>
{
    public RpsIbsCbsDtoValidator()
    {
        RuleFor(x => x.CIndOp)
            .Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CIndOp))
            .WithMessage("IbsCbs.CIndOp must contain exactly 6 digits.");

        RuleFor(x => x.CClassTrib)
            .Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CClassTrib))
            .WithMessage("IbsCbs.CClassTrib must contain exactly 6 digits.");

        RuleFor(x => x.CClassTribReg)
            .Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CClassTribReg))
            .WithMessage("IbsCbs.CClassTribReg must contain exactly 6 digits.");

        When(x => x.Dest != null, () =>
        {
            RuleFor(x => x.Dest!).SetValidator(new RpsIbsCbsPessoaDtoValidator());
        });

        When(x => x.ImovelObra != null, () =>
        {
            RuleFor(x => x.ImovelObra!).SetValidator(new RpsIbsCbsImovelObraDtoValidator());
        });
    }
}

public class RpsIbsCbsPessoaDtoValidator : AbstractValidator<RpsIbsCbsPessoaDto>
{
    public RpsIbsCbsPessoaDtoValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(75);
        RuleFor(x => x.CpfCnpj)
            .Matches(@"^(\d{11}|\d{14})$")
            .When(x => !string.IsNullOrWhiteSpace(x.CpfCnpj))
            .WithMessage("IbsCbs.Dest.CpfCnpj must contain 11 or 14 digits.");
    }
}

public class RpsIbsCbsImovelObraDtoValidator : AbstractValidator<RpsIbsCbsImovelObraDto>
{
}

public class RpsLegacyTributosValidator : AbstractValidator<RpsDto>
{
    public RpsLegacyTributosValidator()
    {
        RuleFor(x => x.ValorPIS).GreaterThanOrEqualTo(0).When(x => x.ValorPIS.HasValue);
        RuleFor(x => x.ValorCOFINS).GreaterThanOrEqualTo(0).When(x => x.ValorCOFINS.HasValue);
        RuleFor(x => x.ValorINSS).GreaterThanOrEqualTo(0).When(x => x.ValorINSS.HasValue);
        RuleFor(x => x.ValorIR).GreaterThanOrEqualTo(0).When(x => x.ValorIR.HasValue);
        RuleFor(x => x.ValorCSLL).GreaterThanOrEqualTo(0).When(x => x.ValorCSLL.HasValue);
        RuleFor(x => x.ValorIPI).GreaterThanOrEqualTo(0).When(x => x.ValorIPI.HasValue);
        RuleFor(x => x.ValorCargaTributaria).GreaterThanOrEqualTo(0).When(x => x.ValorCargaTributaria.HasValue);
        RuleFor(x => x.PercentualCargaTributaria).GreaterThanOrEqualTo(0).When(x => x.PercentualCargaTributaria.HasValue);
        RuleFor(x => x.ValorTotalRecebido).GreaterThanOrEqualTo(0).When(x => x.ValorTotalRecebido.HasValue);
        RuleFor(x => x.ValorFinalCobrado).GreaterThanOrEqualTo(0).When(x => x.ValorFinalCobrado.HasValue);
        RuleFor(x => x.ValorMulta).GreaterThanOrEqualTo(0).When(x => x.ValorMulta.HasValue);
        RuleFor(x => x.ValorJuros).GreaterThanOrEqualTo(0).When(x => x.ValorJuros.HasValue);
    }
}

