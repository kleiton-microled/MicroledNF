using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.XmlSchemas;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class XmlSerializerService_DestinatarioTests
{
    [Fact]
    public void SerializePedidoEnvioLoteRPS_WhenDestHasEndereco_ShouldWriteEndAndEmailAfterXNome()
    {
        var xml = Serialize(new tpInformacoesPessoa
        {
            CNPJ = "02390435000115",
            xNome = "ECOPORTO SANTOS S/A",
            end = new tpEnderecoIBSCBS
            {
                endNac = new tpEnderecoNacional { cMun = 3548500, CEP = 11010230 },
                xLgr = "ENGENHEIRO ALVES FREIRE",
                nro = "S/N",
                xBairro = "CAIS SABOO"
            },
            email = "administrativoti.op@ecoportosantos.com.br"
        });

        Assert.Contains(
            "<dest><CNPJ>02390435000115</CNPJ><xNome>ECOPORTO SANTOS S/A</xNome>" +
            "<end><endNac><cMun>3548500</cMun><CEP>11010230</CEP></endNac>" +
            "<xLgr>ENGENHEIRO ALVES FREIRE</xLgr><nro>S/N</nro><xBairro>CAIS SABOO</xBairro></end>" +
            "<email>administrativoti.op@ecoportosantos.com.br</email></dest>",
            xml);
    }

    [Fact]
    public void SerializePedidoEnvioLoteRPS_WhenDestEnderecoHasNoMunicipio_ShouldOmitEnd()
    {
        var xml = Serialize(new tpInformacoesPessoa
        {
            CNPJ = "02390435000115",
            xNome = "ECOPORTO SANTOS S/A",
            end = new tpEnderecoIBSCBS
            {
                endNac = new tpEnderecoNacional { cMun = 0, CEP = 0 },
                xLgr = "",
                nro = "",
                xBairro = ""
            }
        });

        Assert.Contains("<dest><CNPJ>02390435000115</CNPJ><xNome>ECOPORTO SANTOS S/A</xNome></dest>", xml);
    }

    private static string Serialize(tpInformacoesPessoa dest)
    {
        var options = Options.Create(new NfeServiceOptions
        {
            Versao = "2",
            EnableXmlSignature = false,
            UseSchemaV2Fields = true
        });
        var svc = new XmlSerializerService(NullLogger<XmlSerializerService>.Instance, options);

        var pedido = new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = 2,
                CPFCNPJRemetente = new tpCPFCNPJ { CNPJ = "02126914000129" },
                transacao = true,
                dtInicio = new DateTime(2026, 10, 6),
                dtFim = new DateTime(2026, 10, 6),
                QtdRPS = 1
            },
            RPS = new()
            {
                new tpRPS
                {
                    Assinatura = new byte[] { 1, 2, 3 },
                    ChaveRPS = new tpChaveRPS { InscricaoPrestador = 37684280, SerieRPS = "A", NumeroRPS = 2549 },
                    TipoRPS = "RPS",
                    DataEmissao = new DateTime(2026, 10, 6),
                    StatusRPS = "N",
                    TributacaoRPS = "T",
                    ValorDeducoes = 0m,
                    ValorPIS = 0m,
                    ValorCOFINS = 0m,
                    ValorINSS = 0m,
                    ValorIR = 0m,
                    ValorCSLL = 0m,
                    CodigoServico = 2919,
                    AliquotaServicos = 0.05m,
                    ISSRetido = false,
                    Discriminacao = "Teste",
                    ValorCargaTributaria = 0m,
                    PercentualCargaTributaria = 0m,
                    FonteCargaTributaria = "0",
                    ValorTotalRecebido = 10m,
                    ValorFinalCobrado = 10m,
                    ValorMulta = 0m,
                    ValorJuros = 0m,
                    ValorIPI = 0m,
                    ExigibilidadeSuspensa = 0,
                    PagamentoParceladoAntecipado = 0,
                    NBS = "115022000",
                    cLocPrestacao = 3550308,
                    IBSCBS = new tpIBSCBS
                    {
                        finNFSe = 0,
                        indFinal = 0,
                        cIndOp = "100301",
                        indDest = 1,
                        dest = dest,
                        valores = new tpValores { trib = new tpTrib { gIBSCBS = new tpGIBSCBS { cClassTrib = "000001" } } }
                    }
                }
            }
        };

        return svc.SerializePedidoEnvioLoteRPS(pedido);
    }
}
