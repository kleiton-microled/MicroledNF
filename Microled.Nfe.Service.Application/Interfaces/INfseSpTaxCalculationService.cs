using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

public interface INfseSpTaxCalculationService
{
    NfseSpTaxCalculationResponse Calculate(NfseSpTaxCalculationRequest request);
}
