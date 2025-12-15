using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Schema utilizado para RETORNO de Pedidos de cancelamento de NFS-e.
/// Origem: RetornoCancelamentoNFe_v02.xsd
/// Elemento raiz: RetornoCancelamentoNFe
/// Namespace: http://www.prefeitura.sp.gov.br/nfe
/// </summary>
[XmlRoot("RetornoCancelamentoNFe", Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false)]
public class RetornoCancelamentoNFe
{
    [XmlElement(Order = 0)]
    public RetornoCancelamentoNFeCabecalho Cabecalho { get; set; } = null!;

    [XmlElement("Alerta", Order = 1)]
    public List<tpEvento> Alerta { get; set; } = new();

    [XmlElement("Erro", Order = 2)]
    public List<tpEvento> Erro { get; set; } = new();
}

/// <summary>
/// Cabeçalho do retorno de cancelamento de NFS-e.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class RetornoCancelamentoNFeCabecalho
{
    [XmlAttribute("Versao")]
    public long Versao { get; set; }

    [XmlElement(Order = 0)]
    public bool Sucesso { get; set; }
}

