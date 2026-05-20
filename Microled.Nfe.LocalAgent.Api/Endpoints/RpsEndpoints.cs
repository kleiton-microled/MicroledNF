using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microled.Nfe.LocalAgent.Api.Contracts;
using Microled.Nfe.LocalAgent.Api.Infrastructure;
using Microled.Nfe.LocalAgent.Api.Services;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.LocalAgent.Api.Endpoints;

public static class RpsEndpoints
{
    public static IEndpointRouteBuilder MapRpsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/local/rps")
            .WithTags("Local RPS");

        group.MapPost("/generate-files", async Task<Results<Ok<LocalRpsProcessResponse>, ValidationProblem>> (
            SendRpsRequestDto request,
            IValidator<SendRpsRequestDto> validator,
            LocalRpsProcessingService processingService,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var response = await processingService.GenerateFilesAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        });

        group.MapPost("/process", async Task<Results<Ok<LocalRpsProcessResponse>, ValidationProblem>> (
            SendRpsRequestDto request,
            IValidator<SendRpsRequestDto> validator,
            LocalRpsProcessingService processingService,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var response = await processingService.ProcessAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        });

        group.MapPost("/status", async Task<Results<Ok<ConsultBatchStatusResponseDto>, ValidationProblem>> (
            ConsultBatchStatusRequestDto request,
            IValidator<ConsultBatchStatusRequestDto> validator,
            CertificateUnlockService unlockService,
            INfeGateway nfeGateway,
            LocalAgentNotaFiscalSyncService notaFiscalSyncService,
            CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator, cancellationToken);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            await unlockService.UnlockAsync(cancellationToken);
            var gatewayResult = await nfeGateway.ConsultBatchStatusAsync(
                request.NumeroProtocolo,
                request.CnpjRemetente,
                cancellationToken);

            await notaFiscalSyncService.SyncBatchStatusAsync(request, gatewayResult, cancellationToken);

            var response = new ConsultBatchStatusResponseDto
            {
                Sucesso = gatewayResult.Sucesso,
                SituacaoCodigo = gatewayResult.SituacaoCodigo,
                SituacaoNome = gatewayResult.SituacaoNome,
                NumeroLote = gatewayResult.NumeroLote,
                DataRecebimento = gatewayResult.DataRecebimento,
                DataProcessamento = gatewayResult.DataProcessamento,
                ResultadoOperacao = gatewayResult.ResultadoOperacao,
                Erros = gatewayResult.Erros
                    .Select(erro => new EventoDto
                    {
                        Codigo = erro.Codigo,
                        Descricao = erro.Descricao
                    })
                    .ToList()
            };

            return TypedResults.Ok(response);
        });

        return endpoints;
    }
}
