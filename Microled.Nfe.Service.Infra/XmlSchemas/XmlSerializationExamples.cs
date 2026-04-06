using System.Text;
using Microled.Nfe.Service.Infra.Interfaces;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Exemplos de uso das classes geradas para serialização XML
/// </summary>
public static class XmlSerializationExamples
{
    /// <summary>
    /// Exemplo de como criar e serializar um PedidoEnvioLoteRPS
    /// </summary>
    public static string ExampleSerializePedidoEnvioLoteRPS(IXmlSerializerService xmlSerializer)
    {
        var pedido = new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = 2,
                CPFCNPJRemetente = new tpCPFCNPJ
                {
                    CNPJ = "12345678000190"
                },
                transacao = true,
                dtInicio = new DateTime(2024, 1, 1),
                dtFim = new DateTime(2024, 1, 31),
                QtdRPS = 1
            },
            RPS = new List<tpRPS>
            {
                new tpRPS
                {
                    Assinatura = new byte[] { 0x01, 0x02 }, // TODO: Assinatura real
                    ChaveRPS = new tpChaveRPS
                    {
                        InscricaoPrestador = 12345678,
                        SerieRPS = "NF",
                        NumeroRPS = 1
                    },
                    TipoRPS = "RPS",
                    DataEmissao = new DateTime(2024, 1, 15),
                    StatusRPS = "N",
                    TributacaoRPS = "T",
                    ValorDeducoes = 0,
                    ValorPIS = 0,
                    ValorCOFINS = 0,
                    ValorINSS = 0,
                    ValorIR = 0,
                    ValorCSLL = 0,
                    CodigoServico = 1401,
                    AliquotaServicos = 5.00m,
                    ISSRetido = false,
                    Discriminacao = "Serviço de exemplo",
                    ValorIPI = 0,
                    ExigibilidadeSuspensa = 0,
                    PagamentoParceladoAntecipado = 0,
                    NBS = "123456789",
                    cLocPrestacao = 3550308, // Código do município de São Paulo (IBGE)
                    IBSCBS = new tpIBSCBS
                    {
                        finNFSe = 0, // NFS-e regular
                        indFinal = 0, // Não é consumo pessoal
                        cIndOp = "000000", // Código indicador da operação (6 dígitos)
                        indDest = 0, // Destinatário é o próprio tomador
                        valores = new tpValores
                        {
                            trib = new tpTrib
                            {
                                gIBSCBS = new tpGIBSCBS
                                {
                                    cClassTrib = "000000" // Código de classificação tributária (6 dígitos)
                                }
                            }
                        }
                    }
                }
            }
        };

        return xmlSerializer.Serialize(pedido);
    }

    /// <summary>
    /// Exemplo de como desserializar um RetornoEnvioLoteRPS
    /// </summary>
    public static RetornoEnvioLoteRPS ExampleDeserializeRetornoEnvioLoteRPS(
        IXmlSerializerService xmlSerializer,
        string xmlContent)
    {
        return xmlSerializer.Deserialize<RetornoEnvioLoteRPS>(xmlContent);
    }

    /// <summary>
    /// Exemplo de XML de RetornoEnvioLoteRPS para testes
    /// </summary>
    public static string ExampleRetornoEnvioLoteRPSXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RetornoEnvioLoteRPS xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
    <Cabecalho Versao=""2"">
        <Sucesso>true</Sucesso>
        <InformacoesLote>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <CPFCNPJRemetente>
                <CNPJ>12345678000190</CNPJ>
            </CPFCNPJRemetente>
            <DataEnvioLote>2024-01-15T10:00:00</DataEnvioLote>
            <QtdNotasProcessadas>1</QtdNotasProcessadas>
            <TempoProcessamento>5000</TempoProcessamento>
            <ValorTotalServicos>1000.00</ValorTotalServicos>
            <ValorTotalDeducoes>0.00</ValorTotalDeducoes>
        </InformacoesLote>
    </Cabecalho>
    <ChaveNFeRPS>
        <ChaveNFe>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <NumeroNFe>1</NumeroNFe>
            <CodigoVerificacao>ABCD1234</CodigoVerificacao>
        </ChaveNFe>
        <ChaveRPS>
            <InscricaoPrestador>12345678</InscricaoPrestador>
            <SerieRPS>NF</SerieRPS>
            <NumeroRPS>1</NumeroRPS>
        </ChaveRPS>
    </ChaveNFeRPS>
</RetornoEnvioLoteRPS>";
    }
}

