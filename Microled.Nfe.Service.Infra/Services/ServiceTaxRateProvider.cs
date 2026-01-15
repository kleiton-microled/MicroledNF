using Microled.Nfe.Service.Infra.Interfaces;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Simple initial implementation:
/// - CodigoServico 2919 => 0.029 (2,9%)
/// - Otherwise => 0 (caller may keep existing rate if present)
/// </summary>
public sealed class ServiceTaxRateProvider : IServiceTaxRateProvider
{
    public decimal GetAliquota(int codigoServico)
    {
        return codigoServico == 2919 ? 0.029m : 0m;
    }
}


