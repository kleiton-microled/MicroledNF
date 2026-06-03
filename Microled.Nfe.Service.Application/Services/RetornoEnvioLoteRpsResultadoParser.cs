using System.Xml.Linq;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>
/// Parses embedded RetornoEnvioLoteRPS XML inside ConsultaSituacaoLote ResultadoOperacao.
/// </summary>
public static class RetornoEnvioLoteRpsResultadoParser
{
    public sealed record RpsEvento(
        string InscricaoPrestador,
        string SerieRps,
        string NumeroRps,
        string Codigo,
        string Descricao,
        bool IsErro);

    public static IReadOnlyList<RpsEvento> ParseRpsEvents(string? resultadoOperacao)
    {
        if (string.IsNullOrWhiteSpace(resultadoOperacao))
        {
            return [];
        }

        try
        {
            var document = XDocument.Parse(resultadoOperacao);
            var events = new List<RpsEvento>();

            foreach (var element in document.Descendants())
            {
                if (element.Name.LocalName is not ("Erro" or "Alerta"))
                {
                    continue;
                }

                var chave = element.Elements().FirstOrDefault(e => e.Name.LocalName == "ChaveRPS");
                if (chave is null)
                {
                    continue;
                }

                var inscricao = ReadChildValue(chave, "InscricaoPrestador");
                var serie = ReadChildValue(chave, "SerieRPS");
                var numero = ReadChildValue(chave, "NumeroRPS");

                if (string.IsNullOrWhiteSpace(inscricao) || string.IsNullOrWhiteSpace(numero))
                {
                    continue;
                }

                events.Add(new RpsEvento(
                    inscricao.Trim(),
                    (serie ?? string.Empty).Trim(),
                    numero.Trim(),
                    ReadChildValue(element, "Codigo") ?? string.Empty,
                    ReadChildValue(element, "Descricao") ?? string.Empty,
                    element.Name.LocalName == "Erro"));
            }

            return events;
        }
        catch
        {
            return [];
        }
    }

    private static string? ReadChildValue(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;
}
