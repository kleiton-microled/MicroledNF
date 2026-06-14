using System.Xml;
using System.Xml.Serialization;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.XmlSchemas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Microled.Nfe.Service.Infra.Services;

public sealed class GenerateNotaFiscalPdfUseCase : IGenerateNotaFiscalPdfUseCase
{
    private readonly INotaFiscalRepository _repository;

    public GenerateNotaFiscalPdfUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<(bool Found, bool HasXml, string? Error, byte[]? Pdf)> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nota = await _repository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return (false, false, null, null);
        }

        if (string.IsNullOrWhiteSpace(nota.Xml))
        {
            return (true, false, null, null);
        }

        tpNFe nfe;
        try
        {
            nfe = DeserializeNfe(nota.Xml);
        }
        catch (Exception ex)
        {
            return (true, true, $"XML inválido para geração de PDF: {ex.Message}", null);
        }

        var pdfBytes = GeneratePdf(nfe, nota.CodigoVerificacao, nota.Protocolo);

        nota.AttachPdf(pdfBytes, "system");
        await _repository.UpdateAsync(nota, cancellationToken);

        return (true, true, null, pdfBytes);
    }

    private static readonly XmlRootAttribute[] _nfeRootCandidates =
    [
        new XmlRootAttribute("NFe") { Namespace = "",                                    IsNullable = false },
        new XmlRootAttribute("NFe") { Namespace = "http://www.prefeitura.sp.gov.br/nfe", IsNullable = false },
        new XmlRootAttribute("NFe") { IsNullable = false },
    ];

    private static tpNFe DeserializeNfe(string xml)
    {
        // Try direct deserialization with each namespace candidate
        Exception? lastEx = null;
        foreach (var root in _nfeRootCandidates)
        {
            try
            {
                var s = new XmlSerializer(typeof(tpNFe), root);
                using var r = new StringReader(xml);
                using var xr = XmlReader.Create(r);
                return (tpNFe)s.Deserialize(xr)!;
            }
            catch (Exception ex) { lastEx = ex; }
        }

        // XML may be wrapped — extract inner <NFe> element and retry
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var nfeEl =
            doc.Descendants(System.Xml.Linq.XName.Get("NFe", "http://www.prefeitura.sp.gov.br/nfe")).FirstOrDefault()
            ?? doc.Descendants(System.Xml.Linq.XName.Get("NFe", "")).FirstOrDefault()
            ?? doc.Descendants("NFe").FirstOrDefault();

        if (nfeEl is null)
        {
            throw new InvalidOperationException(
                $"Elemento <NFe> não encontrado. Raiz: <{doc.Root?.Name.LocalName}>. Último erro: {lastEx?.Message}");
        }

        foreach (var root in _nfeRootCandidates)
        {
            try
            {
                var s = new XmlSerializer(typeof(tpNFe), root);
                using var r = new StringReader(nfeEl.ToString());
                using var xr = XmlReader.Create(r);
                return (tpNFe)s.Deserialize(xr)!;
            }
            catch (Exception ex) { lastEx = ex; }
        }

        throw new InvalidOperationException($"Não foi possível deserializar o <NFe>. Último erro: {lastEx?.Message}");
    }

    private static byte[] GeneratePdf(tpNFe nfe, string? codigoVerificacao, string? protocolo)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(8).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    // Header
                    col.Item().Border(1).BorderColor(Colors.Grey.Darken2).Padding(6).Column(header =>
                    {
                        header.Item().AlignCenter().Text("NOTA FISCAL DE SERVIÇOS ELETRÔNICA - NFS-e")
                            .Bold().FontSize(11);
                        header.Item().AlignCenter().Text("Prefeitura do Município de São Paulo")
                            .FontSize(9);
                        header.Item().Height(4);
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("Número NFS-e: ").Bold();
                                    text.Span(nfe.ChaveNFe?.NumeroNFe.ToString() ?? "-");
                                });
                                c.Item().Text(text =>
                                {
                                    text.Span("Data de Emissão: ").Bold();
                                    text.Span(nfe.DataEmissaoNFe.ToString("dd/MM/yyyy HH:mm:ss"));
                                });
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("Inscrição Municipal: ").Bold();
                                    text.Span(nfe.ChaveNFe?.InscricaoPrestador.ToString() ?? "-");
                                });
                                c.Item().Text(text =>
                                {
                                    text.Span("Competência: ").Bold();
                                    text.Span(nfe.DataFatoGeradorNFe.ToString("MM/yyyy"));
                                });
                            });
                        });
                    });

                    // Prestador
                    col.Item().Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("PRESTADOR DE SERVIÇOS").Bold().FontSize(8.5f);

                        section.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Razão Social: ").Bold();
                                text.Span(nfe.RazaoSocialPrestador ?? "-");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    var cnpj = nfe.CPFCNPJPrestador?.CNPJ ?? nfe.CPFCNPJPrestador?.CPF ?? "-";
                                    text.Span("CNPJ/CPF: ").Bold();
                                    text.Span(cnpj);
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Inscrição Municipal: ").Bold();
                                    text.Span(nfe.ChaveNFe?.InscricaoPrestador.ToString() ?? "-");
                                });
                            });
                            c.Item().Text(text =>
                            {
                                var end = nfe.EnderecoPrestador;
                                var endereco = end is null ? "-"
                                    : $"{end.TipoLogradouro} {end.Logradouro}, {end.NumeroEndereco}"
                                      + (string.IsNullOrWhiteSpace(end.ComplementoEndereco) ? "" : $" - {end.ComplementoEndereco}")
                                      + $" - {end.Bairro} - {end.UF} - CEP: {end.CEP}";
                                text.Span("Endereço: ").Bold();
                                text.Span(endereco);
                            });
                            if (!string.IsNullOrWhiteSpace(nfe.EmailPrestador))
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("E-mail: ").Bold();
                                    text.Span(nfe.EmailPrestador);
                                });
                            }
                        });
                    });

                    // Tomador
                    col.Item().Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("TOMADOR DE SERVIÇOS").Bold().FontSize(8.5f);

                        section.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("Razão Social/Nome: ").Bold();
                                text.Span(nfe.RazaoSocialTomador ?? "-");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    var doc = nfe.CPFCNPJTomador?.CNPJ ?? nfe.CPFCNPJTomador?.CPF ?? nfe.CPFCNPJTomador?.NIF ?? "-";
                                    text.Span("CNPJ/CPF/NIF: ").Bold();
                                    text.Span(doc);
                                });
                                if (nfe.InscricaoMunicipalTomador.HasValue)
                                {
                                    row.RelativeItem().Text(text =>
                                    {
                                        text.Span("Inscrição Municipal: ").Bold();
                                        text.Span(nfe.InscricaoMunicipalTomador.Value.ToString());
                                    });
                                }
                                else
                                {
                                    row.RelativeItem();
                                }
                            });
                            if (nfe.EnderecoTomador is not null)
                            {
                                c.Item().Text(text =>
                                {
                                    var end = nfe.EnderecoTomador;
                                    var endereco = $"{end.TipoLogradouro} {end.Logradouro}, {end.NumeroEndereco}"
                                        + (string.IsNullOrWhiteSpace(end.ComplementoEndereco) ? "" : $" - {end.ComplementoEndereco}")
                                        + $" - {end.Bairro} - {end.UF} - CEP: {end.CEP}";
                                    text.Span("Endereço: ").Bold();
                                    text.Span(endereco);
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(nfe.EmailTomador))
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("E-mail: ").Bold();
                                    text.Span(nfe.EmailTomador);
                                });
                            }
                        });
                    });

                    // Discriminação
                    col.Item().Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("DISCRIMINAÇÃO DOS SERVIÇOS").Bold().FontSize(8.5f);

                        section.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Código do Serviço: ").Bold();
                                    text.Span(nfe.CodigoServico.ToString());
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Tributação: ").Bold();
                                    text.Span(nfe.TributacaoNFe ?? "-");
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("ISS Retido: ").Bold();
                                    text.Span(nfe.ISSRetido ? "Sim" : "Não");
                                });
                            });
                            c.Item().Height(4);
                            c.Item().Text(nfe.Discriminacao ?? "-");
                        });
                    });

                    // Valores
                    col.Item().Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("VALORES").Bold().FontSize(8.5f);

                        section.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Valor dos Serviços: ").Bold();
                                    text.Span(nfe.ValorServicos.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Deduções: ").Bold();
                                    text.Span((nfe.ValorDeducoes ?? 0).ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    var bc = nfe.ValorServicos - (nfe.ValorDeducoes ?? 0);
                                    text.Span("Base de Cálculo: ").Bold();
                                    text.Span(bc.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                });
                            });
                            c.Item().Height(3);
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Alíquota ISS: ").Bold();
                                    text.Span((nfe.AliquotaServicos * 100).ToString("0.00") + "%");
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Valor ISS: ").Bold();
                                    text.Span(nfe.ValorISS.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Crédito: ").Bold();
                                    text.Span(nfe.ValorCredito.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                });
                            });
                            if (nfe.ValorPIS.HasValue || nfe.ValorCOFINS.HasValue || nfe.ValorINSS.HasValue || nfe.ValorIR.HasValue || nfe.ValorCSLL.HasValue)
                            {
                                c.Item().Height(3);
                                c.Item().Row(row =>
                                {
                                    if (nfe.ValorPIS.HasValue)
                                    {
                                        row.RelativeItem().Text(text =>
                                        {
                                            text.Span("PIS: ").Bold();
                                            text.Span(nfe.ValorPIS.Value.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                        });
                                    }
                                    if (nfe.ValorCOFINS.HasValue)
                                    {
                                        row.RelativeItem().Text(text =>
                                        {
                                            text.Span("COFINS: ").Bold();
                                            text.Span(nfe.ValorCOFINS.Value.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                        });
                                    }
                                    if (nfe.ValorINSS.HasValue)
                                    {
                                        row.RelativeItem().Text(text =>
                                        {
                                            text.Span("INSS: ").Bold();
                                            text.Span(nfe.ValorINSS.Value.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                        });
                                    }
                                    if (nfe.ValorIR.HasValue)
                                    {
                                        row.RelativeItem().Text(text =>
                                        {
                                            text.Span("IR: ").Bold();
                                            text.Span(nfe.ValorIR.Value.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                        });
                                    }
                                    if (nfe.ValorCSLL.HasValue)
                                    {
                                        row.RelativeItem().Text(text =>
                                        {
                                            text.Span("CSLL: ").Bold();
                                            text.Span(nfe.ValorCSLL.Value.ToString("C2", new System.Globalization.CultureInfo("pt-BR")));
                                        });
                                    }
                                });
                            }
                        });
                    });

                    // Rodapé de verificação
                    if (!string.IsNullOrWhiteSpace(codigoVerificacao) || !string.IsNullOrWhiteSpace(protocolo))
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(protocolo))
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("Protocolo: ").Bold();
                                    text.Span(protocolo);
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(codigoVerificacao))
                            {
                                c.Item().Text(text =>
                                {
                                    text.Span("Código de Verificação: ").Bold();
                                    text.Span(codigoVerificacao);
                                });
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado em: ").FontSize(7);
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(7);
                    text.Span(" — NFS-e São Paulo").FontSize(7);
                });
            });
        });

        return document.GeneratePdf();
    }
}
