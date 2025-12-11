namespace Microled.Nfe.Service.Domain.Enums;

/// <summary>
/// Tax type for RPS/NFe
/// T = Tributação no município de São Paulo
/// F = Tributação fora do município de São Paulo
/// I = Isento
/// J = ISS Suspenso por Decisão Judicial
/// </summary>
public enum TipoTributacao
{
    /// <summary>
    /// Tributação no município de São Paulo
    /// </summary>
    TributacaoMunicipio = 'T',

    /// <summary>
    /// Tributação fora do município de São Paulo
    /// </summary>
    TributacaoForaMunicipio = 'F',

    /// <summary>
    /// Isento
    /// </summary>
    Isento = 'I',

    /// <summary>
    /// ISS Suspenso por Decisão Judicial
    /// </summary>
    IssSuspensoJudicial = 'J'
}

