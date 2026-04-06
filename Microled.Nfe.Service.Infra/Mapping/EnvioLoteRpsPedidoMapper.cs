using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.XmlSchemas;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Infra.Mapping;

/// <summary>
/// Mapeamento compartilhado entre <c>NfeSoapClient</c> (envio) e <c>RpsXmlValidationExportService</c> (arquivos locais).
/// </summary>
public sealed class EnvioLoteRpsPedidoMapper : IEnvioLoteRpsPedidoMapper
{
    private readonly NfeServiceOptions _options;
    private readonly IServiceTaxRateProvider _serviceTaxRateProvider;
    private readonly ICertificateProvider? _certificateProvider;
    private readonly ILogger<EnvioLoteRpsPedidoMapper> _logger;

    public EnvioLoteRpsPedidoMapper(
        IOptions<NfeServiceOptions> options,
        ILogger<EnvioLoteRpsPedidoMapper> logger,
        ICertificateProvider? certificateProvider = null,
        IServiceTaxRateProvider? serviceTaxRateProvider = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _certificateProvider = certificateProvider;
        _serviceTaxRateProvider = serviceTaxRateProvider ?? new ServiceTaxRateProvider();
    }

    public PedidoEnvioLoteRPS MapFromBatch(RpsBatch batch)
    {
        if (batch.RpsList.Count == 0)
        {
            throw new ArgumentException("RPS batch cannot be empty", nameof(batch));
        }

        var primeiroRps = batch.RpsList[0];
        var prestador = primeiroRps.Prestador;

        tpCPFCNPJ cpfCnpjRemetente;
        if (_certificateProvider != null)
        {
            try
            {
                var certificate = _certificateProvider.GetCertificate();
                var cnpjFromCertificate = ExtractCnpjFromCertificate(certificate);
                if (!string.IsNullOrEmpty(cnpjFromCertificate))
                {
                    cpfCnpjRemetente = new tpCPFCNPJ { CNPJ = cnpjFromCertificate };
                    _logger.LogInformation("Using CNPJ from certificate for CPFCNPJRemetente: {CNPJ}", cnpjFromCertificate);

                    var prestadorCnpj = prestador.CpfCnpj.GetValue();
                    if (!string.Equals(cnpjFromCertificate, prestadorCnpj, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "CNPJ mismatch detected: Certificate CNPJ ({CertificateCNPJ}) differs from Prestador CNPJ ({PrestadorCNPJ}). " +
                            "The InscricaoMunicipal ({IM}) must correspond to the Certificate CNPJ. " +
                            "If you get error 1202 (Prestador não encontrado), verify that the IM is registered for the Certificate CNPJ.",
                            cnpjFromCertificate,
                            prestadorCnpj,
                            primeiroRps.ChaveRPS.InscricaoPrestador);
                    }
                }
                else
                {
                    cpfCnpjRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj);
                    _logger.LogWarning("Could not extract CNPJ from certificate, using prestador CNPJ: {CNPJ}", prestador.CpfCnpj.GetValue());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting certificate, using prestador CNPJ for CPFCNPJRemetente");
                cpfCnpjRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj);
            }
        }
        else
        {
            cpfCnpjRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj);
        }

        return new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = _options.GetCabecalhoVersaoNumber(),
                CPFCNPJRemetente = cpfCnpjRemetente,
                transacao = batch.Transacao,
                dtInicio = batch.DataInicio.ToDateTime(TimeOnly.MinValue),
                dtFim = batch.DataFim.ToDateTime(TimeOnly.MinValue),
                QtdRPS = batch.RpsList.Count
            },
            RPS = batch.RpsList.Select(MapRpsToTpRPS).ToList()
        };
    }

    private tpRPS MapRpsToTpRPS(DomainEntities.Rps rps)
    {
        if (string.IsNullOrEmpty(rps.Assinatura))
        {
            throw new InvalidOperationException($"RPS {rps.ChaveRPS.NumeroRps} does not have a signature. It must be signed before sending.");
        }

        var assinaturaBytes = Convert.FromBase64String(rps.Assinatura);

        var versaoSchema = _options.GetVersaoSchemaNumber();
        var tributos = rps.Tributos;
        var ibsCbsInfo = rps.IbsCbs;
        string cClassTrib;
        if (versaoSchema >= 2)
        {
            var cClassTribRaw = ibsCbsInfo?.CClassTrib ?? rps.IbsCbsCClassTrib;
            cClassTrib = IbsCbsCClassTribValidator.ValidateAndGet(cClassTribRaw ?? "000001");

            if (string.IsNullOrWhiteSpace(cClassTribRaw))
            {
                _logger.LogWarning(
                    "IBSCBS_CClassTrib ausente; usando fallback {Fallback}. RPS {InscricaoPrestador}-{NumeroRps}, CodigoServico={CodigoServico}",
                    cClassTrib,
                    rps.ChaveRPS.InscricaoPrestador,
                    rps.ChaveRPS.NumeroRps,
                    rps.Item.CodigoServico);
            }
            else if (!string.Equals(cClassTribRaw, cClassTrib, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "IBSCBS_CClassTrib normalizado. Raw='{Raw}' => Final='{Final}'. RPS {InscricaoPrestador}-{NumeroRps}, CodigoServico={CodigoServico}",
                    cClassTribRaw,
                    cClassTrib,
                    rps.ChaveRPS.InscricaoPrestador,
                    rps.ChaveRPS.NumeroRps,
                    rps.Item.CodigoServico);
            }
        }
        else
        {
            cClassTrib = "0";
        }

        var cIndOp = IbsCbsCIndOpNormalizer.NormalizeOrDefault(ibsCbsInfo?.CIndOp ?? rps.IbsCbsCIndOp);

        var originalAliquota = rps.Item.AliquotaServicos.Value;
        var providerAliquota = _serviceTaxRateProvider.GetAliquota(rps.Item.CodigoServico);
        var aliquotaToSend = providerAliquota != 0m ? providerAliquota : originalAliquota;

        if (aliquotaToSend != originalAliquota)
        {
            _logger.LogInformation(
                "AliquotaServicos adjusted from {From} to {To} for CodigoServico {CodigoServico}",
                originalAliquota,
                aliquotaToSend,
                rps.Item.CodigoServico);
        }

        var omitValorTotalRecebido = rps.Item.CodigoServico == 2919;
        var valorTotalRecebidoBase = tributos?.ValorTotalRecebido?.Value ?? rps.Item.ValorServicos.Value;
        var valorTotalRecebido = omitValorTotalRecebido ? (decimal?)null : valorTotalRecebidoBase;

        _logger.LogInformation(
            "RPS XML fields: CodigoServico={CodigoServico}, AliquotaServicos={AliquotaServicos}, OmitValorTotalRecebido={OmitValorTotalRecebido}",
            rps.Item.CodigoServico,
            aliquotaToSend,
            omitValorTotalRecebido);

        if (versaoSchema >= 2)
        {
            _logger.LogInformation(
                "Applying IBSCBS cClassTrib={cClassTrib}, cIndOp={cIndOp} for RPS {InscricaoPrestador}-{NumeroRps}",
                cClassTrib,
                cIndOp,
                rps.ChaveRPS.InscricaoPrestador,
                rps.ChaveRPS.NumeroRps);
        }

        var cLocPrestacao = CLocPrestacaoResolver.Resolve(
            ibsCbsInfo?.CLocPrestacao,
            rps.Prestador.Endereco?.CodigoMunicipio);

        var valorInicialCobrado = rps.Item.ValorServicos.Value;
        decimal? valorFinalCobrado = null;
        if (versaoSchema < 2)
        {
            // Layout antigo: mantém comportamento anterior.
            valorFinalCobrado = rps.GetValorFinalCobradoParaEnvio();
        }
        else if (tributos?.ValorFinalCobrado?.Value is decimal valorFinalInformado && valorFinalInformado != valorInicialCobrado)
        {
            _logger.LogInformation(
                "ValorFinalCobrado ({ValorFinal}) ignorado no v2; usando ValorInicialCobrado com valor bruto ({ValorInicial}) para RPS {InscricaoPrestador}-{NumeroRps}.",
                valorFinalInformado,
                valorInicialCobrado,
                rps.ChaveRPS.InscricaoPrestador,
                rps.ChaveRPS.NumeroRps);
        }

        var tpRps = new tpRPS
        {
            Assinatura = assinaturaBytes,
            ChaveRPS = new tpChaveRPS
            {
                InscricaoPrestador = rps.ChaveRPS.InscricaoPrestador,
                SerieRPS = rps.ChaveRPS.SerieRps,
                NumeroRPS = rps.ChaveRPS.NumeroRps
            },
            TipoRPS = MapTipoRpsToString(rps.TipoRPS),
            DataEmissao = rps.DataEmissao.ToDateTime(TimeOnly.MinValue),
            StatusRPS = ((char)rps.StatusRPS).ToString(),
            TributacaoRPS = ((char)rps.TributacaoRPS).ToString(),
            ValorDeducoes = rps.Item.ValorDeducoes.Value,
            ValorPIS = tributos?.ValorPIS?.Value ?? 0.00m,
            ValorCOFINS = tributos?.ValorCOFINS?.Value ?? 0.00m,
            ValorINSS = tributos?.ValorINSS?.Value ?? 0.00m,
            ValorIR = tributos?.ValorIR?.Value ?? 0.00m,
            ValorCSLL = tributos?.ValorCSLL?.Value ?? 0.00m,
            CodigoServico = rps.Item.CodigoServico,
            AliquotaServicos = aliquotaToSend,
            ISSRetido = rps.Item.IssRetido == IssRetido.Sim,
            Discriminacao = rps.Item.Discriminacao,
            ValorTotalRecebido = valorTotalRecebido,
            ValorInicialCobrado = valorInicialCobrado,
            ValorFinalCobrado = valorFinalCobrado,
            ValorMulta = tributos?.ValorMulta?.Value,
            ValorJuros = tributos?.ValorJuros?.Value,
            ValorIPI = tributos?.ValorIPI?.Value ?? 0.00m,
            ExigibilidadeSuspensa = 0,
            PagamentoParceladoAntecipado = 0,
            NCM = tributos?.NCM,
            NBS = ibsCbsInfo?.Nbs ?? "123456789",
            cLocPrestacao = cLocPrestacao,
            cPaisPrestacao = null,
            IBSCBS = BuildIbsCbs(rps, cClassTrib, cIndOp)
        };

        tpRps.ValorCargaTributaria = tributos?.ValorCargaTributaria?.Value;
        tpRps.PercentualCargaTributaria = tributos?.PercentualCargaTributaria;
        tpRps.FonteCargaTributaria = tributos?.FonteCargaTributaria;

        var tributacaoIsT = string.Equals(tpRps.TributacaoRPS, "T", StringComparison.OrdinalIgnoreCase);

        if (tributacaoIsT)
        {
            if (tpRps.MunicipioPrestacao.HasValue)
            {
                _logger.LogWarning(
                    "Removido MunicipioPrestacao por TributacaoRPS=T (regra erro 1223). MunicipioPrestacao={MunicipioPrestacao}",
                    tpRps.MunicipioPrestacao);
            }

            tpRps.MunicipioPrestacao = null;
        }

        if (!string.IsNullOrWhiteSpace(tpRps.cPaisPrestacao))
        {
            _logger.LogInformation("Ignoring cPaisPrestacao — system does not support services outside Brazil. Provided='{Provided}'", tpRps.cPaisPrestacao);
            tpRps.cPaisPrestacao = null;
        }

        if (rps.Tomador != null)
        {
            tpRps.CPFCNPJTomador = MapCpfCnpjToTpCPFCNPJNIF(rps.Tomador.CpfCnpj);
            tpRps.InscricaoMunicipalTomador = rps.Tomador.InscricaoMunicipal;
            tpRps.InscricaoEstadualTomador = rps.Tomador.InscricaoEstadual;
            tpRps.RazaoSocialTomador = rps.Tomador.RazaoSocial;
            tpRps.EnderecoTomador = MapAddressToTpEndereco(rps.Tomador.Endereco);
            tpRps.EmailTomador = rps.Tomador.Email;
        }

        return tpRps;
    }

    private static tpCPFCNPJ MapCpfCnpjToTpCPFCNPJ(CpfCnpj cpfCnpj)
    {
        if (cpfCnpj == null)
        {
            throw new ArgumentNullException(nameof(cpfCnpj));
        }

        var value = cpfCnpj.GetValue();
        return cpfCnpj.IsCpf
            ? new tpCPFCNPJ { CPF = value }
            : new tpCPFCNPJ { CNPJ = value };
    }

    private static tpCPFCNPJNIF? MapCpfCnpjToTpCPFCNPJNIF(CpfCnpj? cpfCnpj)
    {
        if (cpfCnpj == null)
        {
            return null;
        }

        var value = cpfCnpj.GetValue();
        return cpfCnpj.IsCpf
            ? new tpCPFCNPJNIF { CPF = value }
            : new tpCPFCNPJNIF { CNPJ = value };
    }

    private static tpEndereco? MapAddressToTpEndereco(Address? address)
    {
        if (address == null)
        {
            return null;
        }

        return new tpEndereco
        {
            TipoLogradouro = address.TipoLogradouro,
            Logradouro = address.Logradouro,
            NumeroEndereco = address.Numero,
            ComplementoEndereco = address.Complemento,
            Bairro = address.Bairro,
            Cidade = address.CodigoMunicipio,
            UF = address.UF,
            CEP = address.CEP
        };
    }

    private static string MapTipoRpsToString(TipoRps tipoRps)
    {
        return tipoRps switch
        {
            TipoRps.RPS => "RPS",
            TipoRps.RPS_M => "RPS-M",
            TipoRps.RPS_C => "RPS-C",
            _ => throw new ArgumentException($"Unknown TipoRps: {tipoRps}")
        };
    }

    private tpIBSCBS BuildIbsCbs(DomainEntities.Rps rps, string cClassTrib, string cIndOp)
    {
        var ibsCbs = rps.IbsCbs;
        if (ibsCbs == null)
        {
            return CreateDefaultIBSCBS(cClassTrib, cIndOp);
        }

        var tpEnteGov = NormalizeTpEnteGov(ibsCbs.TpEnteGov);

        return new tpIBSCBS
        {
            finNFSe = ibsCbs.FinNfSe ?? 0,
            indFinal = ibsCbs.IndFinal ?? 0,
            cIndOp = cIndOp,
            tpOper = ibsCbs.TpOper,
            gRefNFSe = ibsCbs.RefNfSe.Count > 0 ? new tpGRefNFSe { refNFSe = ibsCbs.RefNfSe.ToList() } : null,
            tpEnteGov = tpEnteGov,
            indDest = ibsCbs.IndDest ?? 0,
            dest = MapIbsCbsPessoa(ibsCbs.Dest),
            valores = new tpValores
            {
                trib = new tpTrib
                {
                    gIBSCBS = new tpGIBSCBS
                    {
                        cClassTrib = cClassTrib,
                        gTribRegular = string.IsNullOrWhiteSpace(ibsCbs.CClassTribReg)
                            ? null
                            : new tpGTribRegular
                            {
                                cClassTribReg = IbsCbsCClassTribValidator.ValidateAndGet(ibsCbs.CClassTribReg!)
                            }
                    }
                }
            },
            imovelobra = IbsCbsCIndOpNormalizer.ShouldSerializeImovelObra(cIndOp)
                ? MapIbsCbsImovelObra(ibsCbs.ImovelObra)
                : null
        };
    }

    private static tpIBSCBS CreateDefaultIBSCBS(string cClassTrib, string cIndOp)
    {
        return new tpIBSCBS
        {
            finNFSe = 0,
            indFinal = 0,
            cIndOp = cIndOp,
            indDest = 0,
            valores = new tpValores
            {
                trib = new tpTrib
                {
                    gIBSCBS = new tpGIBSCBS
                    {
                        cClassTrib = cClassTrib
                    }
                }
            }
        };
    }

    private static int? NormalizeTpEnteGov(int? tpEnteGov)
    {
        return tpEnteGov.HasValue && tpEnteGov.Value > 0 ? tpEnteGov : null;
    }

    private static tpInformacoesPessoa? MapIbsCbsPessoa(DomainEntities.RpsIbsCbsPersonInfo? pessoa)
    {
        if (pessoa == null)
        {
            return null;
        }

        return new tpInformacoesPessoa
        {
            CPF = pessoa.Cpf,
            CNPJ = pessoa.Cnpj,
            NIF = pessoa.Nif,
            NaoNIF = pessoa.NaoNif,
            xNome = pessoa.RazaoSocial,
            end = MapIbsCbsEndereco(pessoa.Endereco),
            email = pessoa.Email
        };
    }

    private static tpImovelObra? MapIbsCbsImovelObra(DomainEntities.RpsIbsCbsImovelObraInfo? imovelObra)
    {
        if (imovelObra == null)
        {
            return null;
        }

        return new tpImovelObra
        {
            inscImobFisc = imovelObra.InscricaoImobiliariaFiscal,
            cCIB = imovelObra.CCib,
            cObra = imovelObra.CObra,
            end = MapIbsCbsEnderecoSimples(imovelObra.Endereco)
        };
    }

    private static tpEnderecoIBSCBS? MapIbsCbsEndereco(Address? address)
    {
        if (address == null)
        {
            return null;
        }

        return new tpEnderecoIBSCBS
        {
            endNac = address.CodigoMunicipio.HasValue || address.CEP.HasValue
                ? new tpEnderecoNacional
                {
                    cMun = address.CodigoMunicipio ?? 0,
                    CEP = address.CEP ?? 0
                }
                : null,
            xLgr = address.Logradouro ?? string.Empty,
            nro = address.Numero ?? "S/N",
            xCpl = address.Complemento,
            xBairro = address.Bairro ?? string.Empty
        };
    }

    private static tpEnderecoSimplesIBSCBS? MapIbsCbsEnderecoSimples(Address? address)
    {
        if (address == null)
        {
            return null;
        }

        return new tpEnderecoSimplesIBSCBS
        {
            CEP = address.CEP,
            xLgr = address.Logradouro ?? string.Empty,
            nro = address.Numero ?? "S/N",
            xCpl = address.Complemento,
            xBairro = address.Bairro ?? string.Empty
        };
    }

    private static string? ExtractCnpjFromCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        if (certificate == null)
        {
            return null;
        }

        var subject = certificate.Subject;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var parts = subject.Split(':', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length >= 14)
            {
                var digits = new StringBuilder();
                foreach (var c in trimmed)
                {
                    if (char.IsDigit(c))
                    {
                        digits.Append(c);
                    }
                }

                if (digits.Length == 14)
                {
                    return digits.ToString();
                }
            }
        }

        var cnpjPattern = @"(\d{14})";
        var match = Regex.Match(subject, cnpjPattern);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}
