using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.ValueObjects;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Infra.Client;

/// <summary>
/// Fake implementation of INfeGateway for development and testing
/// Returns mock responses without calling the real Web Service
/// </summary>
public class FakeNfeGateway : INfeGateway
{
    private readonly ILogger<FakeNfeGateway> _logger;
    private int _protocolCounter = 1;
    private int _nfeCounter = 1;

    public FakeNfeGateway(ILogger<FakeNfeGateway> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<RetornoEnvioLoteRpsResult> SendRpsBatchAsync(DomainEntities.RpsBatch batch, CancellationToken cancellationToken)
    {
        _logger.LogInformation("FAKE: SendRpsBatchAsync called with {Count} RPS", batch.RpsList.Count);

        var protocolo = $"PROTOCOLO-FAKE-{DateTime.Now:yyyyMMdd}-{_protocolCounter++:D6}";

        var result = new RetornoEnvioLoteRpsResult
        {
            Sucesso = true,
            Protocolo = protocolo,
            ChavesNFeRPS = batch.RpsList.Select((rps, index) => new NfeRpsKeyPair
            {
                ChaveNFe = new DomainEntities.NfeKey(
                    rps.ChaveRPS.InscricaoPrestador,
                    _nfeCounter++,
                    $"CODIGO-VERIFICACAO-{DateTime.Now:yyyyMMdd}-{index + 1:D6}",
                    $"CHAVE-NOTA-NACIONAL-{DateTime.Now:yyyyMMdd}-{index + 1:D12}"
                ),
                ChaveRPS = rps.ChaveRPS
            }).ToList(),
            Alertas = new List<Evento>(),
            Erros = new List<Evento>()
        };

        _logger.LogInformation("FAKE: Returning success result with protocolo {Protocolo} and {Count} NFe keys",
            protocolo, result.ChavesNFeRPS.Count);

        return Task.FromResult(result);
    }

    public Task<ConsultaNfeResult> ConsultNfeAsync(ConsultNfeCriteria criteria, CancellationToken cancellationToken)
    {
        _logger.LogInformation("FAKE: ConsultNfeAsync called for ChaveNFe: {ChaveNFe}, ChaveRps: {ChaveRps}",
            criteria.ChaveNFe, criteria.ChaveRps);

        var nfeList = new List<DomainEntities.Nfe>();

        if (criteria.ChaveNFe != null)
        {
            var nfe = new DomainEntities.Nfe(
                criteria.ChaveNFe,
                DateTime.Now.AddDays(-1),
                DateTime.Now.AddDays(-2),
                "Autorizada",
                Money.Create(1000.00m),
                Money.Create(0.00m),
                Money.Create(50.00m),
                criteria.ChaveNFe.CodigoVerificacao
            );
            nfeList.Add(nfe);
        }
        else if (criteria.ChaveRps != null)
        {
            var nfeKey = new DomainEntities.NfeKey(
                criteria.ChaveRps.InscricaoPrestador,
                12345,
                "CODIGO-VERIFICACAO-FAKE",
                "CHAVE-NOTA-NACIONAL-FAKE"
            );
            var nfe = new DomainEntities.Nfe(
                nfeKey,
                DateTime.Now.AddDays(-1),
                DateTime.Now.AddDays(-2),
                "Autorizada",
                Money.Create(500.00m),
                Money.Create(0.00m),
                Money.Create(25.00m),
                nfeKey.CodigoVerificacao
            );
            nfeList.Add(nfe);
        }

        var result = new ConsultaNfeResult
        {
            Sucesso = true,
            NFeList = nfeList,
            Alertas = new List<Evento>(),
            Erros = new List<Evento>()
        };

        _logger.LogInformation("FAKE: Returning success result with {Count} NFe", nfeList.Count);

        return Task.FromResult(result);
    }

    public Task<CancelNfeResult> CancelNfeAsync(DomainEntities.NfeCancellation cancellation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("FAKE: CancelNfeAsync called for ChaveNFe: {ChaveNFe} with signature length {SignatureLength}",
            cancellation.ChaveNFe, cancellation.AssinaturaCancelamento?.Length ?? 0);

        var result = new CancelNfeResult
        {
            Sucesso = true,
            Alertas = new List<Evento>
            {
                new Evento
                {
                    Codigo = 0,
                    Descricao = "Cancelamento processado com sucesso (FAKE)"
                }
            },
            Erros = new List<Evento>()
        };

        _logger.LogInformation("FAKE: Returning success result for cancellation");

        return Task.FromResult(result);
    }
}

