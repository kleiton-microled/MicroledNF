using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case implementation for consulting async batch status by protocol number.
/// </summary>
public class ConsultBatchStatusUseCase : IConsultBatchStatusUseCase
{
    private readonly INfeGateway _nfeGateway;

    public ConsultBatchStatusUseCase(INfeGateway nfeGateway)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
    }

    public async Task<ConsultBatchStatusResponseDto> ExecuteAsync(
        ConsultBatchStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _nfeGateway.ConsultBatchStatusAsync(
            request.NumeroProtocolo,
            request.CnpjRemetente,
            cancellationToken);

        return new ConsultBatchStatusResponseDto
        {
            Sucesso = result.Sucesso,
            SituacaoCodigo = result.SituacaoCodigo,
            SituacaoNome = result.SituacaoNome,
            NumeroLote = result.NumeroLote,
            DataRecebimento = result.DataRecebimento,
            DataProcessamento = result.DataProcessamento,
            ResultadoOperacao = result.ResultadoOperacao,
            Erros = result.Erros
                .Select(erro => new EventoDto
                {
                    Codigo = erro.Codigo,
                    Descricao = erro.Descricao
                })
                .ToList()
        };
    }
}
