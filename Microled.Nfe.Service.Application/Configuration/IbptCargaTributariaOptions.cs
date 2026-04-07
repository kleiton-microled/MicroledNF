namespace Microled.Nfe.Service.Application.Configuration;

/// <summary>
/// Carga tributária estimada (IBPT) preenchida no servidor quando o cliente não envia valor, percentual nem fonte.
/// </summary>
public sealed class IbptCargaTributariaOptions
{
    public const string SectionName = "IbptCargaTributaria";

    /// <summary>
    /// Quando true, preenche ValorCargaTributaria, PercentualCargaTributaria e FonteCargaTributaria se todos estiverem ausentes.
    /// </summary>
    public bool PreencherQuandoAusente { get; set; } = true;

    /// <summary>
    /// Quando true, ignora os campos enviados pelo cliente e aplica o padrão configurado.
    /// </summary>
    public bool SobrescreverQuandoInformado { get; set; } = true;

    /// <summary>
    /// Fração entre 0 e 1 (ex.: 0,1645 = 16,45%). Usada como PercentualCargaTributaria no XML e no cálculo do valor.
    /// </summary>
    public decimal PercentualFracaoPadrao { get; set; } = 0.1645m;

    /// <summary>
    /// Valor enviado em FonteCargaTributaria (normalmente IBPT).
    /// </summary>
    public string FontePadrao { get; set; } = "IBPT";

    /// <summary>
    /// Base = ValorServicos - ValorDeducoes (mínimo zero).
    /// </summary>
    public bool UsarBaseServicoMenosDeducoes { get; set; } = true;
}
