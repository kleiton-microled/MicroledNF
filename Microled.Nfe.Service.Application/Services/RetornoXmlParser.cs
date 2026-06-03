using System.Xml.Linq;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.Service.Application.Services;

public static class RetornoXmlParser
{
    public static (IReadOnlyList<NotaFiscalEventoDto> Erros, IReadOnlyList<NotaFiscalEventoDto> Alertas) Parse(
        string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return ([], []);
        }

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root is null)
            {
                return ([], []);
            }

            var erros = ExtractEventos(root, "Erro");
            var alertas = ExtractEventos(root, "Alerta");
            return (erros, alertas);
        }
        catch
        {
            return ([], []);
        }
    }

    private static IReadOnlyList<NotaFiscalEventoDto> ExtractEventos(XElement root, string elementName)
    {
        // Elements may be in the root namespace or explicitly in the empty namespace (xmlns="").
        // Search both to handle all prefeitura response variants.
        var ns = root.Name.Namespace;

        var elements = root.Elements(ns + elementName).Concat(root.Elements(XName.Get(elementName)));

        return elements
            .Select(e => new NotaFiscalEventoDto
            {
                Codigo = e.Element("Codigo")?.Value.Trim() ?? string.Empty,
                Descricao = e.Element("Descricao")?.Value.Trim() ?? string.Empty
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.Codigo) || !string.IsNullOrWhiteSpace(e.Descricao))
            .ToList();
    }
}
