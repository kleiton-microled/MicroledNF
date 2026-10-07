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

    Task<int> CountPendingRpsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks RPS records as sent in the Access database
    /// </summary>
    /// <param name="rpsRecords">RPS records to mark as sent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsSentAsync(IEnumerable<RpsRecord> rpsRecords, CancellationToken cancellationToken);

    /// <summary>
    /// Marks RPS records as generated (files created but not sent to WebService) in the Access database
    /// </summary>
    /// <param name="rpsRecords">RPS records to mark as generated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsGeneratedAsync(IEnumerable<RpsRecord> rpsRecords, CancellationToken cancellationToken);

    /// <summary>
    /// Grava no registro RPS do Access os impostos calculados pelo servico (colunas BC, PIS, COFINS, IR, CSLL)
    /// e o valor liquido da nota (Valor_liquido).
    /// Colunas inexistentes na tabela e valores nulos sao ignorados.
    /// </summary>
    Task UpdateTaxValuesAsync(int recordId, RpsTaxValues values, CancellationToken cancellationToken);

    /// <summary>
    /// Grava na tabela NF a nota autorizada pela prefeitura, com os dados do RPS de origem (Numero_RPS).
    /// Idempotente: nao duplica se a NF ja existir. Notas sem RPS de origem no Access sao ignoradas.
    /// </summary>
    Task<AccessNfInsertResult> InsertNfFromRpsAsync(AccessNfEmitida nota, CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza o pagamento da nota na tabela NF (Pago, dt_pgto, valor_depositado), localizada pelo numero da NFS-e.
    /// Retorna false quando a NF nao existe no Access.
    /// </summary>
    Task<bool> UpdateNfPagamentoAsync(
        string numeroNota,
        bool pago,
        DateTime? dataPagamento,
        decimal? valorDepositado,
        CancellationToken cancellationToken);
}

/// <summary>Nota autorizada pela prefeitura a gravar na tabela NF do Access.</summary>
public sealed class AccessNfEmitida
{
    public string NumeroNota { get; init; } = string.Empty;
    public string NumeroRps { get; init; } = string.Empty;
    public DateTime? DataEmissao { get; init; }
}

public enum AccessNfInsertResult
{
    Inserted,
    AlreadyExists,
    RpsNotFound,
    Skipped
}

/// <summary>Impostos calculados a gravar de volta na tabela RPS do Access.</summary>
public sealed class RpsTaxValues
{
    public decimal? BaseCalculo { get; init; }
    public decimal? ValorPIS { get; init; }
    public decimal? ValorCOFINS { get; init; }
    public decimal? ValorIR { get; init; }
    public decimal? ValorCSLL { get; init; }
    public decimal? ValorLiquido { get; init; }
}

/// <summary>
/// Record from Access database with RPS data and primary key for updates
/// </summary>
public class RpsRecord
{
    public int Id { get; set; }
    public Rps Rps { get; set; } = null!;
}

