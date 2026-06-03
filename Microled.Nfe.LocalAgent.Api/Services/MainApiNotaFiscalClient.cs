using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.LocalAgent.Api.Services;

public sealed class MainApiNotaFiscalClient : IMainApiNotaFiscalClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<MainApiNotaFiscalClient> _logger;

    public MainApiNotaFiscalClient(
        HttpClient httpClient,
        IOptions<NfeIntegrationOptions> integrationOptions,
        ILogger<MainApiNotaFiscalClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var baseUrl = integrationOptions.Value.MainApiBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }
    }

    public Task<ApiResponse<PersistNotaFiscalBatchResponse>> PersistSendResultAsync(
        PersistRpsSendResultRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<PersistRpsSendResultRequest, PersistNotaFiscalBatchResponse>(
            "api/v1/notas-fiscais/persist/send-result",
            request,
            cancellationToken);

    public Task<ApiResponse<PersistNotaFiscalBatchResponse>> PersistBatchStatusAsync(
        PersistBatchStatusDataRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<PersistBatchStatusDataRequest, PersistNotaFiscalBatchResponse>(
            "api/v1/notas-fiscais/persist/batch-status",
            request,
            cancellationToken);

    public Task<ApiResponse<PersistNotaFiscalBatchResponse>> PersistConsultResultAsync(
        PersistConsultNfeResultRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<PersistConsultNfeResultRequest, PersistNotaFiscalBatchResponse>(
            "api/v1/notas-fiscais/persist/consult-result",
            request,
            cancellationToken);

    public Task<ApiResponse<NotaFiscalResponse>> PersistCancelResultAsync(
        PersistCancelNfeResultRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<PersistCancelNfeResultRequest, NotaFiscalResponse>(
            "api/v1/notas-fiscais/persist/cancel-result",
            request,
            cancellationToken);

    public async Task<IReadOnlyList<NotaFiscalResponse>> SearchByProtocoloAsync(
        string protocolo,
        CancellationToken cancellationToken)
    {
        var url =
            $"api/v1/notas-fiscais?protocolo={Uri.EscapeDataString(protocolo)}&pageSize=100";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Main API search by protocolo failed: {StatusCode} {Url} Body={BodyPreview}",
                (int)response.StatusCode,
                BuildAbsoluteUrl(url),
                Truncate(body));
            return [];
        }

        var payload = DeserializeOrNull<ApiResponse<PagedNotaFiscalResponse>>(body, url);
        return payload?.Data?.Items ?? [];
    }

    private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress is null)
        {
            return ApiResponse<TResponse>.Fail(
                "NfeIntegration:MainApiBaseUrl is not configured.");
        }

        var absoluteUrl = BuildAbsoluteUrl(relativeUrl);
        LogPersistRequestSummary(relativeUrl, request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                relativeUrl,
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Main API request failed: POST {Url}", absoluteUrl);
            return ApiResponse<TResponse>.Fail($"Could not reach Main API at {absoluteUrl}: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "(none)";

        if (!LooksLikeJson(body))
        {
            _logger.LogError(
                "Main API returned non-JSON response: POST {Url} Status={StatusCode} ContentType={ContentType} Body={BodyPreview}",
                absoluteUrl,
                (int)response.StatusCode,
                contentType,
                Truncate(body));

            return ApiResponse<TResponse>.Fail(
                $"Main API at {absoluteUrl} returned {(int)response.StatusCode} with non-JSON body " +
                $"(Content-Type: {contentType}). Verify NfeIntegration:MainApiBaseUrl points to Microled.Nfe.Service.Api, " +
                "not the frontend, and that the API is deployed with the persist endpoints.");
        }

        ApiResponse<TResponse>? payload;
        try
        {
            payload = DeserializeOrNull<ApiResponse<TResponse>>(body, relativeUrl);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Main API JSON parse failed: POST {Url} Status={StatusCode} Body={BodyPreview}",
                absoluteUrl,
                (int)response.StatusCode,
                Truncate(body));

            return ApiResponse<TResponse>.Fail(
                $"Main API returned invalid JSON from {absoluteUrl}: {ex.Message}");
        }

        if (payload is not null)
        {
            if (payload.Success)
            {
                LogPersistResponseSummary(relativeUrl, absoluteUrl, (int)response.StatusCode, payload);
            }
            else
            {
                _logger.LogWarning(
                    "Main API POST {Url} persist failed: HTTP {StatusCode} Message={Message}",
                    absoluteUrl,
                    (int)response.StatusCode,
                    payload.Message);
            }

            return payload;
        }

        return ApiResponse<TResponse>.Fail(
            $"Main API returned {(int)response.StatusCode} without a valid payload from {absoluteUrl}.");
    }

    private string BuildAbsoluteUrl(string relativeUrl)
    {
        if (_httpClient.BaseAddress is null)
        {
            return relativeUrl;
        }

        return new Uri(_httpClient.BaseAddress, relativeUrl).ToString();
    }

    private T? DeserializeOrNull<T>(string body, string contextUrl)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static bool LooksLikeJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        var first = body.AsSpan().TrimStart();
        return first.Length > 0 && (first[0] is '{' or '[');
    }

    private void LogPersistRequestSummary<TRequest>(string relativeUrl, TRequest request)
    {
        switch (request)
        {
            case PersistRpsSendResultRequest send:
                _logger.LogInformation(
                    "Main API persist request POST {Endpoint}: Protocolo={Protocolo} Sucesso={Sucesso} CnpjPrestador={Cnpj} Itens={Itens} Autorizacoes={Autorizacoes}",
                    relativeUrl,
                    send.Protocolo ?? "(null)",
                    send.Sucesso,
                    send.CnpjPrestador ?? "(null)",
                    send.Itens.Count,
                    send.Autorizacoes.Count);
                break;
            case PersistBatchStatusDataRequest batch:
                _logger.LogInformation(
                    "Main API persist request POST {Endpoint}: Protocolo={Protocolo} Sucesso={Sucesso} Situacao={Situacao} Lote={Lote} Autorizacoes={Autorizacoes}",
                    relativeUrl,
                    batch.NumeroProtocolo,
                    batch.Sucesso,
                    batch.SituacaoCodigo,
                    batch.NumeroLote,
                    batch.Autorizacoes.Count);
                break;
            case PersistConsultNfeResultRequest consult:
                _logger.LogInformation(
                    "Main API persist request POST {Endpoint}: Autorizacoes={Autorizacoes}",
                    relativeUrl,
                    consult.Autorizacoes.Count);
                break;
            case PersistCancelNfeResultRequest cancel:
                _logger.LogInformation(
                    "Main API persist request POST {Endpoint}: NumeroNota={NumeroNota}",
                    relativeUrl,
                    cancel.NumeroNota);
                break;
            default:
                _logger.LogInformation("Main API persist request POST {Endpoint}", relativeUrl);
                break;
        }
    }

    private void LogPersistResponseSummary<TResponse>(
        string relativeUrl,
        string absoluteUrl,
        int statusCode,
        ApiResponse<TResponse> payload)
    {
        if (payload.Data is PersistNotaFiscalBatchResponse batch)
        {
            _logger.LogInformation(
                "Main API persist response POST {Endpoint}: HTTP {StatusCode} Success=true ProcessedCount={ProcessedCount} Notas={Notas} Url={Url}",
                relativeUrl,
                statusCode,
                batch.ProcessedCount,
                batch.Notas.Count,
                absoluteUrl);
            return;
        }

        if (payload.Data is NotaFiscalResponse nota)
        {
            _logger.LogInformation(
                "Main API persist response POST {Endpoint}: HTTP {StatusCode} Success=true NotaId={NotaId} Url={Url}",
                relativeUrl,
                statusCode,
                nota.Id,
                absoluteUrl);
            return;
        }

        _logger.LogInformation(
            "Main API persist response POST {Endpoint}: HTTP {StatusCode} Success=true Url={Url}",
            relativeUrl,
            statusCode,
            absoluteUrl);
    }

    private static string Truncate(string value, int maxLength = 300)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(empty)";
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength] + "...";
    }
}
