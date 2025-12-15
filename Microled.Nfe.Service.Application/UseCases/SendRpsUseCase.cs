using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.ValueObjects;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case implementation for sending RPS batch
/// </summary>
public class SendRpsUseCase : ISendRpsUseCase
{
    private readonly INfeGateway _nfeGateway;
    private readonly IRpsSignatureService _signatureService;
    private readonly ICertificateProvider _certificateProvider;

    public SendRpsUseCase(
        INfeGateway nfeGateway,
        IRpsSignatureService signatureService,
        ICertificateProvider certificateProvider)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
    }

    public async Task<SendRpsResponseDto> ExecuteAsync(SendRpsRequestDto request, CancellationToken cancellationToken)
    {
        // Get certificate for signing
        var certificate = _certificateProvider.GetCertificate();

        // Map DTOs to domain entities and sign each RPS
        var rpsList = request.RpsList.Select(rpsDto =>
        {
            var rps = MapToRps(rpsDto, request.Prestador);
            
            // Generate signature for RPS using SHA1 + RSA
            // Reference: NFe_Web_Service-4.pdf - Campos para assinatura do RPS (versão 2.0)
            var assinatura = _signatureService.SignRps(rps, certificate);
            rps.SetAssinatura(assinatura);
            
            return rps;
        }).ToList();

        var batch = new RpsBatch(rpsList, request.DataInicio, request.DataFim, request.Transacao);

        // Call gateway (which will use the signature service to sign RPS before sending)
        var result = await _nfeGateway.SendRpsBatchAsync(batch, cancellationToken);

        // Map result to response DTO
        return new SendRpsResponseDto
        {
            Sucesso = result.Sucesso,
            Protocolo = result.Protocolo,
            ChavesNFeRPS = result.ChavesNFeRPS.Select(k => new NfeRpsKeyDto
            {
                ChaveNFe = new NfeKeyDto
                {
                    InscricaoPrestador = k.ChaveNFe.InscricaoPrestador,
                    NumeroNFe = k.ChaveNFe.NumeroNFe,
                    CodigoVerificacao = k.ChaveNFe.CodigoVerificacao,
                    ChaveNotaNacional = k.ChaveNFe.ChaveNotaNacional
                },
                ChaveRPS = new RpsKeyDto
                {
                    InscricaoPrestador = k.ChaveRPS.InscricaoPrestador,
                    SerieRps = k.ChaveRPS.SerieRps,
                    NumeroRps = k.ChaveRPS.NumeroRps
                }
            }).ToList(),
            Alertas = result.Alertas.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList(),
            Erros = result.Erros.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList()
        };
    }

    private Rps MapToRps(RpsDto dto, ServiceProviderDto prestadorDto)
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

    private Address MapToAddress(AddressDto dto)
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

