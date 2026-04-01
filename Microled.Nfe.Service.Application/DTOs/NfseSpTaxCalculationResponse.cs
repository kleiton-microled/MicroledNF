namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Resultado do cálculo tributário auxiliar NFS-e SP.
/// </summary>
public sealed class NfseSpTaxCalculationResponse
{
    public decimal BaseCalculoIss { get; set; }
    public decimal BaseCalculoFederal { get; set; }

    public decimal ValorIss { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorCsll { get; set; }
    public decimal ValorIr { get; set; }
    public decimal ValorInss { get; set; }

    public decimal TotalRetencoesFederais { get; set; }
    public decimal TotalRetencoes { get; set; }
    public decimal ValorLiquido { get; set; }

    /// <summary>Passos textuais para auditoria / UI.</summary>
    public IReadOnlyList<string> MemoriaCalculo { get; set; } = Array.Empty<string>();
}
