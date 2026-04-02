namespace Microled.Nfe.Service.Application.NfseSpTax;

/// <summary>
/// Retenções e alíquotas federais (PIS, COFINS, CSLL, IR) aplicadas ao cálculo após resolução por regime.
/// INSS permanece fora deste snapshot (continua vindo do request em todas as versões atuais).
/// </summary>
public sealed record FederalTaxSnapshot(
    bool ReterPis,
    decimal AliquotaPis,
    bool ReterCofins,
    decimal AliquotaCofins,
    bool ReterCsll,
    decimal AliquotaCsll,
    bool ReterIr,
    decimal AliquotaIr)
{
    /// <summary>Placeholder quando o regime usa valores do request (ex.: Lucro Real).</summary>
    public static FederalTaxSnapshot Ignored { get; } = new(false, 0m, false, 0m, false, 0m, false, 0m);
}
