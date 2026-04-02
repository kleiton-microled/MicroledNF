using Microled.Nfe.Service.Application.Enums;

namespace Microled.Nfe.Service.Application.NfseSpTax;

/// <summary>
/// Constantes e regras federais padrão NFS-e SP (escopo atual: Lucro Presumido e Simples Nacional).
/// </summary>
public sealed class NfseSpFederalTaxRuleProvider : INfseSpFederalTaxRuleProvider
{
    /// <summary>0,65% — Lucro Presumido (retenção).</summary>
    public const decimal AliquotaPisLucroPresumido = 0.0065m;

    /// <summary>3% — Lucro Presumido (retenção).</summary>
    public const decimal AliquotaCofinsLucroPresumido = 0.03m;

    /// <summary>1% — Lucro Presumido (retenção).</summary>
    public const decimal AliquotaCsllLucroPresumido = 0.01m;

    /// <summary>1,5% — Lucro Presumido (retenção).</summary>
    public const decimal AliquotaIrLucroPresumido = 0.015m;

    private static readonly FederalTaxSnapshot LucroPresumidoPreset = new(
        ReterPis: true,
        AliquotaPis: AliquotaPisLucroPresumido,
        ReterCofins: true,
        AliquotaCofins: AliquotaCofinsLucroPresumido,
        ReterCsll: true,
        AliquotaCsll: AliquotaCsllLucroPresumido,
        ReterIr: true,
        AliquotaIr: AliquotaIrLucroPresumido);

    private static readonly FederalTaxSnapshot SimplesNacionalPreset = new(
        ReterPis: false,
        AliquotaPis: 0m,
        ReterCofins: false,
        AliquotaCofins: 0m,
        ReterCsll: false,
        AliquotaCsll: 0m,
        ReterIr: false,
        AliquotaIr: 0m);

    /// <summary>Singleton padrão para hosts que não usam DI explícito.</summary>
    public static NfseSpFederalTaxRuleProvider Default { get; } = new();

    public FederalTaxResolution GetFederalTaxesByRegime(RegimeTributarioNfseSp regime)
    {
        return regime switch
        {
            RegimeTributarioNfseSp.LucroPresumido => new FederalTaxResolution(true, LucroPresumidoPreset),
            RegimeTributarioNfseSp.SimplesNacional => new FederalTaxResolution(true, SimplesNacionalPreset),
            RegimeTributarioNfseSp.LucroReal => new FederalTaxResolution(false, FederalTaxSnapshot.Ignored),
            _ => new FederalTaxResolution(false, FederalTaxSnapshot.Ignored)
        };
    }
}
