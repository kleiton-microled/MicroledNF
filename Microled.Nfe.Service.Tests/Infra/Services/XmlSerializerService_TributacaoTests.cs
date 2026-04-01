using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.XmlSchemas;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class XmlSerializerService_TributacaoTests
{
    [Fact]
    public void SerializePedidoEnvioLoteRPS_WhenTributacaoIsT_ShouldNotWriteMunicipioPrestacaoOrCLocPrestacao()
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
                dtInicio = new DateTime(2025, 12, 1),
                dtFim = new DateTime(2025, 12, 1),
                QtdRPS = 1
            },
            RPS = new()
            {
                new tpRPS
                {
                    Assinatura = new byte[] { 1, 2, 3 },
                    ChaveRPS = new tpChaveRPS { InscricaoPrestador = 37684280, SerieRPS = "A", NumeroRPS = 3712 },
                    TipoRPS = "RPS",
                    DataEmissao = new DateTime(2025, 12, 1),
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
                    MunicipioPrestacao = 3550308,
                    ValorTotalRecebido = 10m,
                    ValorInicialCobrado = 10m,
                    ValorFinalCobrado = 10m,
                    ValorMulta = 0m,
                    ValorJuros = 0m,
                    ValorIPI = 0m,
                    ExigibilidadeSuspensa = 0,
                    PagamentoParceladoAntecipado = 0,
                    NBS = "123456789",
                    cLocPrestacao = 3550308,
                    IBSCBS = new tpIBSCBS
                    {
                        finNFSe = 0,
                        indFinal = 0,
                        cIndOp = "100301",
                        indDest = 0,
                        valores = new tpValores { trib = new tpTrib { gIBSCBS = new tpGIBSCBS { cClassTrib = "000001" } } },
                        imovelobra = new tpImovelObra
                        {
                            end = new tpEnderecoSimplesIBSCBS
                            {
                                CEP = 12345678,
                                xLgr = "RUA TESTE",
                                nro = "0",
                                xBairro = "CENTRO"
                            }
                        }
                    }
                }
            }
        };

        var xml = svc.SerializePedidoEnvioLoteRPS(pedido);

        Assert.DoesNotContain("<MunicipioPrestacao>", xml);
        Assert.DoesNotContain("cPaisPrestacao", xml); // must not exist anywhere
        Assert.DoesNotContain("<ValorInicialCobrado>", xml);
        Assert.Contains("<ValorFinalCobrado>10.00</ValorFinalCobrado>", xml);
        Assert.Contains("<cLocPrestacao>", xml);
        Assert.Contains("<IBSCBS>", xml);
        var nbsIdx = xml.IndexOf("<NBS>", StringComparison.Ordinal);
        var locIdx = xml.IndexOf("<cLocPrestacao>", StringComparison.Ordinal);
        var ibsIdx = xml.IndexOf("<IBSCBS>", StringComparison.Ordinal);
        Assert.True(nbsIdx >= 0 && locIdx > nbsIdx && ibsIdx > locIdx, "Expected order: NBS -> cLocPrestacao -> IBSCBS");
        // tpEnderecoSimplesIBSCBS: após CEP vêm xLgr, nro e xBairro (gpEnderecoBaseIBSCBS)
        Assert.Contains("<end><CEP>12345678</CEP>", xml);
        Assert.Contains("<xLgr>RUA TESTE</xLgr>", xml);
        Assert.Contains("<nro>0</nro>", xml);
        Assert.Contains("<xBairro>CENTRO</xBairro>", xml);
        // sanity: tributação T ainda presente
        Assert.Matches(new Regex("<TributacaoRPS>\\s*T\\s*</TributacaoRPS>"), xml);
    }
}


