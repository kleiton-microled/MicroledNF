namespace Microled.Nfe.Service.Infra.Interfaces;

/// <summary>
/// Centralized provider for service tax rates (AliquotaServicos) based on CodigoServico.
/// </summary>
public interface IServiceTaxRateProvider
{
    decimal GetAliquota(int codigoServico);
}


