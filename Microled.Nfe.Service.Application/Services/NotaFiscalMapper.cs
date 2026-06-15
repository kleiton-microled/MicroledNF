using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Services;

public static class NotaFiscalMapper
{
    public static NotaFiscalResponse ToResponse(NotaFiscal nota)
    {
        var (erros, alertas) = RetornoXmlParser.Parse(nota.Xml);
        var rps = RetornoXmlParser.ExtractRpsContentFields(nota.Xml);
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
            Alertas = alertas,
            TipoRps = rps.TipoRps,
            StatusRps = rps.StatusRps,
            TributacaoRps = rps.TributacaoRps,
            Discriminacao = rps.Discriminacao,
            CodigoMunicipio = rps.CodigoMunicipio,
            ExigibilidadeISS = rps.ExigibilidadeISS,
            MunicipioIncidencia = rps.MunicipioIncidencia,
            TomadorEmail = rps.TomadorEmail,
            TomadorInscricaoMunicipal = rps.TomadorInscricaoMunicipal,
            TomadorCep = rps.TomadorCep,
            TomadorLogradouro = rps.TomadorLogradouro,
            TomadorNumero = rps.TomadorNumero,
            TomadorComplemento = rps.TomadorComplemento,
            TomadorBairro = rps.TomadorBairro,
            TomadorCodigoMunicipio = rps.TomadorCodigoMunicipio,
            TomadorUf = rps.TomadorUf,
            CodigoServico = rps.CodigoServico,
            ValorServicos = rps.ValorServicos,
            AliquotaServicos = rps.AliquotaServicos,
            IssRetido = rps.IssRetido,
            ValorIss = rps.ValorIss,
            ValorDeducoes = rps.ValorDeducoes,
            ValorPis = rps.ValorPis,
            ValorCofins = rps.ValorCofins,
            ValorInss = rps.ValorInss,
            ValorIr = rps.ValorIr,
            ValorCsll = rps.ValorCsll,
            OutrasRetencoes = rps.OutrasRetencoes,
            DescontoCondicionado = rps.DescontoCondicionado,
            DescontoIncondicionado = rps.DescontoIncondicionado,
            IbsIndDest = rps.IbsIndDest,
            IbsCstIbs = rps.IbsCstIbs,
            IbsAliqEstadual = rps.IbsAliqEstadual,
            IbsAliqMunicipal = rps.IbsAliqMunicipal,
            IbsCstCbs = rps.IbsCstCbs,
            IbsAliqCbs = rps.IbsAliqCbs,
        };
    }
}
