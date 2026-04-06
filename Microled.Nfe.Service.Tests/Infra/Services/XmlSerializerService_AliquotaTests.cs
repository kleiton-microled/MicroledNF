using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.XmlSchemas;
using Xunit;

namespace Microled.Nfe.Service.Tests.Infra.Services;

public class XmlSerializerService_AliquotaTests
{
    [Fact]
    public void SerializePedidoEnvioLoteRPS_ShouldSerializeAliquotaWith3DecimalsWhenNeeded()
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
                    AliquotaServicos = 0.029m,
                    ISSRetido = false,
                    Discriminacao = "Teste",
                    ValorCargaTributaria = 0m,
                    PercentualCargaTributaria = 0m,
                    FonteCargaTributaria = "0",
                    ValorTotalRecebido = null, // must be omitted from XML for CodigoServico 2919 (erro 1630)
                    ValorInicialCobrado = null,
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
                        valores = new tpValores { trib = new tpTrib { gIBSCBS = new tpGIBSCBS { cClassTrib = "000001" } } }
                    }
                }
            }
        };

        var xml = svc.SerializePedidoEnvioLoteRPS(pedido);
        Assert.Contains("<AliquotaServicos>0.029</AliquotaServicos>", xml);
        Assert.DoesNotContain("ValorTotalRecebido", xml);
        Assert.DoesNotContain("<ValorInicialCobrado>", xml);
        Assert.Contains("<ValorFinalCobrado>10.00</ValorFinalCobrado>", xml);
    }
}


