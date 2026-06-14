using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Domain.Interfaces;

public interface INotaFiscalRepository
{
    Task AddAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken);
    Task<NotaFiscal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<NotaFiscal?> GetByNumeroNotaAsync(string numeroNota, CancellationToken cancellationToken);
    Task<NotaFiscal?> GetByProtocoloAsync(string protocolo, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotaFiscal>> ListByProtocoloAsync(string protocolo, CancellationToken cancellationToken);
    Task<NotaFiscal?> GetByRpsAsync(string inscricaoPrestador, string serieRps, string numeroRps, CancellationToken cancellationToken);
    Task<(IReadOnlyList<NotaFiscal> Items, int TotalCount)> SearchAsync(NotaFiscalSearchFilter filter, CancellationToken cancellationToken);
    Task UpdateAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
