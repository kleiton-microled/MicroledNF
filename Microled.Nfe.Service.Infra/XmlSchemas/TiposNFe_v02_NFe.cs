using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Tipo que representa uma NFS-e.
/// Origem: tpNFe (TiposNFe_v02.xsd)
/// Namespace: http://www.prefeitura.sp.gov.br/nfe/tipos
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpNFe
{
    [XmlElement(Order = 0, IsNullable = true)]
    public byte[]? Assinatura { get; set; }

    [XmlElement(Order = 1)]
    public tpChaveNFe ChaveNFe { get; set; } = null!;

    [XmlElement(Order = 2)]
    public DateTime DataEmissaoNFe { get; set; }

    [XmlElement(Order = 3, IsNullable = true)]
    public long? NumeroLote { get; set; }

    [XmlElement(Order = 4, IsNullable = true)]
    public tpChaveRPS? ChaveRPS { get; set; }

    [XmlElement(Order = 5, IsNullable = true)]
    public string? TipoRPS { get; set; }

    [XmlElement(Order = 6, DataType = "date", IsNullable = true)]
    public DateTime? DataEmissaoRPS { get; set; }

    [XmlElement(Order = 7)]
    public DateTime DataFatoGeradorNFe { get; set; }

    [XmlElement(Order = 8)]
    public tpCPFCNPJ CPFCNPJPrestador { get; set; } = null!;

    [XmlElement(Order = 9)]
    public string RazaoSocialPrestador { get; set; } = null!;

    [XmlElement(Order = 10)]
    public tpEndereco EnderecoPrestador { get; set; } = null!;

    [XmlElement(Order = 11, IsNullable = true)]
    public string? EmailPrestador { get; set; }

    [XmlElement(Order = 12)]
    public string StatusNFe { get; set; } = null!; // N, C, E

    [XmlElement(Order = 13, IsNullable = true)]
    public DateTime? DataCancelamento { get; set; }

    [XmlElement(Order = 14)]
    public string TributacaoNFe { get; set; } = null!;

    [XmlElement(Order = 15)]
    public string OpcaoSimples { get; set; } = null!;

    [XmlElement(Order = 16, IsNullable = true)]
    public long? NumeroGuia { get; set; }

    [XmlElement(Order = 17, DataType = "date", IsNullable = true)]
    public DateTime? DataQuitacaoGuia { get; set; }

    [XmlElement(Order = 18)]
    public decimal ValorServicos { get; set; }

    [XmlElement(Order = 19, IsNullable = true)]
    public decimal? ValorDeducoes { get; set; }

    [XmlElement(Order = 20, IsNullable = true)]
    public decimal? ValorPIS { get; set; }

    [XmlElement(Order = 21, IsNullable = true)]
    public decimal? ValorCOFINS { get; set; }

    [XmlElement(Order = 22, IsNullable = true)]
    public decimal? ValorINSS { get; set; }

    [XmlElement(Order = 23, IsNullable = true)]
    public decimal? ValorIR { get; set; }

    [XmlElement(Order = 24, IsNullable = true)]
    public decimal? ValorCSLL { get; set; }

    [XmlElement(Order = 25)]
    public int CodigoServico { get; set; }

    [XmlElement(Order = 26)]
    public decimal AliquotaServicos { get; set; }

    [XmlElement(Order = 27)]
    public decimal ValorISS { get; set; }

    [XmlElement(Order = 28)]
    public decimal ValorCredito { get; set; }

    [XmlElement(Order = 29)]
    public bool ISSRetido { get; set; }

    [XmlElement(Order = 30, IsNullable = true)]
    public tpCPFCNPJNIF? CPFCNPJTomador { get; set; }

    [XmlElement(Order = 31, IsNullable = true)]
    public long? InscricaoMunicipalTomador { get; set; }

    [XmlElement(Order = 32, IsNullable = true)]
    public long? InscricaoEstadualTomador { get; set; }

    [XmlElement(Order = 33, IsNullable = true)]
    public string? RazaoSocialTomador { get; set; }

    [XmlElement(Order = 34, IsNullable = true)]
    public tpEndereco? EnderecoTomador { get; set; }

    [XmlElement(Order = 35, IsNullable = true)]
    public string? EmailTomador { get; set; }

    [XmlElement(Order = 36, IsNullable = true)]
    public tpCPFCNPJ? CPFCNPJIntermediario { get; set; }

    [XmlElement(Order = 37, IsNullable = true)]
    public long? InscricaoMunicipalIntermediario { get; set; }

    [XmlElement(Order = 38, IsNullable = true)]
    public string? ISSRetidoIntermediario { get; set; }

    [XmlElement(Order = 39, IsNullable = true)]
    public string? EmailIntermediario { get; set; }

    [XmlElement(Order = 40)]
    public string Discriminacao { get; set; } = null!;

    // TODO: Campos adicionais opcionais (ValorCargaTributaria, etc.)
    // TODO: IBSCBS e RetornoComplementarIBSCBS
}

