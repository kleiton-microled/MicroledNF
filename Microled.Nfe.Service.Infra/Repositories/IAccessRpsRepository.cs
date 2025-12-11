using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Infra.Repositories;

/// <summary>
/// Repository for reading RPS from Access database (.MDB)
/// </summary>
public interface IAccessRpsRepository
{
    /// <summary>
    /// Gets pending RPS records from the Access database
    /// </summary>
    /// <param name="batchSize">Maximum number of RPS to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of RPS domain entities</returns>
    Task<IReadOnlyList<RpsRecord>> GetPendingRpsAsync(int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Marks RPS records as sent in the Access database
    /// </summary>
    /// <param name="rpsRecords">RPS records to mark as sent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsSentAsync(IEnumerable<RpsRecord> rpsRecords, CancellationToken cancellationToken);
}

/// <summary>
/// Record from Access database with RPS data and primary key for updates
/// </summary>
public class RpsRecord
{
    public int Id { get; set; }
    public Rps Rps { get; set; } = null!;
}

