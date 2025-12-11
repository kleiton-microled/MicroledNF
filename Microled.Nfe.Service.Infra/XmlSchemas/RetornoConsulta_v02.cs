using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Schema utilizado para RETORNO de pedidos de consulta de NFS-e/RPS.
/// Origem: RetornoConsulta_v02.xsd
/// Elemento raiz: RetornoConsulta
/// Namespace: http://www.prefeitura.sp.gov.br/nfe
/// </summary>
[XmlRoot("RetornoConsulta", Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false)]
public class RetornoConsulta
{
    [XmlElement(Order = 0)]
    public RetornoConsultaCabecalho Cabecalho { get; set; } = null!;

    [XmlElement("Alerta", Order = 1)]
    public List<tpEvento> Alerta { get; set; } = new();

    [XmlElement("Erro", Order = 2)]
    public List<tpEvento> Erro { get; set; } = new();

    [XmlElement("NFe", Order = 3)]
    public List<tpNFe> NFe { get; set; } = new();
}

/// <summary>
/// Cabeçalho do retorno de consulta.
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe")]
public class RetornoConsultaCabecalho
{
    [XmlAttribute("Versao")]
    public long Versao { get; set; }

    [XmlElement(Order = 0)]
    public bool Sucesso { get; set; }
}

