using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.LocalAgent.Api.Services;

public sealed class MainApiNotaFiscalClient : IMainApiNotaFiscalClient
{
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

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Main API search by protocolo failed: {StatusCode} {Protocolo}",
                response.StatusCode,
                protocolo);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PagedNotaFiscalResponse>>(
            cancellationToken);

        return payload?.Data?.Items ?? [];
    }

    private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(relativeUrl, request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(cancellationToken);

        if (payload is not null)
        {
            return payload;
        }

        return ApiResponse<TResponse>.Fail(
            $"Main API returned {(int)response.StatusCode} without a valid payload.");
    }
}
