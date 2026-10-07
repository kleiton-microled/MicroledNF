namespace Microled.Nfe.Service.Infra.Configuration;

/// <summary>
/// Configuration options for Access database (.MDB) integration
/// </summary>
public class AccessDatabaseOptions
{
    public const string SectionName = "AccessDatabase";

    /// <summary>
    /// Full path to the Access database file (.MDB)
    /// </summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the table containing RPS records
    /// </summary>
    public string RpsTableName { get; set; } = "RPS";

    /// <summary>
    /// Tabela de notas emitidas (uma linha por NFS-e autorizada, gravada a partir do RPS de origem)
    /// </summary>
    public string NfTableName { get; set; } = "NF";

    /// <summary>
    /// Tabela de clientes usada para resolver cod_cli/nome_cli pelo CNPJ do tomador
    /// </summary>
    public string ClientesTableName { get; set; } = "clientes";

    /// <summary>
    /// Maximum number of RPS to process per batch
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Status value for pending RPS (to filter records)
    /// </summary>
    public string PendingStatus { get; set; } = "P";

    /// <summary>
    /// Status value for sent RPS (to mark records after sending)
    /// </summary>
    public string SentStatus { get; set; } = "E";

    /// <summary>
    /// Status value for generated RPS (files created but not sent to WebService)
    /// </summary>
    public string GeneratedStatus { get; set; } = "G";

    /// <summary>
    /// Name of the primary key column (usually "Id")
    /// </summary>
    public string PrimaryKeyColumn { get; set; } = "Id";

    /// <summary>
    /// Name of the status column (usually "Status")
    /// </summary>
    public string StatusColumn { get; set; } = "Status";
}

