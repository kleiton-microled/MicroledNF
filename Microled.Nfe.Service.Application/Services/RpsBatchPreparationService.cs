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

        return new Rps(chaveRps, tipoRps, dto.DataEmissao, statusRps, tributacaoRps, item, prestador, tomador);
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
}
