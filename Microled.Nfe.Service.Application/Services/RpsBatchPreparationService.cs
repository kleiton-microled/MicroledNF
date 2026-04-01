using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.ValueObjects;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>
/// Maps request DTOs to domain entities and signs each RPS using the active certificate.
/// </summary>
public class RpsBatchPreparationService : IRpsBatchPreparationService
{
    private readonly IRpsSignatureService _signatureService;
    private readonly ICertificateProvider _certificateProvider;

    public RpsBatchPreparationService(
        IRpsSignatureService signatureService,
        ICertificateProvider certificateProvider)
    {
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
    }

    public RpsBatch PrepareSignedBatch(SendRpsRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var certificate = _certificateProvider.GetCertificate();
        var rpsList = request.RpsList.Select(rpsDto =>
        {
            var rps = MapToRps(rpsDto, request.Prestador);
            var assinatura = _signatureService.SignRps(rps, certificate);
            rps.SetAssinatura(assinatura);
            return rps;
        }).ToList();

        return new RpsBatch(rpsList, request.DataInicio, request.DataFim, request.Transacao);
    }

    private static Rps MapToRps(RpsDto dto, ServiceProviderDto prestadorDto)
    {
        var prestador = new ServiceProvider(
            prestadorDto.CpfCnpj.Length == 11
                ? CpfCnpj.CreateFromCpf(prestadorDto.CpfCnpj)
                : CpfCnpj.CreateFromCnpj(prestadorDto.CpfCnpj),
            prestadorDto.InscricaoMunicipal,
            prestadorDto.RazaoSocial,
            prestadorDto.Endereco != null ? MapToAddress(prestadorDto.Endereco) : null,
            prestadorDto.Email
        );

        var chaveRps = new RpsKey(dto.InscricaoPrestador, dto.NumeroRps, dto.SerieRps);

        var tipoRps = Enum.Parse<TipoRps>(dto.TipoRPS.Replace("-", "_"));
        var statusRps = (StatusRps)dto.StatusRPS[0];
        var tributacaoRps = (TipoTributacao)dto.TributacaoRPS[0];

        var item = new RpsItem(
            dto.Item.CodigoServico,
            dto.Item.Discriminacao,
            Money.Create(dto.Item.ValorServicos),
            Money.Create(dto.Item.ValorDeducoes),
            Aliquota.Create(dto.Item.AliquotaServicos),
            dto.Item.IssRetido ? IssRetido.Sim : IssRetido.Nao,
            tributacaoRps
        );

        var tomador = dto.Tomador != null ? new ServiceCustomer(
            dto.Tomador.CpfCnpj != null
                ? (dto.Tomador.CpfCnpj.Length == 11
                    ? CpfCnpj.CreateFromCpf(dto.Tomador.CpfCnpj)
                    : CpfCnpj.CreateFromCnpj(dto.Tomador.CpfCnpj))
                : null,
            dto.Tomador.InscricaoMunicipal,
            dto.Tomador.InscricaoEstadual,
            dto.Tomador.RazaoSocial,
            dto.Tomador.Endereco != null ? MapToAddress(dto.Tomador.Endereco) : null,
            dto.Tomador.Email
        ) : null;

        var rps = new Rps(chaveRps, tipoRps, dto.DataEmissao, statusRps, tributacaoRps, item, prestador, tomador);
        rps.SetTributos(MapToTributos(dto.Tributos));

        var ibsCbs = MapToIbsCbs(dto.IbsCbs);
        rps.SetIbsCbs(ibsCbs);

        return rps;
    }

    private static Address MapToAddress(AddressDto dto)
    {
        return new Address(
            dto.TipoLogradouro,
            dto.Logradouro,
            dto.Numero,
            dto.Complemento,
            dto.Bairro,
            dto.CodigoMunicipio,
            dto.UF,
            dto.CEP
        );
    }

    private static RpsTaxInfo? MapToTributos(RpsTributosDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        return new RpsTaxInfo(
            MapMoney(dto.ValorPIS),
            MapMoney(dto.ValorCOFINS),
            MapMoney(dto.ValorINSS),
            MapMoney(dto.ValorIR),
            MapMoney(dto.ValorCSLL),
            MapMoney(dto.ValorIPI),
            MapMoney(dto.ValorCargaTributaria),
            dto.PercentualCargaTributaria,
            dto.FonteCargaTributaria,
            MapMoney(dto.ValorTotalRecebido),
            MapMoney(dto.ValorFinalCobrado),
            MapMoney(dto.ValorMulta),
            MapMoney(dto.ValorJuros),
            dto.NCM);
    }

    private static RpsIbsCbsInfo? MapToIbsCbs(RpsIbsCbsDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        return new RpsIbsCbsInfo(
            dto.FinNFSe,
            dto.IndFinal,
            dto.CIndOp,
            dto.TpOper,
            dto.RefNfSe,
            dto.TpEnteGov,
            dto.IndDest,
            MapToIbsCbsPessoa(dto.Dest),
            dto.CClassTrib,
            dto.CClassTribReg,
            dto.NBS,
            dto.CLocPrestacao,
            MapToIbsCbsImovelObra(dto.ImovelObra));
    }

    private static RpsIbsCbsPersonInfo? MapToIbsCbsPessoa(RpsIbsCbsPessoaDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        var cpf = dto.CpfCnpj?.Length == 11 ? dto.CpfCnpj : null;
        var cnpj = dto.CpfCnpj?.Length == 14 ? dto.CpfCnpj : null;

        return new RpsIbsCbsPersonInfo(
            cpf,
            cnpj,
            dto.Nif,
            dto.NaoNif,
            dto.RazaoSocial,
            dto.Endereco != null ? MapToAddress(dto.Endereco) : null,
            dto.Email);
    }

    private static RpsIbsCbsImovelObraInfo? MapToIbsCbsImovelObra(RpsIbsCbsImovelObraDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        return new RpsIbsCbsImovelObraInfo(
            dto.InscricaoImobiliariaFiscal,
            dto.CCib,
            dto.CObra,
            dto.Endereco != null ? MapToAddress(dto.Endereco) : null);
    }

    private static Money? MapMoney(decimal? value)
    {
        return value.HasValue ? Money.Create(value.Value) : null;
    }
}
