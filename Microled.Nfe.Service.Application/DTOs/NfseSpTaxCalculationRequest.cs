using Microled.Nfe.Service.Application.Enums;

namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Entrada para cálculo tributário auxiliar (NFS-e São Paulo). Sem emissão de XML nem persistência.
/// </summary>
public sealed class NfseSpTaxCalculationRequest
{
    public decimal ValorServico { get; set; }
    public decimal ValorDeducoes { get; set; }
    public decimal DescontoIncondicional { get; set; }
    public decimal DescontoCondicional { get; set; }

    /// <summary>Ex.: 0,05 para 5%.</summary>
    public decimal AliquotaIss { get; set; }

    public bool IssRetido { get; set; }

    /// <summary>
    /// Lucro Presumido / Simples Nacional: ignorado (regras internas).
    /// Lucro Real: define se há retenção de PIS.
    /// </summary>
    public bool ReterPis { get; set; }

    /// <summary>
    /// Lucro Presumido / Simples Nacional: ignorado.
    /// Lucro Real: alíquota de PIS (0–1). Opcional no sentido fiscal para LP/SN.
    /// </summary>
    public decimal AliquotaPis { get; set; }

    /// <summary>Lucro Presumido / Simples Nacional: ignorado. Lucro Real: retenção COFINS.</summary>
    public bool ReterCofins { get; set; }

    /// <summary>Lucro Presumido / Simples Nacional: ignorado. Lucro Real: alíquota COFINS (0–1).</summary>
    public decimal AliquotaCofins { get; set; }

    /// <summary>Lucro Presumido / Simples Nacional: ignorado. Lucro Real: retenção CSLL.</summary>
    public bool ReterCsll { get; set; }

    /// <summary>Lucro Presumido / Simples Nacional: ignorado. Lucro Real: alíquota CSLL (0–1).</summary>
    public decimal AliquotaCsll { get; set; }

    /// <summary>Lucro Presumido / Simples Nacional: ignorado. Lucro Real: retenção IR.</summary>
    public bool ReterIr { get; set; }

    /// <summary>Lucro Presumido / Simples Nacional: ignorado. Lucro Real: alíquota IR (0–1).</summary>
    public decimal AliquotaIr { get; set; }

    public bool ReterInss { get; set; }
    public decimal AliquotaInss { get; set; }

    public bool ConsiderarDescontoCondicionalNoLiquido { get; set; }
    public bool ArredondarNaCasaFiscal { get; set; }

    public int CodigoServico { get; set; }

    public RegimeTributarioNfseSp RegimeTributario { get; set; }

    /// <summary>
    /// Se true, base federal = valorServico - valorDeducoes - descontoIncondicional;
    /// senão, base federal = valorServico - descontoIncondicional.
    /// </summary>
    public bool BaseFederalSobreValorLiquido { get; set; }
}
