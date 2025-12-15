using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Schema utilizado para RETORNO de Pedidos de Envio de lote de RPS.
/// Origem: RetornoEnvioLoteRPS_v02.xsd
/// Elemento raiz: RetornoEnvioLoteRPS
/// Namespace: http://www.prefeitura.sp.gov.br/nfe
/// </summary>
[XmlRoot("RetornoEnvioLoteRPS", Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false)]
public class RetornoEnvioLoteRPS
{
    [XmlElement(Order = 0)]
    public RetornoEnvioLoteRPSCabecalho Cabecalho { get; set; } = null!;

    [XmlElement("Alerta", Order = 1)]
    public List<tpEvento> Alerta { get; set; } = new();

    [XmlElement("Erro", Order = 2)]
    public List<tpEvento> Erro { get; set; } = new();

    [XmlElement("ChaveNFeRPS", Order = 3)]
    public List<tpChaveNFeRPS> ChaveNFeRPS { get; set; } = new();
}

/// <summary>
/// Cabeçalho do retorno de envio de lote de RPS.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class RetornoEnvioLoteRPSCabecalho
{
    [XmlAttribute("Versao")]
    public long Versao { get; set; }

    [XmlElement(Order = 0)]
    public bool Sucesso { get; set; }

    [XmlElement(Order = 1, IsNullable = true)]
    public tpInformacoesLote? InformacoesLote { get; set; }
}

