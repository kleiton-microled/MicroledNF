using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Schema utilizado para PEDIDO de consultas de NFS-e.
/// Origem: PedidoConsultaNFe_v02.xsd
/// Elemento raiz: PedidoConsultaNFe
/// Namespace: http://www.prefeitura.sp.gov.br/nfe
/// </summary>
[XmlRoot("PedidoConsultaNFe", Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false)]
public class PedidoConsultaNFe
{
    [XmlElement(Order = 0)]
    public PedidoConsultaNFeCabecalho Cabecalho { get; set; } = null!;

    [XmlElement("Detalhe", Order = 1)]
    public List<PedidoConsultaNFeDetalhe> Detalhe { get; set; } = new();

    // TODO: Signature element from xmldsig namespace
    // [XmlElement("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 2)]
    // public SignatureType? Signature { get; set; }
}

/// <summary>
/// Cabeçalho do pedido de consulta de NFS-e.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class PedidoConsultaNFeCabecalho
{
    [XmlAttribute("Versao")]
    public long Versao { get; set; }

    [XmlElement(Order = 0)]
    public tpCPFCNPJ CPFCNPJRemetente { get; set; } = null!;
}

/// <summary>
/// Detalhe do pedido de consulta. Cada item de detalhe deverá conter a chave de uma NFS-e ou a chave de um RPS.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class PedidoConsultaNFeDetalhe
{
    [XmlElement("ChaveRPS", Order = 0, IsNullable = true)]
    public tpChaveRPS? ChaveRPS { get; set; }

    [XmlElement("ChaveNFe", Order = 1, IsNullable = true)]
    public tpChaveNFe? ChaveNFe { get; set; }
}

