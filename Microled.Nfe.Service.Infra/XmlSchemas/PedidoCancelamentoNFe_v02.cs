using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Schema utilizado para PEDIDO de Cancelamento de NFS-e.
/// Origem: PedidoCancelamentoNFe_v02.xsd
/// Elemento raiz: PedidoCancelamentoNFe
/// Namespace: http://www.prefeitura.sp.gov.br/nfe
/// </summary>
[XmlRoot("PedidoCancelamentoNFe", Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false)]
public class PedidoCancelamentoNFe
{
    [XmlElement(Order = 0)]
    public PedidoCancelamentoNFeCabecalho Cabecalho { get; set; } = null!;

    [XmlElement("Detalhe", Order = 1)]
    public List<PedidoCancelamentoNFeDetalhe> Detalhe { get; set; } = new();

    // TODO: Signature element from xmldsig namespace
    // [XmlElement("Signature", Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 2)]
    // public SignatureType? Signature { get; set; }
}

/// <summary>
/// Cabeçalho do pedido de cancelamento de NFS-e.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class PedidoCancelamentoNFeCabecalho
{
    [XmlAttribute("Versao")]
    public long Versao { get; set; }

    [XmlElement(Order = 0)]
    public tpCPFCNPJ CPFCNPJRemetente { get; set; } = null!;

    [XmlElement(Order = 1)]
    public bool transacao { get; set; }
}

/// <summary>
/// Detalhe do pedido de cancelamento de NFS-e. Cada detalhe deverá conter a Chave de uma NFS-e e sua respectiva assinatura de cancelamento.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class PedidoCancelamentoNFeDetalhe
{
    [XmlElement(Order = 0)]
    public tpChaveNFe ChaveNFe { get; set; } = null!;

    [XmlElement(Order = 1)]
    public byte[] AssinaturaCancelamento { get; set; } = null!;
}

