using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Classes geradas a partir do XSD TiposNFe_v02.xsd
/// Namespace: http://www.prefeitura.sp.gov.br/nfe/tipos
/// </summary>

/// <summary>
/// Tipo que representa a chave de uma NFS-e e a Chave do RPS que a mesma substitui.
/// Origem: tpChaveNFeRPS (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpChaveNFeRPS
{
    [XmlElement(Order = 0)]
    public tpChaveNFe ChaveNFe { get; set; } = null!;

    [XmlElement(Order = 1)]
    public tpChaveRPS ChaveRPS { get; set; } = null!;
}

/// <summary>
/// Chave de identificação da NFS-e.
/// Origem: tpChaveNFe (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpChaveNFe
{
    [XmlElement(Order = 0)]
    public long InscricaoPrestador { get; set; }

    [XmlElement(Order = 1)]
    public long NumeroNFe { get; set; }

    [XmlElement(Order = 2, IsNullable = true)]
    public string? CodigoVerificacao { get; set; }

    [XmlElement(Order = 3, IsNullable = true)]
    public string? ChaveNotaNacional { get; set; }
}

/// <summary>
/// Tipo que define a chave identificadora de um RPS.
/// Origem: tpChaveRPS (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpChaveRPS
{
    [XmlElement(Order = 0)]
    public long InscricaoPrestador { get; set; }

    [XmlElement(Order = 1, IsNullable = true)]
    public string? SerieRPS { get; set; }

    [XmlElement(Order = 2)]
    public long NumeroRPS { get; set; }
}

/// <summary>
/// Tipo que representa um CPF/CNPJ.
/// Origem: tpCPFCNPJ (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpCPFCNPJ
{
    [XmlElement("CPF", Order = 0, IsNullable = true)]
    public string? CPF { get; set; }

    [XmlElement("CNPJ", Order = 1, IsNullable = true)]
    public string? CNPJ { get; set; }
}

/// <summary>
/// Tipo que representa um CPF/CNPJ/NIF.
/// Origem: tpCPFCNPJNIF (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpCPFCNPJNIF
{
    [XmlElement("CPF", Order = 0, IsNullable = true)]
    public string? CPF { get; set; }

    [XmlElement("CNPJ", Order = 1, IsNullable = true)]
    public string? CNPJ { get; set; }

    [XmlElement("NIF", Order = 2, IsNullable = true)]
    public string? NIF { get; set; }

    [XmlElement("NaoNIF", Order = 3, IsNullable = true)]
    public int? NaoNIF { get; set; }
}

/// <summary>
/// Tipo Endereço.
/// Origem: tpEndereco (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpEndereco
{
    [XmlElement(Order = 0, IsNullable = true)]
    public string? TipoLogradouro { get; set; }

    [XmlElement(Order = 1, IsNullable = true)]
    public string? Logradouro { get; set; }

    [XmlElement(Order = 2, IsNullable = true)]
    public string? NumeroEndereco { get; set; }

    [XmlElement(Order = 3, IsNullable = true)]
    public string? ComplementoEndereco { get; set; }

    [XmlElement(Order = 4, IsNullable = true)]
    public string? Bairro { get; set; }

    [XmlElement(Order = 5, IsNullable = true)]
    public int? Cidade { get; set; }

    [XmlElement(Order = 6, IsNullable = true)]
    public string? UF { get; set; }

    [XmlElement(Order = 7, IsNullable = true)]
    public int? CEP { get; set; }

    [XmlElement(Order = 8, IsNullable = true)]
    public tpEnderecoExterior? EnderecoExterior { get; set; }
}

/// <summary>
/// Tipo endereço no exterior.
/// Origem: tpEnderecoExterior (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpEnderecoExterior
{
    [XmlElement(Order = 0)]
    public string cPais { get; set; } = null!;

    [XmlElement(Order = 1)]
    public string cEndPost { get; set; } = null!;

    [XmlElement(Order = 2)]
    public string xCidade { get; set; } = null!;

    [XmlElement(Order = 3)]
    public string xEstProvReg { get; set; } = null!;
}

/// <summary>
/// Informações do lote processado.
/// Origem: tpInformacoesLote (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpInformacoesLote
{
    [XmlElement(Order = 0, IsNullable = true)]
    public long? NumeroLote { get; set; }

    [XmlElement(Order = 1)]
    public long InscricaoPrestador { get; set; }

    [XmlElement(Order = 2)]
    public tpCPFCNPJ CPFCNPJRemetente { get; set; } = null!;

    [XmlElement(Order = 3)]
    public DateTime DataEnvioLote { get; set; }

    [XmlElement(Order = 4)]
    public long QtdNotasProcessadas { get; set; }

    [XmlElement(Order = 5)]
    public long TempoProcessamento { get; set; }

    [XmlElement(Order = 6)]
    public decimal ValorTotalServicos { get; set; }

    [XmlElement(Order = 7, IsNullable = true)]
    public decimal? ValorTotalDeducoes { get; set; }
}

/// <summary>
/// Tipo que representa a ocorrência de eventos durante o processamento.
/// Origem: tpEvento (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpEvento
{
    [XmlElement(Order = 0)]
    public int Codigo { get; set; }

    [XmlElement(Order = 1, IsNullable = true)]
    public string? Descricao { get; set; }

    [XmlElement("ChaveRPS", Order = 2, IsNullable = true)]
    public tpChaveRPS? ChaveRPS { get; set; }

    [XmlElement("ChaveNFe", Order = 3, IsNullable = true)]
    public tpChaveNFe? ChaveNFe { get; set; }
}

