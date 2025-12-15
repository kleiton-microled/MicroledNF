using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case implementation for canceling NFe
/// </summary>
public class CancelNfeUseCase : ICancelNfeUseCase
{
    private readonly INfeGateway _nfeGateway;
    private readonly INfeCancellationSignatureService _signatureService;
    private readonly ICertificateProvider _certificateProvider;

    public CancelNfeUseCase(
        INfeGateway nfeGateway,
        INfeCancellationSignatureService signatureService,
        ICertificateProvider certificateProvider)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
    }

    public async Task<CancelNfeResponseDto> ExecuteAsync(CancelNfeRequestDto request, CancellationToken cancellationToken)
    {
        var chaveNFe = new NfeKey(
            request.ChaveNFe.InscricaoPrestador,
            request.ChaveNFe.NumeroNFe,
            request.ChaveNFe.CodigoVerificacao,
            request.ChaveNFe.ChaveNotaNacional
        );

        // Get certificate for signing
        var certificate = _certificateProvider.GetCertificate();

        // Generate cancellation signature
        var assinaturaCancelamento = _signatureService.SignCancellation(chaveNFe, certificate);

        var cancellation = new NfeCancellation(chaveNFe, assinaturaCancelamento);

        var result = await _nfeGateway.CancelNfeAsync(cancellation, cancellationToken);

        return new CancelNfeResponseDto
        {
            Sucesso = result.Sucesso,
            Alertas = result.Alertas.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList(),
            Erros = result.Erros.Select(e => new EventoDto { Codigo = e.Codigo, Descricao = e.Descricao }).ToList()
        };
    }
}

