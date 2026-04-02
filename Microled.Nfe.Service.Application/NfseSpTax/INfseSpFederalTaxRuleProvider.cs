using Microled.Nfe.Service.Application.Enums;

namespace Microled.Nfe.Service.Application.NfseSpTax;

/// <summary>
/// Fornece retenções e alíquotas federais conforme regime tributário.
/// Evolui para regras por código de serviço, tomador ou município sem alterar o serviço de cálculo.
/// </summary>
public interface INfseSpFederalTaxRuleProvider
{
    /// <summary>
    /// Retorna retenções e alíquotas federais (PIS/COFINS/CSLL/IR) para o regime informado.
    /// Evolução futura: sobrecargas com código de serviço, tomador ou município.
    /// </summary>
    FederalTaxResolution GetFederalTaxesByRegime(RegimeTributarioNfseSp regime);
}
