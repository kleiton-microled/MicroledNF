using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Tipo das informações do IBS/CBS.
/// Origem: tpIBSCBS (TiposNFe_v02.xsd)
/// Namespace: http://www.prefeitura.sp.gov.br/nfe/tipos
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpIBSCBS
{
    [XmlElement(Order = 0)]
    public int finNFSe { get; set; } // 0 = NFS-e regular

    [XmlElement(Order = 1)]
    public int indFinal { get; set; } // 0-Não ou 1-Sim

    [XmlElement(Order = 2)]
    public string cIndOp { get; set; } = null!; // Código indicador da operação (6 dígitos)

    [XmlElement(Order = 3, IsNullable = true)]
    public int? tpOper { get; set; } // 1-5 (operação com entes governamentais)

    [XmlElement(Order = 4, IsNullable = true)]
    public tpGRefNFSe? gRefNFSe { get; set; }

    [XmlElement(Order = 5, IsNullable = true)]
    public int? tpEnteGov { get; set; } // 1-4 (ente da compra governamental)

    [XmlElement(Order = 6)]
    public int indDest { get; set; } // 0 ou 1

    [XmlElement(Order = 7, IsNullable = true)]
    public tpInformacoesPessoa? dest { get; set; }

    [XmlElement(Order = 8)]
    public tpValores valores { get; set; } = null!;

    [XmlElement(Order = 9, IsNullable = true)]
    public tpImovelObra? imovelobra { get; set; }
}

/// <summary>
/// Informações relacionadas ao IBS e à CBS.
/// Origem: tpGIBSCBS (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpGIBSCBS
{
    [XmlElement(Order = 0)]
    public string cClassTrib { get; set; } = null!; // 6 dígitos

    [XmlElement(Order = 1, IsNullable = true)]
    public tpGTribRegular? gTribRegular { get; set; }
}

/// <summary>
/// Informações relacionadas à tributação regular.
/// Origem: tpGTribRegular (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpGTribRegular
{
    [XmlElement(Order = 0)]
    public string cClassTribReg { get; set; } = null!; // 6 dígitos
}

/// <summary>
/// Grupo com Ids da nota nacional referenciadas.
/// Origem: tpGRefNFSe (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpGRefNFSe
{
    [XmlElement("refNFSe", Order = 0)]
    public List<string> refNFSe { get; set; } = new(); // Até 99 itens
}

/// <summary>
/// Informações relacionadas aos valores do serviço prestado para IBS e à CBS.
/// Origem: tpValores (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpValores
{
    [XmlElement(Order = 0, IsNullable = true)]
    public tpGrupoReeRepRes? gReeRepRes { get; set; }

    [XmlElement(Order = 1)]
    public tpTrib trib { get; set; } = null!;
}

/// <summary>
/// Informações relacionadas aos tributos IBS e à CBS.
/// Origem: tpTrib (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpTrib
{
    [XmlElement(Order = 0)]
    public tpGIBSCBS gIBSCBS { get; set; } = null!;
}

/// <summary>
/// Grupo de informações relativas a valores incluídos neste documento e recebidos por motivo de estarem relacionadas a operações de terceiros.
/// Origem: tpGrupoReeRepRes (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpGrupoReeRepRes
{
    [XmlElement("documentos", Order = 0)]
    public List<tpDocumento> documentos { get; set; } = new(); // 1-100 itens
}

/// <summary>
/// Tipo de documento referenciado nos casos de reembolso, repasse e ressarcimento.
/// Origem: tpDocumento (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpDocumento
{
    // TODO: Implementar choice entre dFeNacional, docFiscalOutro, docOutro
    [XmlElement(Order = 0, IsNullable = true)]
    public tpFornecedor? fornec { get; set; }

    [XmlElement(Order = 1, DataType = "date")]
    public DateTime dtEmiDoc { get; set; }

    [XmlElement(Order = 2, DataType = "date")]
    public DateTime dtCompDoc { get; set; }

    [XmlElement(Order = 3)]
    public int tpReeRepRes { get; set; } // 01, 02, 03, 04, 99

    [XmlElement(Order = 4, IsNullable = true)]
    public string? xTpReeRepRes { get; set; }

    [XmlElement(Order = 5)]
    public decimal vlrReeRepRes { get; set; }
}

/// <summary>
/// Grupo de informações do fornecedor do documento referenciado.
/// Origem: tpFornecedor (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpFornecedor
{
    [XmlElement("CPF", Order = 0, IsNullable = true)]
    public string? CPF { get; set; }

    [XmlElement("CNPJ", Order = 1, IsNullable = true)]
    public string? CNPJ { get; set; }

    [XmlElement("NIF", Order = 2, IsNullable = true)]
    public string? NIF { get; set; }

    [XmlElement("NaoNIF", Order = 3, IsNullable = true)]
    public int? NaoNIF { get; set; }

    [XmlElement(Order = 4)]
    public string xNome { get; set; } = null!;
}

/// <summary>
/// Tipo de informações de pessoa.
/// Origem: tpInformacoesPessoa (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpInformacoesPessoa
{
    [XmlElement("CPF", Order = 0, IsNullable = true)]
    public string? CPF { get; set; }

    [XmlElement("CNPJ", Order = 1, IsNullable = true)]
    public string? CNPJ { get; set; }

    [XmlElement("NIF", Order = 2, IsNullable = true)]
    public string? NIF { get; set; }

    [XmlElement("NaoNIF", Order = 3, IsNullable = true)]
    public int? NaoNIF { get; set; }

    [XmlElement(Order = 4)]
    public string xNome { get; set; } = null!;

    [XmlElement(Order = 5, IsNullable = true)]
    public tpEnderecoIBSCBS? end { get; set; }

    [XmlElement(Order = 6, IsNullable = true)]
    public string? email { get; set; }
}

/// <summary>
/// Tipo Endereço para o IBSCBS.
/// Origem: tpEnderecoIBSCBS (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpEnderecoIBSCBS
{
    [XmlElement("endNac", Order = 0, IsNullable = true)]
    public tpEnderecoNacional? endNac { get; set; }

    [XmlElement("endExt", Order = 1, IsNullable = true)]
    public tpEnderecoExterior? endExt { get; set; }

    [XmlElement(Order = 2)]
    public string xLgr { get; set; } = null!;

    [XmlElement(Order = 3)]
    public string nro { get; set; } = null!;

    [XmlElement(Order = 4, IsNullable = true)]
    public string? xCpl { get; set; }

    [XmlElement(Order = 5)]
    public string xBairro { get; set; } = null!;
}

/// <summary>
/// Tipo endereço no nacional.
/// Origem: tpEnderecoNacional (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpEnderecoNacional
{
    [XmlElement(Order = 0)]
    public int cMun { get; set; }

    [XmlElement(Order = 1)]
    public int CEP { get; set; }
}

/// <summary>
/// Tipo de imovel/obra.
/// Origem: tpImovelObra (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpImovelObra
{
    [XmlElement(Order = 0, IsNullable = true)]
    public string? inscImobFisc { get; set; }

    [XmlElement("cCIB", Order = 1, IsNullable = true)]
    public string? cCIB { get; set; }

    [XmlElement("cObra", Order = 2, IsNullable = true)]
    public string? cObra { get; set; }

    [XmlElement("end", Order = 3, IsNullable = true)]
    public tpEnderecoSimplesIBSCBS? end { get; set; }
}

/// <summary>
/// Tipo Endereço simplificado para o IBSCBS.
/// Origem: tpEnderecoSimplesIBSCBS (TiposNFe_v02.xsd)
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpEnderecoSimplesIBSCBS
{
    [XmlElement("CEP", Order = 0, IsNullable = true)]
    public int? CEP { get; set; }

    [XmlElement("endExt", Order = 1, IsNullable = true)]
    public tpEnderecoExterior? endExt { get; set; }

    [XmlElement(Order = 2)]
    public string xLgr { get; set; } = null!;

    [XmlElement(Order = 3)]
    public string nro { get; set; } = null!;

    [XmlElement(Order = 4, IsNullable = true)]
    public string? xCpl { get; set; }

    [XmlElement(Order = 5)]
    public string xBairro { get; set; } = null!;
}

