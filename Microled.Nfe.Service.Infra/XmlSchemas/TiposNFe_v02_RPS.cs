using System.Xml.Serialization;

namespace Microled.Nfe.Service.Infra.XmlSchemas;

/// <summary>
/// Tipo que representa um RPS.
/// Origem: tpRPS (TiposNFe_v02.xsd)
/// Namespace: http://www.prefeitura.sp.gov.br/nfe/tipos
/// </summary>
[XmlType(Namespace = "http://www.prefeitura.sp.gov.br/nfe/tipos")]
public class tpRPS
{
    [XmlElement(Order = 0)]
    public byte[] Assinatura { get; set; } = null!;

    [XmlElement(Order = 1)]
    public tpChaveRPS ChaveRPS { get; set; } = null!;

    [XmlElement(Order = 2)]
    public string TipoRPS { get; set; } = null!; // RPS, RPS-M, RPS-C

    [XmlElement(Order = 3, DataType = "date")]
    public DateTime DataEmissao { get; set; }

    [XmlElement(Order = 4)]
    public string StatusRPS { get; set; } = null!; // N, C, E

    [XmlElement(Order = 5)]
    public string TributacaoRPS { get; set; } = null!; // T, F, I, J

    [XmlElement(Order = 6)]
    public decimal ValorDeducoes { get; set; }

    [XmlElement(Order = 7)]
    public decimal ValorPIS { get; set; }

    [XmlElement(Order = 8)]
    public decimal ValorCOFINS { get; set; }

    [XmlElement(Order = 9)]
    public decimal ValorINSS { get; set; }

    [XmlElement(Order = 10)]
    public decimal ValorIR { get; set; }

    [XmlElement(Order = 11)]
    public decimal ValorCSLL { get; set; }

    [XmlElement(Order = 12)]
    public int CodigoServico { get; set; }

    [XmlElement(Order = 13)]
    public decimal AliquotaServicos { get; set; }

    [XmlElement(Order = 14)]
    public bool ISSRetido { get; set; }

    [XmlElement(Order = 15, IsNullable = true)]
    public tpCPFCNPJNIF? CPFCNPJTomador { get; set; }

    [XmlElement(Order = 16, IsNullable = true)]
    public long? InscricaoMunicipalTomador { get; set; }

    [XmlElement(Order = 17, IsNullable = true)]
    public long? InscricaoEstadualTomador { get; set; }

    [XmlElement(Order = 18, IsNullable = true)]
    public string? RazaoSocialTomador { get; set; }

    [XmlElement(Order = 19, IsNullable = true)]
    public tpEndereco? EnderecoTomador { get; set; }

    [XmlElement(Order = 20, IsNullable = true)]
    public string? EmailTomador { get; set; }

    [XmlElement(Order = 21, IsNullable = true)]
    public tpCPFCNPJ? CPFCNPJIntermediario { get; set; }

    [XmlElement(Order = 22, IsNullable = true)]
    public long? InscricaoMunicipalIntermediario { get; set; }

    [XmlElement(Order = 23, IsNullable = true)]
    public string? ISSRetidoIntermediario { get; set; }

    [XmlElement(Order = 24, IsNullable = true)]
    public string? EmailIntermediario { get; set; }

    [XmlElement(Order = 25)]
    public string Discriminacao { get; set; } = null!;

    [XmlElement(Order = 26, IsNullable = true)]
    public decimal? ValorCargaTributaria { get; set; }

    [XmlElement(Order = 27, IsNullable = true)]
    public decimal? PercentualCargaTributaria { get; set; }

    [XmlElement(Order = 28, IsNullable = true)]
    public string? FonteCargaTributaria { get; set; }

    [XmlElement(Order = 29, IsNullable = true)]
    public long? CodigoCEI { get; set; }

    [XmlElement(Order = 30, IsNullable = true)]
    public long? MatriculaObra { get; set; }

    [XmlElement(Order = 31, IsNullable = true)]
    public int? MunicipioPrestacao { get; set; }

    [XmlElement(Order = 32, IsNullable = true)]
    public long? NumeroEncapsulamento { get; set; }

    [XmlElement(Order = 33, IsNullable = true)]
    public decimal? ValorTotalRecebido { get; set; }

    [XmlElement("ValorInicialCobrado", Order = 34, IsNullable = true)]
    public decimal? ValorInicialCobrado { get; set; }

    [XmlElement("ValorFinalCobrado", Order = 35, IsNullable = true)]
    public decimal? ValorFinalCobrado { get; set; }

    [XmlElement(Order = 36, IsNullable = true)]
    public decimal? ValorMulta { get; set; }

    [XmlElement(Order = 37, IsNullable = true)]
    public decimal? ValorJuros { get; set; }

    [XmlElement(Order = 38)]
    public decimal ValorIPI { get; set; }

    [XmlElement(Order = 39)]
    public int ExigibilidadeSuspensa { get; set; } // 0 ou 1

    [XmlElement(Order = 40)]
    public int PagamentoParceladoAntecipado { get; set; } // 0 ou 1

    [XmlElement(Order = 41, IsNullable = true)]
    public string? NCM { get; set; }

    [XmlElement(Order = 42)]
    public string NBS { get; set; } = null!;

    // TODO: tpAtividadeEvento atvEvento (opcional)

    [XmlElement("cLocPrestacao", Order = 43, IsNullable = true)]
    public int? cLocPrestacao { get; set; }

    [XmlElement("cPaisPrestacao", Order = 44, IsNullable = true)]
    public string? cPaisPrestacao { get; set; }

    [XmlElement(Order = 45)]
    public tpIBSCBS IBSCBS { get; set; } = null!;
}

