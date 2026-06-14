using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Services;

public static class NotaFiscalMapper
{
    public static NotaFiscalResponse ToResponse(NotaFiscal nota)
    {
        var (erros, alertas) = RetornoXmlParser.Parse(nota.Xml);
        return new NotaFiscalResponse
        {
            Id = nota.Id,
            Protocolo = nota.Protocolo,
            NumeroNota = nota.NumeroNota,
            CodigoVerificacao = nota.CodigoVerificacao,
            NumeroLote = nota.NumeroLote,
            NumeroRps = nota.NumeroRps,
            SerieRps = nota.SerieRps,
            InscricaoPrestador = nota.InscricaoPrestador,
            CnpjPrestador = nota.CnpjPrestador,
            CpfCnpjTomador = nota.CpfCnpjTomador,
            NomeTomador = RetornoXmlParser.ExtractRazaoSocialTomador(nota.Xml),
            Status = nota.Status.ToString(),
            Pago = nota.Pago,
            DataPagamento = nota.DataPagamento,
            ValorDepositado = nota.ValorDepositado,
            DataEmissao = nota.DataEmissao,
            DataCancelamento = nota.DataCancelamento,
            CriadoPor = nota.CriadoPor,
            AlteradoPor = nota.AlteradoPor,
            CriadoEm = nota.CriadoEm,
            AlteradoEm = nota.AlteradoEm,
            HasPdf = nota.Pdf is { Length: > 0 },
            HasXml = !string.IsNullOrWhiteSpace(nota.Xml),
            Erros = erros,
            Alertas = alertas
        };
    }
}
