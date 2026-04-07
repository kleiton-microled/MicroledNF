using System.Globalization;
using System.Text.RegularExpressions;

namespace Microled.Nfe.Service.Application.NfseSpTax;

/// <summary>
/// Normaliza percentual, valor e texto de carga tributária (ex.: IBPT) para o padrão esperado no XML e na discriminação.
/// </summary>
public static class CargaTributariaNormalization
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Converte o percentual armazenado (vários formatos legados) para fração usada no XML (ex.: 16,45% → 0,1645).
    /// </summary>
    public static decimal? NormalizePercentualToFraction(decimal? percentualInformado)
    {
        if (!percentualInformado.HasValue)
        {
            return null;
        }

        var x = percentualInformado.Value;
        if (x < 0m)
        {
            return x;
        }

        // Já em fração (0–1], ex.: 0,1645
        if (x <= 1m)
        {
            return x;
        }

        // Percentual em pontos (1–100], ex.: 16,45 representando 16,45%
        if (x <= 100m)
        {
            return x / 100m;
        }

        // Legado incorreto: ex. 1645,00 no lugar de 16,45% (100× a mais)
        if (x < 100_000m)
        {
            return x / 10_000m;
        }

        return x / 100m;
    }

    /// <summary>
    /// Reduz o campo de fonte ao código esperado pelo schema (ex.: "IBPT"), removendo textos compostos legados.
    /// </summary>
    public static string? CanonicalFonteCargaTributariaXml(string? fonte)
    {
        if (string.IsNullOrWhiteSpace(fonte))
        {
            return fonte;
        }

        var trimmed = fonte.Trim();

        if (trimmed.Contains("IBPT", StringComparison.OrdinalIgnoreCase))
        {
            return "IBPT";
        }

        return trimmed;
    }

    /// <summary>
    /// Tenta recuperar fração e reforçar o valor quando <paramref name="fonte"/> veio gravado como texto composto (ex.: "1645,00% / IBPT"
    /// ou "R$ 3.757,92 (16,45%) / IBPT").
    /// </summary>
    public static (decimal? ValorCarga, decimal? FracaoPercentual) RepairFromFonteComposta(
        decimal? valorCargaTributaria,
        decimal? percentualBruto,
        string? fonte)
    {
        if (string.IsNullOrWhiteSpace(fonte))
        {
            return (valorCargaTributaria, NormalizePercentualToFraction(percentualBruto));
        }

        var trimmed = fonte.Trim();

        var matchLegadoPercentual = Regex.Match(
            trimmed,
            @"^([\d\.\,]+)\s*%\s*/\s*",
            RegexOptions.CultureInvariant);
        if (matchLegadoPercentual.Success
            && decimal.TryParse(matchLegadoPercentual.Groups[1].Value, NumberStyles.Number, PtBr, out var rawPct))
        {
            var fracao = NormalizePercentualToFraction(rawPct);
            return (valorCargaTributaria, fracao);
        }

        var matchCompleto = Regex.Match(
            trimmed,
            @"R\$\s*([\d\.\,]+)\s*\(([\d\.\,]+)%\)\s*/\s*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (matchCompleto.Success)
        {
            decimal? v = decimal.TryParse(matchCompleto.Groups[1].Value, NumberStyles.Number, PtBr, out var val)
                ? val
                : valorCargaTributaria;
            decimal? frac = decimal.TryParse(matchCompleto.Groups[2].Value, NumberStyles.Number, PtBr, out var pctPts)
                ? NormalizePercentualToFraction(pctPts)
                : NormalizePercentualToFraction(percentualBruto);
            return (v ?? valorCargaTributaria, frac);
        }

        return (valorCargaTributaria, NormalizePercentualToFraction(percentualBruto));
    }

    /// <summary>
    /// Linha para discriminação: "R$ 3.757,92 (16,45%) / IBPT".
    /// </summary>
    public static string FormatIbptDiscriminacaoLine(decimal valorCarga, decimal fracaoPercentual, string fonte = "IBPT")
    {
        var pctExibicao = fracaoPercentual * 100m;
        var valorTxt = valorCarga.ToString("N2", PtBr);
        var pctTxt = pctExibicao.ToString("N2", PtBr);
        return $"R$ {valorTxt} ({pctTxt}%) / {fonte}";
    }
}
