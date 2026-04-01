using FluentAssertions;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Repositories;
using Microled.Nfe.Service.Infra.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class AccessRpsPayloadMapperTests
{
    [Fact]
    public void MapToSendRpsRequest_ShouldMapTributosAndIbsCbs()
    {
        var mapper = new AccessRpsPayloadMapper();
        var rps = CreateRps();
        var records = new List<RpsRecord>
        {
            new() { Id = 10, Rps = rps }
        };

        var result = mapper.MapToSendRpsRequest(records);

        result.Prestador.CpfCnpj.Should().Be("02126914000129");
        result.RpsList.Should().HaveCount(1);
        result.RpsList[0].Tributos.Should().NotBeNull();
        result.RpsList[0].Tributos!.ValorIR.Should().Be(12.34m);
        result.RpsList[0].IbsCbs.Should().NotBeNull();
        var ibsCbs = result.RpsList[0].IbsCbs!;
        ibsCbs.CClassTrib.Should().Be("000001");
        ibsCbs.CIndOp.Should().Be("100301");
        ibsCbs.Dest!.CpfCnpj.Should().Be("02390435000115");
    }

    private static Rps CreateRps()
    {
        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj("02126914000129"),
            37684280,
            "MICROLED",
            null,
            "ipsilva@microled.com.br");

        var tomador = new ServiceCustomer(
            CpfCnpj.CreateFromCnpj("02390435000115"),
            null,
            633388271114,
            "ECOPORTO SANTOS S/A",
            new Address("Av.", "ENGENHEIRO ALVES FREIRE", "SN", null, "CAIS SABOO", 3548500, "SP", 11010230),
            "administrativoti.op@ecoportosantos.com.br");

        var rps = new Rps(
            new RpsKey(37684280, 1005, "A"),
            TipoRps.RPS,
            new DateOnly(2026, 4, 1),
            StatusRps.Normal,
            TipoTributacao.TributacaoMunicipio,
            new RpsItem(
                2919,
                "SERVICOS DE DESENVOLVIMENTO DE SOFTWARE TESTE",
                Money.Create(1000m),
                Money.Create(0m),
                Aliquota.Create(2.9m),
                IssRetido.Nao,
                TipoTributacao.TributacaoMunicipio),
            prestador,
            tomador);

        rps.SetTributos(new RpsTaxInfo(
            valorIR: Money.Create(12.34m),
            valorFinalCobrado: Money.Create(1000m)));
        rps.SetIbsCbs(new RpsIbsCbsInfo(
            finNfSe: 0,
            indFinal: 1,
            cIndOp: "100301",
            indDest: 1,
            dest: new RpsIbsCbsPersonInfo(
                cpf: null,
                cnpj: "02390435000115",
                nif: null,
                naoNif: null,
                razaoSocial: "ECOPORTO SANTOS S/A",
                endereco: tomador.Endereco,
                email: tomador.Email),
            cClassTrib: "000001"));

        return rps;
    }
}
