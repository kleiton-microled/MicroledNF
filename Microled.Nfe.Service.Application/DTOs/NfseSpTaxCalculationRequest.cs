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

    public bool ReterPis { get; set; }
    public decimal AliquotaPis { get; set; }

    public bool ReterCofins { get; set; }
    public decimal AliquotaCofins { get; set; }

    public bool ReterCsll { get; set; }
    public decimal AliquotaCsll { get; set; }

    public bool ReterIr { get; set; }
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
