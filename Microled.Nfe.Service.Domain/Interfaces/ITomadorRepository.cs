using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Domain.Interfaces;

public interface ITomadorRepository
{
    Task AddAsync(Tomador tomador, CancellationToken cancellationToken);
    Task UpdateAsync(Tomador tomador, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Tomador?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Tomador?> GetByCpfCnpjAsync(string cpfCnpj, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Tomador> Items, int TotalCount)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
