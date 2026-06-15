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
            var nfe = ResolveNfeElement(doc);
            var searchRoot = nfe ?? doc.Root;
            return searchRoot is null ? null : GetChildText(searchRoot, "RazaoSocialTomador");
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
            var nfe = ResolveNfeElement(doc);
            if (nfe is not null)
            {
                return ExtractFromSpNfe(nfe);
            }

            var root = doc.Root;
            if (root is null)
            {
                return new RpsContentFields();
            }

            return ExtractFromAbrasfRps(root);
        }
        catch
        {
            return new RpsContentFields();
        }
    }

    private static RpsContentFields ExtractFromSpNfe(XElement nfe)
    {
        var enderecoTomador = GetChildElement(nfe, "EnderecoTomador");
        var ibscbs = GetChildElement(nfe, "IBSCBS");
        var retornoIbs = GetChildElement(nfe, "RetornoComplementarIBSCBS");
        var ibsValores = ibscbs is not null ? GetChildElement(ibscbs, "valores") : null;
        var ibsTrib = ibsValores is not null ? GetChildElement(ibsValores, "trib") : null;
        var ibsGibscbs = ibsTrib is not null ? GetChildElement(ibsTrib, "gIBSCBS") : null;

        var tipoLogradouro = GetChildText(enderecoTomador, "TipoLogradouro");
        var logradouro = GetChildText(enderecoTomador, "Logradouro");
        var logradouroCompleto = string.Join(
            " ",
            new[] { tipoLogradouro, logradouro }.Where(static s => !string.IsNullOrWhiteSpace(s))).Trim();

        if (string.IsNullOrWhiteSpace(logradouroCompleto))
        {
            logradouroCompleto = logradouro;
        }

        return new RpsContentFields
        {
            TipoRps = GetChildText(nfe, "TipoRPS"),
            StatusRps = GetChildText(nfe, "StatusNFe"),
            TributacaoRps = GetChildText(nfe, "TributacaoNFe"),
            Discriminacao = GetChildText(nfe, "Discriminacao"),
            TomadorEmail = GetChildText(nfe, "EmailTomador"),
            TomadorInscricaoMunicipal = GetChildText(nfe, "InscricaoMunicipalTomador"),
            TomadorCep = GetChildText(enderecoTomador, "CEP"),
            TomadorLogradouro = logradouroCompleto,
            TomadorNumero = GetChildText(enderecoTomador, "NumeroEndereco"),
            TomadorComplemento = GetChildText(enderecoTomador, "ComplementoEndereco"),
            TomadorBairro = GetChildText(enderecoTomador, "Bairro"),
            TomadorCodigoMunicipio = GetChildText(enderecoTomador, "Cidade"),
            TomadorUf = GetChildText(enderecoTomador, "UF"),
            CodigoServico = GetChildText(nfe, "CodigoServico"),
            ValorServicos = GetChildText(nfe, "ValorServicos"),
            AliquotaServicos = GetChildText(nfe, "AliquotaServicos"),
            IssRetido = GetChildText(nfe, "ISSRetido"),
            ValorIss = GetChildText(nfe, "ValorISS"),
            ValorDeducoes = GetChildText(nfe, "ValorDeducoes"),
            ValorPis = GetChildText(nfe, "ValorPIS"),
            ValorCofins = GetChildText(nfe, "ValorCOFINS"),
            ValorInss = GetChildText(nfe, "ValorINSS"),
            ValorIr = GetChildText(nfe, "ValorIR"),
            ValorCsll = GetChildText(nfe, "ValorCSLL"),
            IbsIndDest = GetChildText(ibscbs, "indDest"),
            IbsCstIbs = GetChildText(ibsGibscbs, "cClassTrib"),
            IbsAliqEstadual = FirstNonEmpty(
                GetChildText(retornoIbs, "ValorAliqEfetivaEstadualIBS"),
                GetChildText(retornoIbs, "ValorAliqEstadualIBS")),
            IbsAliqMunicipal = FirstNonEmpty(
                GetChildText(retornoIbs, "ValorAliqEfetivaMunicipalIBS"),
                GetChildText(retornoIbs, "ValorAliqMunicipalIBS")),
            IbsAliqCbs = FirstNonEmpty(
                GetChildText(retornoIbs, "ValorAliqEfetivaCBS"),
                GetChildText(retornoIbs, "ValorAliqCBS")),
        };
    }

    private static RpsContentFields ExtractFromAbrasfRps(XElement root)
    {
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
                ? FindDescendant(tomador, "InscricaoMunicipal")?.Value.Trim()
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

    private static XElement? ResolveNfeElement(XDocument doc)
    {
        return doc.Descendants(XName.Get("NFe", "http://www.prefeitura.sp.gov.br/nfe")).FirstOrDefault()
            ?? doc.Descendants(XName.Get("NFe", "")).FirstOrDefault()
            ?? doc.Descendants("NFe").FirstOrDefault()
            ?? (string.Equals(doc.Root?.Name.LocalName, "NFe", StringComparison.OrdinalIgnoreCase)
                ? doc.Root
                : null);
    }

    private static XElement? GetChildElement(XElement? parent, string localName)
    {
        if (parent is null)
        {
            return null;
        }

        return parent.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetChildText(XElement? parent, string localName)
    {
        return GetChildElement(parent, localName)?.Value.Trim();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
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
