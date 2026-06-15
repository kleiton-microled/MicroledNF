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
            var content = ResolveSpContentElement(doc);
            var searchRoot = content ?? doc.Root;
            if (searchRoot is null)
            {
                return null;
            }

            return FirstNonEmpty(
                GetChildText(searchRoot, "RazaoSocialTomador"),
                GetDescendantTextWithin(searchRoot, "RazaoSocialTomador"));
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
            var spContent = ResolveSpContentElement(doc);
            if (spContent is not null)
            {
                return ExtractFromSpContent(spContent);
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

    private static RpsContentFields ExtractFromSpContent(XElement content)
    {
        var isNfe = content.Name.LocalName.Equals("NFe", StringComparison.OrdinalIgnoreCase);
        var enderecoTomador = GetChildElement(content, "EnderecoTomador");
        var ibscbs = GetChildElement(content, "IBSCBS");
        var retornoIbs = GetChildElement(content, "RetornoComplementarIBSCBS");
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
            TipoRps = GetChildText(content, "TipoRPS"),
            StatusRps = GetChildText(content, isNfe ? "StatusNFe" : "StatusRPS"),
            TributacaoRps = GetChildText(content, isNfe ? "TributacaoNFe" : "TributacaoRPS"),
            Discriminacao = GetChildText(content, "Discriminacao"),
            TomadorEmail = GetChildText(content, "EmailTomador"),
            TomadorInscricaoMunicipal = ExtractTomadorInscricaoMunicipal(content),
            TomadorCep = GetChildText(enderecoTomador, "CEP"),
            TomadorLogradouro = logradouroCompleto,
            TomadorNumero = GetChildText(enderecoTomador, "NumeroEndereco"),
            TomadorComplemento = GetChildText(enderecoTomador, "ComplementoEndereco"),
            TomadorBairro = GetChildText(enderecoTomador, "Bairro"),
            TomadorCodigoMunicipio = GetChildText(enderecoTomador, "Cidade"),
            TomadorUf = GetChildText(enderecoTomador, "UF"),
            CodigoServico = GetChildText(content, "CodigoServico"),
            ValorServicos = isNfe
                ? GetChildText(content, "ValorServicos")
                : FirstNonEmpty(GetChildText(content, "ValorServicos"), GetChildText(content, "ValorFinalCobrado")),
            AliquotaServicos = GetChildText(content, "AliquotaServicos"),
            IssRetido = GetChildText(content, "ISSRetido"),
            ValorIss = isNfe ? GetChildText(content, "ValorISS") : null,
            ValorDeducoes = GetChildText(content, "ValorDeducoes"),
            ValorPis = GetChildText(content, "ValorPIS"),
            ValorCofins = GetChildText(content, "ValorCOFINS"),
            ValorInss = GetChildText(content, "ValorINSS"),
            ValorIr = GetChildText(content, "ValorIR"),
            ValorCsll = GetChildText(content, "ValorCSLL"),
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

    private static string? ExtractTomadorInscricaoMunicipal(XElement content)
    {
        var imTomador = FirstNonEmpty(
            GetChildText(content, "InscricaoMunicipalTomador"),
            GetDescendantTextWithin(content, "InscricaoMunicipalTomador"));

        if (!string.IsNullOrWhiteSpace(imTomador))
        {
            return imTomador;
        }

        var tomadorScope = content.Name.LocalName.Equals("TomadorServico", StringComparison.OrdinalIgnoreCase)
            ? content
            : FindDescendantByLocalName(content, "TomadorServico");

        if (tomadorScope is null)
        {
            return null;
        }

        var identificacao = FindDescendantByLocalName(tomadorScope, "IdentificacaoTomador");
        if (identificacao is not null)
        {
            var imIdentificacao = FirstNonEmpty(
                GetChildText(identificacao, "InscricaoMunicipal"),
                GetDescendantTextWithin(identificacao, "InscricaoMunicipal"));

            if (!string.IsNullOrWhiteSpace(imIdentificacao))
            {
                return imIdentificacao;
            }
        }

        return GetDescendantTextWithin(tomadorScope, "InscricaoMunicipal");
    }

    private static RpsContentFields ExtractFromAbrasfRps(XElement root)
    {
        var rpsIdent = FindDescendantByLocalName(root, "IdentificacaoRps");
        var servico = FindDescendantByLocalName(root, "Servico");
        var valores = servico is not null ? FindDescendantByLocalName(servico, "Valores") : null;
        var tomador = FindDescendantByLocalName(root, "TomadorServico");
        var tomadorEndereco = tomador is not null ? FindDescendantByLocalName(tomador, "Endereco") : null;
        var ibscbs = valores is not null ? FindDescendantByLocalName(valores, "IBSCBS") : null;
        var ibsValores = ibscbs is not null ? FindDescendantByLocalName(ibscbs, "valores") : null;
        var ibsTrib = ibsValores is not null ? FindDescendantByLocalName(ibsValores, "trib") : null;
        var ibsGibscbs = ibsTrib is not null ? FindDescendantByLocalName(ibsTrib, "gIBSCBS") : null;

        return new RpsContentFields
        {
            TipoRps = rpsIdent is not null ? GetChildText(rpsIdent, "Tipo") : null,
            StatusRps = GetDescendantTextWithin(root, "Status"),
            TributacaoRps = GetDescendantTextWithin(root, "TributacaoRps"),
            Discriminacao = servico is not null ? GetChildText(servico, "Discriminacao") : null,
            CodigoMunicipio = servico is not null ? GetChildText(servico, "CodigoMunicipio") : null,
            ExigibilidadeISS = servico is not null ? GetChildText(servico, "ExigibilidadeISS") : null,
            MunicipioIncidencia = servico is not null ? GetChildText(servico, "MunicipioIncidencia") : null,
            TomadorEmail = tomador is not null ? GetChildText(tomador, "Email") : null,
            TomadorInscricaoMunicipal = tomador is not null ? ExtractTomadorInscricaoMunicipal(tomador) : null,
            TomadorCep = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "Cep") : null,
            TomadorLogradouro = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "Endereco") : null,
            TomadorNumero = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "Numero") : null,
            TomadorComplemento = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "Complemento") : null,
            TomadorBairro = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "Bairro") : null,
            TomadorCodigoMunicipio = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "CodigoMunicipio") : null,
            TomadorUf = tomadorEndereco is not null ? GetChildText(tomadorEndereco, "Uf") : null,
            CodigoServico = servico is not null ? GetChildText(servico, "ItemListaServico") : null,
            ValorServicos = valores is not null ? GetChildText(valores, "ValorServicos") : null,
            AliquotaServicos = valores is not null ? GetChildText(valores, "Aliquota") : null,
            IssRetido = servico is not null ? GetChildText(servico, "IssRetido") : null,
            ValorIss = valores is not null ? GetChildText(valores, "ValorIss") : null,
            ValorDeducoes = valores is not null ? GetChildText(valores, "ValorDeducoes") : null,
            ValorPis = valores is not null ? GetChildText(valores, "ValorPis") : null,
            ValorCofins = valores is not null ? GetChildText(valores, "ValorCofins") : null,
            ValorInss = valores is not null ? GetChildText(valores, "ValorInss") : null,
            ValorIr = valores is not null ? GetChildText(valores, "ValorIr") : null,
            ValorCsll = valores is not null ? GetChildText(valores, "ValorCsll") : null,
            OutrasRetencoes = valores is not null ? GetChildText(valores, "OutrasRetencoes") : null,
            DescontoCondicionado = valores is not null ? GetChildText(valores, "DescontoCondicionado") : null,
            DescontoIncondicionado = valores is not null ? GetChildText(valores, "DescontoIncondicionado") : null,
            IbsIndDest = ibscbs is not null ? GetChildText(ibscbs, "indDest") : null,
            IbsCstIbs = ibsGibscbs is not null ? GetChildText(ibsGibscbs, "CST") : null,
            IbsAliqEstadual = ibsValores is not null ? GetChildText(ibsValores, "pAliqEstadual") : null,
            IbsAliqMunicipal = ibsValores is not null ? GetChildText(ibsValores, "pAliqMunicipal") : null,
            IbsCstCbs = ibsGibscbs is not null ? GetChildText(ibsGibscbs, "CSTCbs") : null,
            IbsAliqCbs = ibsValores is not null ? GetChildText(ibsValores, "pAliqCbs") : null,
        };
    }

    private static XElement? ResolveSpContentElement(XDocument doc) =>
        ResolveNfeElement(doc) ?? ResolveRpsElement(doc);

    private static XElement? ResolveNfeElement(XDocument doc)
    {
        return doc.Descendants(XName.Get("NFe", "http://www.prefeitura.sp.gov.br/nfe")).FirstOrDefault()
            ?? doc.Descendants(XName.Get("NFe", "")).FirstOrDefault()
            ?? doc.Descendants("NFe").FirstOrDefault()
            ?? (string.Equals(doc.Root?.Name.LocalName, "NFe", StringComparison.OrdinalIgnoreCase)
                ? doc.Root
                : null);
    }

    private static XElement? ResolveRpsElement(XDocument doc)
    {
        return doc.Descendants(XName.Get("RPS", "http://www.prefeitura.sp.gov.br/nfe")).FirstOrDefault()
            ?? doc.Descendants(XName.Get("RPS", "")).FirstOrDefault()
            ?? doc.Descendants("RPS").FirstOrDefault()
            ?? (string.Equals(doc.Root?.Name.LocalName, "RPS", StringComparison.OrdinalIgnoreCase)
                ? doc.Root
                : null);
    }

    private static XElement? FindDescendantByLocalName(XElement root, string localName)
    {
        return root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetDescendantTextWithin(XElement root, string localName)
    {
        return FindDescendantByLocalName(root, localName)?.Value.Trim();
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
        var value = GetChildElement(parent, localName)?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
