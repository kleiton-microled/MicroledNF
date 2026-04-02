namespace Microled.Nfe.Service.Application.NfseSpTax;

/// <summary>
/// Resultado da resolução de regras federais por regime.
/// </summary>
/// <param name="UsesPreset">
/// Se true, usar <see cref="Preset"/> e ignorar retenções/alíquotas PIS/COFINS/CSLL/IR do request.
/// Se false (Lucro Real), usar valores informados no request.
/// </param>
/// <param name="Preset">Valores aplicáveis quando <paramref name="UsesPreset"/> é true.</param>
public sealed record FederalTaxResolution(bool UsesPreset, FederalTaxSnapshot Preset);
