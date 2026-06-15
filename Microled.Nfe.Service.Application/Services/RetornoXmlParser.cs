using System.Xml.Linq;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.Service.Application.Services;

public sealed class RpsContentFields
{
    public string? TipoRps { get; init; }
    public string? StatusRps { get; init; }
    public string? TributacaoRps { get; init; }
    public string? Discriminacao { get; init; }
    public string? CodigoMunicipio { get; init; }
    public string? ExigibilidadeISS { get; init; }
    public string? MunicipioIncidencia { get; init; }
    public string? TomadorEmail { get; init; }
    public string? TomadorInscricaoMunicipal { get; init; }
    public string? TomadorCep { get; init; }
    public string? TomadorLogradouro { get; init; }
    public string? TomadorNumero { get; init; }
    public string? TomadorComplemento { get; init; }
    public string? TomadorBairro { get; init; }
    public string? TomadorCodigoMunicipio { get; init; }
    public string? TomadorUf { get; init; }
    public string? CodigoServico { get; init; }
    public string? ValorServicos { get; init; }
    public string? AliquotaServicos { get; init; }
    public string? IssRetido { get; init; }
    public string? ValorIss { get; init; }
    public string? ValorDeducoes { get; init; }
    public string? ValorPis { get; init; }
    public string? ValorCofins { get; init; }
    public string? ValorInss { get; init; }
    public string? ValorIr { get; init; }
    public string? ValorCsll { get; init; }
    public string? OutrasRetencoes { get; init; }
    public string? DescontoCondicionado { get; init; }
    public string? DescontoIncondicionado { get; init; }
    public string? IbsIndDest { get; init; }
    public string? IbsCstIbs { get; init; }
    public string? IbsAliqEstadual { get; init; }
    public string? IbsAliqMunicipal { get; init; }
    public string? IbsCstCbs { get; init; }
    public string? IbsAliqCbs { get; init; }
}

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

    public static string? ExtractRazaoSocialTomador(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root is null) return null;

            var ns = root.Name.Namespace;
            return root.Element(ns + "RazaoSocialTomador")?.Value.Trim()
                ?? root.Element("RazaoSocialTomador")?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }

    public static RpsContentFields ExtractRpsContentFields(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new RpsContentFields();
        }

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root is null) return new RpsContentFields();

            var ns = root.Name.Namespace;

            XElement? FindDescendant(XElement el, string localName)
            {
                return el.Descendants(ns + localName).FirstOrDefault()
                    ?? el.Descendants(XName.Get(localName)).FirstOrDefault();
            }

            string? Text(XElement el, string localName) =>
                FindDescendant(el, localName)?.Value.Trim();

            var rpsIdent = FindDescendant(root, "IdentificacaoRps");
            var servico = FindDescendant(root, "Servico");
            var valores = servico is not null ? FindDescendant(servico, "Valores") : null;
            var tomador = FindDescendant(root, "TomadorServico");
            var tomadorEndereco = tomador is not null ? FindDescendant(tomador, "Endereco") : null;
            var ibscbs = valores is not null ? FindDescendant(valores, "IBSCBS") : null;
            var ibsValores = ibscbs is not null ? FindDescendant(ibscbs, "valores") : null;
            var ibsTrib = ibsValores is not null ? FindDescendant(ibsValores, "trib") : null;
            var ibsGibscbs = ibsTrib is not null ? FindDescendant(ibsTrib, "gIBSCBS") : null;

            return new RpsContentFields
            {
                TipoRps = rpsIdent is not null ? Text(rpsIdent, "Tipo") : null,
                StatusRps = Text(root, "Status"),
                TributacaoRps = Text(root, "TributacaoRps"),
                Discriminacao = servico is not null ? Text(servico, "Discriminacao") : null,
                CodigoMunicipio = servico is not null ? Text(servico, "CodigoMunicipio") : null,
                ExigibilidadeISS = servico is not null ? Text(servico, "ExigibilidadeISS") : null,
                MunicipioIncidencia = servico is not null ? Text(servico, "MunicipioIncidencia") : null,
                TomadorEmail = tomador is not null ? Text(tomador, "Email") : null,
                TomadorInscricaoMunicipal = tomador is not null
                    ? (FindDescendant(tomador, "InscricaoMunicipal")?.Value.Trim())
                    : null,
                TomadorCep = tomadorEndereco is not null ? Text(tomadorEndereco, "Cep") : null,
                TomadorLogradouro = tomadorEndereco is not null ? Text(tomadorEndereco, "Endereco") : null,
                TomadorNumero = tomadorEndereco is not null ? Text(tomadorEndereco, "Numero") : null,
                TomadorComplemento = tomadorEndereco is not null ? Text(tomadorEndereco, "Complemento") : null,
                TomadorBairro = tomadorEndereco is not null ? Text(tomadorEndereco, "Bairro") : null,
                TomadorCodigoMunicipio = tomadorEndereco is not null ? Text(tomadorEndereco, "CodigoMunicipio") : null,
                TomadorUf = tomadorEndereco is not null ? Text(tomadorEndereco, "Uf") : null,
                CodigoServico = servico is not null ? Text(servico, "ItemListaServico") : null,
                ValorServicos = valores is not null ? Text(valores, "ValorServicos") : null,
                AliquotaServicos = valores is not null ? Text(valores, "Aliquota") : null,
                IssRetido = servico is not null ? Text(servico, "IssRetido") : null,
                ValorIss = valores is not null ? Text(valores, "ValorIss") : null,
                ValorDeducoes = valores is not null ? Text(valores, "ValorDeducoes") : null,
                ValorPis = valores is not null ? Text(valores, "ValorPis") : null,
                ValorCofins = valores is not null ? Text(valores, "ValorCofins") : null,
                ValorInss = valores is not null ? Text(valores, "ValorInss") : null,
                ValorIr = valores is not null ? Text(valores, "ValorIr") : null,
                ValorCsll = valores is not null ? Text(valores, "ValorCsll") : null,
                OutrasRetencoes = valores is not null ? Text(valores, "OutrasRetencoes") : null,
                DescontoCondicionado = valores is not null ? Text(valores, "DescontoCondicionado") : null,
                DescontoIncondicionado = valores is not null ? Text(valores, "DescontoIncondicionado") : null,
                IbsIndDest = ibscbs is not null ? Text(ibscbs, "indDest") : null,
                IbsCstIbs = ibsGibscbs is not null ? Text(ibsGibscbs, "CST") : null,
                IbsAliqEstadual = ibsValores is not null ? Text(ibsValores, "pAliqEstadual") : null,
                IbsAliqMunicipal = ibsValores is not null ? Text(ibsValores, "pAliqMunicipal") : null,
                IbsCstCbs = ibsGibscbs is not null ? Text(ibsGibscbs, "CSTCbs") : null,
                IbsAliqCbs = ibsValores is not null ? Text(ibsValores, "pAliqCbs") : null,
            };
        }
        catch
        {
            return new RpsContentFields();
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
