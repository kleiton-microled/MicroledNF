using System.Text.Json;
using System.Text.Json.Serialization;
using Microled.Nfe.Service.Application.Interfaces;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Infra.Client;

/// <summary>
/// Formats parsed prefeitura gateway results for console-friendly JSON logging.
/// </summary>
internal static class PrefeituraGatewayResponseLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Format(object snapshot) =>
        JsonSerializer.Serialize(snapshot, JsonOptions);

    public static object ToSendSnapshot(RetornoEnvioLoteRpsResult result) =>
        new
        {
            result.Sucesso,
            result.Protocolo,
            ChavesNFeRPS = result.ChavesNFeRPS.Select(ToKeyPairSnapshot).ToList(),
            Alertas = result.Alertas.Select(ToEventoSnapshot).ToList(),
            Erros = result.Erros.Select(ToEventoSnapshot).ToList()
        };

    public static object ToConsultSnapshot(ConsultaNfeResult result, bool includeXmlContent) =>
        new
        {
            result.Sucesso,
            NFeList = result.NFeList.Select((nfe, index) => ToNfeSnapshot(
                nfe,
                index < result.NotaXmlList.Count ? result.NotaXmlList[index] : null,
                includeXmlContent)).ToList(),
            Alertas = result.Alertas.Select(ToEventoSnapshot).ToList(),
            Erros = result.Erros.Select(ToEventoSnapshot).ToList()
        };

    public static object ToBatchStatusSnapshot(ConsultaSituacaoLoteResult result) =>
        new
        {
            result.Sucesso,
            result.SituacaoCodigo,
            result.SituacaoNome,
            result.NumeroLote,
            result.DataRecebimento,
            result.DataProcessamento,
            result.ResultadoOperacao,
            Erros = result.Erros.Select(ToEventoSnapshot).ToList()
        };

    public static object ToCancelSnapshot(CancelNfeResult result) =>
        new
        {
            result.Sucesso,
            Alertas = result.Alertas.Select(ToEventoSnapshot).ToList(),
            Erros = result.Erros.Select(ToEventoSnapshot).ToList()
        };

    private static object ToKeyPairSnapshot(NfeRpsKeyPair pair) =>
        new
        {
            ChaveNFe = ToNfeKeySnapshot(pair.ChaveNFe),
            ChaveRPS = ToRpsKeySnapshot(pair.ChaveRPS)
        };

    private static object ToNfeSnapshot(DomainEntities.Nfe nfe, string? xml, bool includeXmlContent) =>
        new
        {
            ChaveNFe = ToNfeKeySnapshot(nfe.ChaveNFe),
            nfe.DataEmissao,
            nfe.DataFatoGerador,
            nfe.Status,
            ValorServicos = nfe.ValorServicos.Value,
            ValorDeducoes = nfe.ValorDeducoes.Value,
            ValorISS = nfe.ValorISS.Value,
            nfe.CodigoVerificacao,
            NotaXml = includeXmlContent ? xml : SummarizeXml(xml)
        };

    private static object ToNfeKeySnapshot(DomainEntities.NfeKey key) =>
        new
        {
            key.InscricaoPrestador,
            key.NumeroNFe,
            key.CodigoVerificacao,
            key.ChaveNotaNacional
        };

    private static object ToRpsKeySnapshot(DomainEntities.RpsKey key) =>
        new
        {
            key.InscricaoPrestador,
            key.SerieRps,
            key.NumeroRps
        };

    private static object ToEventoSnapshot(Evento evento) =>
        new
        {
            evento.Codigo,
            evento.Descricao,
            ChaveRPS = evento.ChaveRPS is null ? null : ToRpsKeySnapshot(evento.ChaveRPS),
            ChaveNFe = evento.ChaveNFe is null ? null : ToNfeKeySnapshot(evento.ChaveNFe)
        };

    private static object? SummarizeXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        return new
        {
            length = xml.Length,
            preview = xml.Length <= 200 ? xml : xml[..200] + "..."
        };
    }
}
