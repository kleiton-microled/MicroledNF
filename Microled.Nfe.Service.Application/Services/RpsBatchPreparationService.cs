using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.ValueObjects;
using System.Globalization;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>
/// Maps request DTOs to domain entities and signs each RPS using the active certificate.
/// </summary>
public class RpsBatchPreparationService : IRpsBatchPreparationService
{
    private readonly IRpsSignatureService _signatureService;
    private readonly ICertificateProvider _certificateProvider;
    private readonly TimeProvider _timeProvider;

    public RpsBatchPreparationService(
        IRpsSignatureService signatureService,
        ICertificateProvider certificateProvider,
        TimeProvider? timeProvider = null)
    {
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public RpsBatch PrepareSignedBatch(SendRpsRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dataAtual = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var certificate = _certificateProvider.GetCertificate();
        var rpsList = request.RpsList.Select(rpsDto =>
        {
            var rps = MapToRps(rpsDto, request.Prestador, dataAtual);
            var assinatura = _signatureService.SignRps(rps, certificate);
            rps.SetAssinatura(assinatura);
            return rps;
        }).ToList();

        return new RpsBatch(rpsList, request.DataInicio, request.DataFim, request.Transacao);
    }

    private static Rps MapToRps(RpsDto dto, ServiceProviderDto prestadorDto, DateOnly dataAtualParaVencimento)
    {
        var tributosDto = dto.Tributos ?? BuildLegacyTributos(dto);

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
            BuildEnrichedDiscriminacao(dto.Item.Discriminacao, dataAtualParaVencimento, dto.Item.ValorServicos, tributosDto),
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
        rps.SetTributos(MapToTributos(tributosDto));

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

    private static RpsTaxInfo? MapToTributos(RpsTributosDto? tributos)
    {
        if (tributos == null)
        {
            return null;
        }

        return new RpsTaxInfo(
            MapMoney(tributos.ValorPIS),
            MapMoney(tributos.ValorCOFINS),
            MapMoney(tributos.ValorINSS),
            MapMoney(tributos.ValorIR),
            MapMoney(tributos.ValorCSLL),
            MapMoney(tributos.ValorIPI),
            MapMoney(tributos.ValorCargaTributaria),
            tributos.PercentualCargaTributaria,
            tributos.FonteCargaTributaria,
            MapMoney(tributos.ValorTotalRecebido),
            MapMoney(tributos.ValorFinalCobrado),
            MapMoney(tributos.ValorMulta),
            MapMoney(tributos.ValorJuros),
            tributos.NCM);
    }

    private static string BuildEnrichedDiscriminacao(
        string descricaoOriginal,
        DateOnly dataAtualParaVencimento,
        decimal valorServicoBruto,
        RpsTributosDto? tributos)
    {
        var valorPis = tributos?.ValorPIS ?? 0m;
        var valorCofins = tributos?.ValorCOFINS ?? 0m;
        var valorCsll = tributos?.ValorCSLL ?? 0m;
        var valorIr = tributos?.ValorIR ?? 0m;
        var valorInss = tributos?.ValorINSS ?? 0m;

        var totalPisCofinsCsll = valorPis + valorCofins + valorCsll;
        var valorTotalRecebidoInformado = tributos?.ValorTotalRecebido;
        var valorLiquidoCalculado = ShouldUseProvidedValorTotalRecebido(valorTotalRecebidoInformado, valorServicoBruto)
            ? valorTotalRecebidoInformado!.Value
            : (valorServicoBruto - valorPis - valorCofins - valorCsll - valorIr - valorInss);

        var vencimento = dataAtualParaVencimento.AddDays(19);
        var descricaoBase = StripAutoSummaryLines(descricaoOriginal);

        return string.Join(
            "\n",
            descricaoBase,
            $"IRRF: {FormatCurrency(valorIr)}",
            $"PIS/COFINS/CSLL: {FormatCurrency(totalPisCofinsCsll)}",
            $"Valor liquido: {FormatCurrency(valorLiquidoCalculado)}",
            $"Vencimento: {vencimento:dd/MM/yyyy}");
    }

    private static string StripAutoSummaryLines(string descricao)
    {
        var lines = (descricao ?? string.Empty)
            .Split('\n', StringSplitOptions.None)
            .Select(l => l.TrimEnd('\r'))
            .Where(l =>
                !l.StartsWith("IRRF:", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("PIS/COFINS/CSLL:", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("Valor liquido:", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("Valor líquido:", StringComparison.OrdinalIgnoreCase) &&
                !l.StartsWith("Vencimento:", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return string.Join("\n", lines).TrimEnd();
    }

    private static string FormatCurrency(decimal value)
    {
        return $"R${value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}";
    }

    private static bool ShouldUseProvidedValorTotalRecebido(decimal? valorTotalRecebidoInformado, decimal valorServicoBruto)
    {
        if (!valorTotalRecebidoInformado.HasValue)
        {
            return false;
        }

        // Legacy payloads às vezes enviam 0 por default mesmo quando há valor bruto.
        if (valorTotalRecebidoInformado.Value == 0m && valorServicoBruto > 0m)
        {
            return false;
        }

        return true;
    }

    private static RpsTributosDto? BuildLegacyTributos(RpsDto dto)
    {
        var hasLegacyValues =
            dto.ValorPIS.HasValue ||
            dto.ValorCOFINS.HasValue ||
            dto.ValorINSS.HasValue ||
            dto.ValorIR.HasValue ||
            dto.ValorCSLL.HasValue ||
            dto.ValorIPI.HasValue ||
            dto.ValorCargaTributaria.HasValue ||
            dto.PercentualCargaTributaria.HasValue ||
            !string.IsNullOrWhiteSpace(dto.FonteCargaTributaria) ||
            dto.ValorTotalRecebido.HasValue ||
            dto.ValorFinalCobrado.HasValue ||
            dto.ValorMulta.HasValue ||
            dto.ValorJuros.HasValue ||
            !string.IsNullOrWhiteSpace(dto.NCM);

        if (!hasLegacyValues)
        {
            return null;
        }

        return new RpsTributosDto
        {
            ValorPIS = dto.ValorPIS,
            ValorCOFINS = dto.ValorCOFINS,
            ValorINSS = dto.ValorINSS,
            ValorIR = dto.ValorIR,
            ValorCSLL = dto.ValorCSLL,
            ValorIPI = dto.ValorIPI,
            ValorCargaTributaria = dto.ValorCargaTributaria,
            PercentualCargaTributaria = dto.PercentualCargaTributaria,
            FonteCargaTributaria = dto.FonteCargaTributaria,
            ValorTotalRecebido = dto.ValorTotalRecebido,
            ValorFinalCobrado = dto.ValorFinalCobrado,
            ValorMulta = dto.ValorMulta,
            ValorJuros = dto.ValorJuros,
            NCM = dto.NCM
        };
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
