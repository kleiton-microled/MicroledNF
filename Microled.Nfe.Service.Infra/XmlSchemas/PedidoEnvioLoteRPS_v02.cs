using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Schema utilizado para PEDIDO de envio de lote de RPS.
/// Origem: PedidoEnvioLoteRPS_v02.xsd
/// Elemento raiz: PedidoEnvioLoteRPS
/// Namespace: http://www.prefeitura.sp.gov.br/nfe
/// </summary>
[XmlRoot("PedidoEnvioLoteRPS", Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false)]
public class PedidoEnvioLoteRPS
{
    [XmlElement(Order = 0)]
    public PedidoEnvioLoteRPSCabecalho Cabecalho { get; set; } = null!;

    [XmlElement("RPS", Order = 1)]
    public List<tpRPS> RPS { get; set; } = new();

    // TODO: Signature element from xmldsig namespace
    // [XmlElement("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 2)]
    // public SignatureType? Signature { get; set; }
}

/// <summary>
/// Cabeçalho do pedido de envio de lote de RPS.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class PedidoEnvioLoteRPSCabecalho
{
    [XmlAttribute("Versao")]
    public long Versao { get; set; }

    [XmlElement(Order = 0)]
    public tpCPFCNPJ CPFCNPJRemetente { get; set; } = null!;

    [XmlElement(Order = 1, IsNullable = true)]
    public bool? transacao { get; set; }

    [XmlElement(Order = 2, DataType = "date")]
    public DateTime dtInicio { get; set; }

    [XmlElement(Order = 3, DataType = "date")]
    public DateTime dtFim { get; set; }

    [XmlElement(Order = 4)]
    public long QtdRPS { get; set; }
}

