using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class GetNotaFiscalPdfUseCase : IGetNotaFiscalPdfUseCase
{
    private readonly INotaFiscalRepository _repository;

    public GetNotaFiscalPdfUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<(bool Found, byte[]? Pdf)> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nota = await _repository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return (false, null);
        }

        if (nota.Pdf is null or { Length: 0 })
        {
            return (true, null);
        }

        return (true, nota.Pdf);
    }
}
