using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Infra.Repositories;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Maps RPS records loaded from Access into the same payload shape consumed by the API/front-end.
/// </summary>
public class AccessRpsPayloadMapper
{
    public SendRpsRequestDto MapToSendRpsRequest(IReadOnlyList<RpsRecord> rpsRecords)
    {
        if (rpsRecords == null || rpsRecords.Count == 0)
        {
            throw new ArgumentException("RPS records list cannot be empty", nameof(rpsRecords));
        }

        var firstRps = rpsRecords[0].Rps;
        var prestador = firstRps.Prestador;

        var prestadorDto = new ServiceProviderDto
        {
            CpfCnpj = prestador.CpfCnpj.GetValue(),
            InscricaoMunicipal = prestador.InscricaoMunicipal,
            RazaoSocial = prestador.RazaoSocial,
            Endereco = prestador.Endereco != null ? MapToAddressDto(prestador.Endereco) : null,
            Email = prestador.Email
        };

        var rpsListDto = rpsRecords.Select(record => MapToRpsDto(record.Rps)).ToList();
        var dates = rpsListDto.Select(r => r.DataEmissao).ToList();

        return new SendRpsRequestDto
        {
            Prestador = prestadorDto,
            RpsList = rpsListDto,
            DataInicio = dates.Min(),
            DataFim = dates.Max(),
            Transacao = true
        };
    }

    private static RpsDto MapToRpsDto(Rps rps)
    {
        return new RpsDto
        {
            InscricaoPrestador = rps.ChaveRPS.InscricaoPrestador,
            SerieRps = rps.ChaveRPS.SerieRps,
            NumeroRps = rps.ChaveRPS.NumeroRps,
            TipoRPS = rps.TipoRPS.ToString().Replace("_", "-"),
            DataEmissao = rps.DataEmissao,
            StatusRPS = ((char)rps.StatusRPS).ToString(),
            TributacaoRPS = ((char)rps.TributacaoRPS).ToString(),
            Item = new RpsItemDto
            {
                CodigoServico = rps.Item.CodigoServico,
                Discriminacao = rps.Item.Discriminacao,
                ValorServicos = rps.Item.ValorServicos.Value,
                ValorDeducoes = rps.Item.ValorDeducoes.Value,
                AliquotaServicos = rps.Item.AliquotaServicos.Value,
                IssRetido = rps.Item.IssRetido == Domain.Enums.IssRetido.Sim
            },
            Tomador = rps.Tomador != null ? new ServiceCustomerDto
            {
                CpfCnpj = rps.Tomador.CpfCnpj?.GetValue(),
                InscricaoMunicipal = rps.Tomador.InscricaoMunicipal,
                InscricaoEstadual = rps.Tomador.InscricaoEstadual,
                RazaoSocial = rps.Tomador.RazaoSocial,
                Endereco = rps.Tomador.Endereco != null ? MapToAddressDto(rps.Tomador.Endereco) : null,
                Email = rps.Tomador.Email
            } : null,
            Tributos = MapToTributosDto(rps.Tributos),
            IbsCbs = MapToIbsCbsDto(rps)
        };
    }

    private static RpsTributosDto? MapToTributosDto(RpsTaxInfo? tributos)
    {
        if (tributos == null)
        {
            return null;
        }

        return new RpsTributosDto
        {
            ValorPIS = tributos.ValorPIS?.Value,
            ValorCOFINS = tributos.ValorCOFINS?.Value,
            ValorINSS = tributos.ValorINSS?.Value,
            ValorIR = tributos.ValorIR?.Value,
            ValorCSLL = tributos.ValorCSLL?.Value,
            ValorIPI = tributos.ValorIPI?.Value,
            ValorCargaTributaria = tributos.ValorCargaTributaria?.Value,
            PercentualCargaTributaria = tributos.PercentualCargaTributaria,
            FonteCargaTributaria = tributos.FonteCargaTributaria,
            ValorTotalRecebido = tributos.ValorTotalRecebido?.Value,
            ValorFinalCobrado = tributos.ValorFinalCobrado?.Value,
            ValorMulta = tributos.ValorMulta?.Value,
            ValorJuros = tributos.ValorJuros?.Value,
            NCM = tributos.NCM
        };
    }

    private static RpsIbsCbsDto? MapToIbsCbsDto(Rps rps)
    {
        var ibsCbs = rps.IbsCbs;
        if (ibsCbs == null && string.IsNullOrWhiteSpace(rps.IbsCbsCClassTrib) && string.IsNullOrWhiteSpace(rps.IbsCbsCIndOp))
        {
            return null;
        }

        var dest = ibsCbs?.Dest != null
            ? MapToIbsCbsPessoaDto(ibsCbs.Dest)
            : MapTomadorToIbsCbsPessoaDto(rps.Tomador);

        return new RpsIbsCbsDto
        {
            FinNFSe = ibsCbs?.FinNfSe,
            IndFinal = ibsCbs?.IndFinal,
            CIndOp = ibsCbs?.CIndOp ?? rps.IbsCbsCIndOp,
            TpOper = ibsCbs?.TpOper,
            RefNfSe = ibsCbs?.RefNfSe.ToList() ?? new List<string>(),
            TpEnteGov = ibsCbs?.TpEnteGov,
            IndDest = ibsCbs?.IndDest,
            Dest = dest,
            CClassTrib = ibsCbs?.CClassTrib ?? rps.IbsCbsCClassTrib,
            CClassTribReg = ibsCbs?.CClassTribReg,
            NBS = ibsCbs?.Nbs,
            CLocPrestacao = ibsCbs?.CLocPrestacao,
            ImovelObra = ibsCbs?.ImovelObra != null
                ? new RpsIbsCbsImovelObraDto
                {
                    InscricaoImobiliariaFiscal = ibsCbs.ImovelObra.InscricaoImobiliariaFiscal,
                    CCib = ibsCbs.ImovelObra.CCib,
                    CObra = ibsCbs.ImovelObra.CObra,
                    Endereco = ibsCbs.ImovelObra.Endereco != null ? MapToAddressDto(ibsCbs.ImovelObra.Endereco) : null
                }
                : null
        };
    }

    private static RpsIbsCbsPessoaDto? MapToIbsCbsPessoaDto(RpsIbsCbsPersonInfo pessoa)
    {
        return new RpsIbsCbsPessoaDto
        {
            CpfCnpj = pessoa.Cnpj ?? pessoa.Cpf,
            Nif = pessoa.Nif,
            NaoNif = pessoa.NaoNif,
            RazaoSocial = pessoa.RazaoSocial,
            Endereco = pessoa.Endereco != null ? MapToAddressDto(pessoa.Endereco) : null,
            Email = pessoa.Email
        };
    }

    private static RpsIbsCbsPessoaDto? MapTomadorToIbsCbsPessoaDto(ServiceCustomer? tomador)
    {
        if (tomador == null)
        {
            return null;
        }

        return new RpsIbsCbsPessoaDto
        {
            CpfCnpj = tomador.CpfCnpj?.GetValue(),
            RazaoSocial = tomador.RazaoSocial ?? string.Empty,
            Endereco = tomador.Endereco != null ? MapToAddressDto(tomador.Endereco) : null,
            Email = tomador.Email
        };
    }

    private static AddressDto MapToAddressDto(Address address)
    {
        return new AddressDto
        {
            TipoLogradouro = address.TipoLogradouro,
            Logradouro = address.Logradouro,
            Numero = address.Numero,
            Complemento = address.Complemento,
            Bairro = address.Bairro,
            CodigoMunicipio = address.CodigoMunicipio,
            UF = address.UF,
            CEP = address.CEP
        };
    }
}
