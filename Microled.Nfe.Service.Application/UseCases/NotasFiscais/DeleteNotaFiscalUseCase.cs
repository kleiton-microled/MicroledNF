using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class DeleteNotaFiscalUseCase : IDeleteNotaFiscalUseCase
{
    private readonly INotaFiscalRepository _repository;

    public DeleteNotaFiscalUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<(bool Found, bool Allowed)> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nota = await _repository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return (false, false);
        }

        if (nota.Status is NotaFiscalStatus.Authorized or NotaFiscalStatus.Cancelled)
        {
            return (true, false);
        }

        await _repository.DeleteAsync(id, cancellationToken);
        return (true, true);
    }
}
