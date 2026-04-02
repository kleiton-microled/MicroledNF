using System.Data;
using System.Data.OleDb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Services;

namespace Microled.Nfe.Service.Infra.Repositories;

/// <summary>
/// Repository implementation for reading RPS from Access database (.MDB)
/// </summary>
public class AccessRpsRepository : IAccessRpsRepository
{
    private readonly AccessDatabaseOptions _options;
    private readonly NfeServiceOptions _nfeOptions;
    private readonly ILogger<AccessRpsRepository> _logger;

    public AccessRpsRepository(
        IOptions<AccessDatabaseOptions> options,
        IOptions<NfeServiceOptions> nfeOptions,
        ILogger<AccessRpsRepository> logger)
    {
        _options = options.Value;
        _nfeOptions = nfeOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RpsRecord>> GetPendingRpsAsync(int batchSize, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.DatabasePath))
        {
            throw new InvalidOperationException(
                "AccessDatabase:DatabasePath is not configured. Please set the path to the .MDB file in appsettings.json");
        }

        if (!File.Exists(_options.DatabasePath))
        {
            throw new FileNotFoundException(
                $"Access database file not found: {_options.DatabasePath}");
        }

        var connectionString = BuildConnectionString(_options.DatabasePath);
        var rpsRecords = new List<RpsRecord>();

        try
        {
            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Detect optional columns (so we can keep backward compatibility with older MDB schemas)
            var availableColumns = await GetAvailableColumnsAsync(connectionString, _options.RpsTableName, cancellationToken);
            var primaryKeyColumn = ResolveRequiredColumnName(
                availableColumns,
                new[] { _options.PrimaryKeyColumn, "index", "Id", "ID" },
                "PrimaryKeyColumn");
            var statusColumn = ResolveRequiredColumnName(
                availableColumns,
                new[] { _options.StatusColumn, "Processado", "Status" },
                "StatusColumn");
            var cIndOpSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_CIndOp", "C_IND_OP", "CODIGO_INDICADOR_OPERACAO" },
                "IbsCbsCIndOp");
            var valorPisSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorPIS", "Valor_PIS", "VlrPIS", "PIS" },
                "ValorPIS");
            var valorCofinsSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorCOFINS", "Valor_COFINS", "VlrCOFINS", "COFINS" },
                "ValorCOFINS");
            var valorInssSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorINSS", "Valor_INSS", "VlrINSS" },
                "ValorINSS");
            var valorIrSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorIR", "Valor_IR", "VlrIR", "ValorIRRF", "IR" },
                "ValorIR");
            var valorCsllSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorCSLL", "Valor_CSLL", "VlrCSLL", "CSLL" },
                "ValorCSLL");
            var valorIpiSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorIPI", "Valor_IPI", "VlrIPI" },
                "ValorIPI");
            var valorCargaTributariaSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorCargaTributaria", "Valor_Carga_Tributaria" },
                "ValorCargaTributaria");
            var percentualCargaTributariaSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "PercentualCargaTributaria", "Percentual_Carga_Tributaria" },
                "PercentualCargaTributaria");
            var fonteCargaTributariaSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "FonteCargaTributaria", "Fonte_Carga_Tributaria" },
                "FonteCargaTributaria");
            var valorTotalRecebidoSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorTotalRecebido", "Valor_Total_Recebido" },
                "ValorTotalRecebido");
            var valorFinalCobradoSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorFinalCobrado", "Valor_Final_Cobrado" },
                "ValorFinalCobrado");
            var valorMultaSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorMulta", "Valor_Multa" },
                "ValorMulta");
            var valorJurosSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ValorJuros", "Valor_Juros" },
                "ValorJuros");
            var ncmSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "NCM" },
                "NCM");
            var nbsSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "NBS", "IBSCBS_NBS" },
                "NBS");
            var finNfSeSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_FinNFSe", "FinNFSe" },
                "IbsCbsFinNFSe");
            var indFinalSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_IndFinal", "IndFinal" },
                "IbsCbsIndFinal");
            var tpOperSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_TpOper", "TpOper" },
                "IbsCbsTpOper");
            var tpEnteGovSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_TpEnteGov", "TpEnteGov" },
                "IbsCbsTpEnteGov");
            var indDestSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_IndDest", "IndDest" },
                "IbsCbsIndDest");
            var cClassTribRegSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "IBSCBS_CClassTribReg", "CClassTribReg" },
                "IbsCbsCClassTribReg");
            var cLocPrestacaoSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "CLocPrestacao", "IBSCBS_CLocPrestacao", "cLocPrestacao" },
                "CLocPrestacao");
            var nomeTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "NomeTomador", "RazaoSocialTomador", "Nome_Tomador" },
                "NomeTomador");
            var cpfCnpjTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "CpfCnpjTomador", "CPFCNPJTomador", "CNPJ_Tomador", "CPF_Tomador" },
                "CpfCnpjTomador");
            var inscricaoEstadualTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "InscricaoEstadualTomador", "IETomador", "IE_Tomador" },
                "InscricaoEstadualTomador");
            var emailTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "EmailTomador", "TomadorEmail", "Email", "e-mail" },
                "EmailTomador");
            var tipoLogradouroTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "TipoLogradouroTomador", "TpLogradouroTomador" },
                "TipoLogradouroTomador");
            var logradouroTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "LogradouroTomador", "EnderecoTomador", "Logradouro_Tomador" },
                "LogradouroTomador");
            var numeroTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "NumeroTomador", "NumeroEnderecoTomador", "Numero_Tomador" },
                "NumeroTomador");
            var complementoTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "ComplementoTomador", "ComplementoEnderecoTomador" },
                "ComplementoTomador");
            var bairroTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "BairroTomador", "Bairro_Tomador" },
                "BairroTomador");
            var codigoMunicipioTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "CodigoMunicipioTomador", "CodigoCidadeTomador", "CidadeTomador", "CodCidadeTomador" },
                "CodigoMunicipioTomador");
            var ufTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "UFTomador", "UF_Tomador" },
                "UFTomador");
            var cepTomadorSelect = BuildOptionalColumnSelect(
                availableColumns,
                new[] { "CEPTomador", "CEP_Tomador" },
                "CEPTomador");

            // Query to get pending RPS
            // Using actual column names from Access database
            var query = $@"
                SELECT TOP {batchSize} 
                    [{primaryKeyColumn}] AS RecordId,
                    [Numero_RPS] AS NumeroRps,
                    'A' AS Serie,
                    [Dt_emissao] AS DataEmissao,
                    0 AS ImPrestador,
                    [CNPJ] AS CnpjTomador,
                    {cpfCnpjTomadorSelect},
                    {nomeTomadorSelect},
                    {inscricaoEstadualTomadorSelect},
                    {emailTomadorSelect},
                    {tipoLogradouroTomadorSelect},
                    {logradouroTomadorSelect},
                    {numeroTomadorSelect},
                    {complementoTomadorSelect},
                    {bairroTomadorSelect},
                    {codigoMunicipioTomadorSelect},
                    {ufTomadorSelect},
                    {cepTomadorSelect},
                    [Valor] AS ValorServico,
                    0 AS ValorDeducao,
                    [Codigo_ISS] AS CodigoServico,
                    [Discriminacao],
                    0 AS AliquotaISS,
                    [IBSCBS_CClassTrib] AS IbsCbsCClassTrib,
                    {cIndOpSelect},
                    {valorPisSelect},
                    {valorCofinsSelect},
                    {valorInssSelect},
                    {valorIrSelect},
                    {valorCsllSelect},
                    {valorIpiSelect},
                    {valorCargaTributariaSelect},
                    {percentualCargaTributariaSelect},
                    {fonteCargaTributariaSelect},
                    {valorTotalRecebidoSelect},
                    {valorFinalCobradoSelect},
                    {valorMultaSelect},
                    {valorJurosSelect},
                    {ncmSelect},
                    {nbsSelect},
                    {finNfSeSelect},
                    {indFinalSelect},
                    {tpOperSelect},
                    {tpEnteGovSelect},
                    {indDestSelect},
                    {cClassTribRegSelect},
                    {cLocPrestacaoSelect},
                    False AS ISSRetido,
                    'RPS' AS TipoRPS,
                    'N' AS StatusRPS,
                    'T' AS TributacaoRPS
                FROM [{_options.RpsTableName}]
                WHERE [{statusColumn}] = ?
                ORDER BY [{primaryKeyColumn}]";

            using var command = new OleDbCommand(query, connection);
            // PendingStatus must match the Access column type. "P"/"E"/"G" are NOT valid for Yes/No (Processado).
            var statusValue = ResolveStatusParameterValue(connection, _options.RpsTableName, statusColumn, _options.PendingStatus);
            command.Parameters.AddWithValue("@Status", statusValue);

            using var reader = await command.ExecuteReaderAsync(cancellationToken) as OleDbDataReader;
            if (reader == null)
                throw new InvalidOperationException("Failed to get OleDbDataReader");

            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    var rps = MapToRps(reader);
                    var record = new RpsRecord
                    {
                        Id = Convert.ToInt32(reader["RecordId"]),
                        Rps = rps
                    };
                    rpsRecords.Add(record);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error mapping RPS record with Id {Id}", reader["RecordId"]);
                    // Continue processing other records
                }
            }

            _logger.LogInformation("Retrieved {Count} pending RPS from Access database", rpsRecords.Count);
            return rpsRecords;
        }
        catch (OleDbException oleEx) when (oleEx.ErrorCode == unchecked((int)0x80040E37))
        {
            // Table not found error
            var availableTables = await GetAvailableTablesAsync(connectionString, cancellationToken);
            
            string errorMessage;
            if (availableTables.Any())
            {
                var tablesList = string.Join(", ", availableTables);
                errorMessage = $"Table '{_options.RpsTableName}' not found in Access database '{_options.DatabasePath}'. " +
                             $"Available tables: {tablesList}. " +
                             $"Please check the 'RpsTableName' configuration in appsettings.json.";
            }
            else
            {
                errorMessage = $"Table '{_options.RpsTableName}' not found in Access database '{_options.DatabasePath}'. " +
                             $"Could not retrieve list of available tables. " +
                             $"Please verify the database file and check the 'RpsTableName' configuration in appsettings.json.";
            }
            
            _logger.LogError(oleEx, errorMessage);
            throw new InvalidOperationException(errorMessage, oleEx);
        }
        catch (OleDbException oleEx) when (oleEx.ErrorCode == unchecked((int)0x80040E10))
        {
            // Parameter error - likely column name mismatch
            var availableColumns = await GetAvailableColumnsAsync(connectionString, _options.RpsTableName, cancellationToken);
            
            string errorMessage;
            if (availableColumns.Any())
            {
                var columnsList = string.Join(", ", availableColumns);
                errorMessage = $"Column name mismatch in table '{_options.RpsTableName}'. " +
                             $"The query references columns that don't exist or have different names. " +
                             $"Available columns in '{_options.RpsTableName}': {columnsList}. " +
                             $"Please check the column names in the query and configuration (StatusColumn: '{_options.StatusColumn}', PrimaryKeyColumn: '{_options.PrimaryKeyColumn}').";
            }
            else
            {
                errorMessage = $"Column name mismatch in table '{_options.RpsTableName}'. " +
                             $"Could not retrieve list of available columns. " +
                             $"Please verify the table structure and check column names in configuration.";
            }
            
            _logger.LogError(oleEx, errorMessage);
            throw new InvalidOperationException(errorMessage, oleEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading RPS from Access database: {DatabasePath}", _options.DatabasePath);
            throw;
        }
    }

    private string BuildOptionalColumnSelect(List<string> availableColumns, string[] candidates, string alias)
    {
        // Access column names are case-insensitive, but we'll compare case-insensitively anyway.
        var found = candidates.FirstOrDefault(c =>
            availableColumns.Any(ac => string.Equals(ac, c, StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrEmpty(found))
        {
            _logger.LogWarning(
                "Optional Access column for {Alias} not found. Candidates={Candidates}. Using NULL fallback. Table={Table}",
                alias,
                string.Join(", ", candidates),
                _options.RpsTableName);
            return $"NULL AS {alias}";
        }

        if (!string.Equals(found, candidates[0], StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Using Access column '{Column}' for {Alias} (preferred '{Preferred}' not found). Table={Table}",
                found,
                alias,
                candidates[0],
                _options.RpsTableName);
        }

        return $"[{found}] AS {alias}";
    }

    private string ResolveRequiredColumnName(List<string> availableColumns, IEnumerable<string> candidates, string optionName)
    {
        var distinctCandidates = candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var found = distinctCandidates.FirstOrDefault(candidate =>
            availableColumns.Any(ac => string.Equals(ac, candidate, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(found))
        {
            if (!string.Equals(found, distinctCandidates[0], StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Using Access column '{Column}' for {OptionName} (configured/preferred '{Preferred}' not found). Table={Table}",
                    found,
                    optionName,
                    distinctCandidates[0],
                    _options.RpsTableName);
            }

            return found;
        }

        var available = string.Join(", ", availableColumns);
        var expected = string.Join(", ", distinctCandidates);
        throw new InvalidOperationException(
            $"Required Access column for {optionName} was not found in table '{_options.RpsTableName}'. " +
            $"Expected one of: {expected}. Available columns: {available}.");
    }

    public async Task MarkAsSentAsync(IEnumerable<RpsRecord> rpsRecords, CancellationToken cancellationToken)
    {
        var records = rpsRecords.ToList();
        if (!records.Any())
            return;

        var connectionString = BuildConnectionString(_options.DatabasePath);

        try
        {
            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            var availableColumns = await GetAvailableColumnsAsync(connectionString, _options.RpsTableName, cancellationToken);
            var primaryKeyColumn = ResolveRequiredColumnName(
                availableColumns,
                new[] { _options.PrimaryKeyColumn, "index", "Id", "ID" },
                "PrimaryKeyColumn");
            var statusColumn = ResolveRequiredColumnName(
                availableColumns,
                new[] { _options.StatusColumn, "Processado", "Status" },
                "StatusColumn");

            foreach (var record in records)
            {
                var updateQuery = $@"
                    UPDATE [{_options.RpsTableName}]
                    SET [{statusColumn}] = ?
                    WHERE [{primaryKeyColumn}] = ?";

                using var command = new OleDbCommand(updateQuery, connection);
                // Convert status string to boolean (column Processado is boolean)
                var statusValue = ConvertStatusToBoolean(_options.SentStatus);
                command.Parameters.AddWithValue("@Status", statusValue);
                command.Parameters.AddWithValue("@Id", record.Id);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Marked {Count} RPS records as sent in Access database", records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RPS status in Access database");
            throw;
        }
    }

    public async Task MarkAsGeneratedAsync(IEnumerable<RpsRecord> rpsRecords, CancellationToken cancellationToken)
    {
        var records = rpsRecords.ToList();
        if (!records.Any())
            return;

        var connectionString = BuildConnectionString(_options.DatabasePath);

        try
        {
            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            var availableColumns = await GetAvailableColumnsAsync(connectionString, _options.RpsTableName, cancellationToken);
            var primaryKeyColumn = ResolveRequiredColumnName(
                availableColumns,
                new[] { _options.PrimaryKeyColumn, "index", "Id", "ID" },
                "PrimaryKeyColumn");
            var statusColumn = ResolveRequiredColumnName(
                availableColumns,
                new[] { _options.StatusColumn, "Processado", "Status" },
                "StatusColumn");

            foreach (var record in records)
            {
                var updateQuery = $@"
                    UPDATE [{_options.RpsTableName}]
                    SET [{statusColumn}] = ?
                    WHERE [{primaryKeyColumn}] = ?";

                using var command = new OleDbCommand(updateQuery, connection);
                // Convert status string to boolean (column Processado is boolean)
                var statusValue = ConvertStatusToBoolean(_options.GeneratedStatus);
                command.Parameters.AddWithValue("@Status", statusValue);
                command.Parameters.AddWithValue("@Id", record.Id);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Marked {Count} RPS records as generated in Access database", records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RPS status to generated in Access database");
            throw;
        }
    }

    /// <summary>
    /// Builds the parameter for WHERE [StatusColumn] = ? so the type matches the column (critical for Access Yes/No).
    /// </summary>
    private object ResolveStatusParameterValue(
        OleDbConnection connection,
        string tableName,
        string statusColumn,
        string configuredStatus)
    {
        if (StatusColumnStoresBoolean(connection, tableName, statusColumn))
        {
            _logger.LogDebug(
                "Access status column '{StatusColumn}' is boolean; mapping PendingStatus '{Pending}' to bool.",
                statusColumn,
                configuredStatus);
            return ConvertStatusToBoolean(configuredStatus);
        }

        // Text or numeric status column (e.g. "P" / "E")
        if (bool.TryParse(configuredStatus, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(configuredStatus, out var intValue))
        {
            return intValue;
        }

        var upperStatus = configuredStatus.ToUpperInvariant();
        if (upperStatus is "N" or "FALSE" or "0" or "NO")
        {
            return false;
        }

        if (upperStatus is "S" or "TRUE" or "1" or "YES")
        {
            return true;
        }

        return configuredStatus;
    }

    /// <summary>
    /// Yes/No columns in Access (e.g. Processado) require bool parameters, not strings like "P".
    /// </summary>
    private bool StatusColumnStoresBoolean(OleDbConnection connection, string tableName, string statusColumn)
    {
        if (statusColumn.Equals("Processado", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var dataType = TryGetColumnOleDbDataType(connection, tableName, statusColumn);
        // 11 = DBTYPE_BOOL (Access Yes/No)
        return dataType == 11;
    }

    private static int? TryGetColumnOleDbDataType(OleDbConnection connection, string tableName, string columnName)
    {
        try
        {
            var restrictions = new string?[] { null, null, tableName, columnName };
            var schema = connection.GetSchema("Columns", restrictions);
            foreach (DataRow row in schema.Rows)
            {
                var name = row["COLUMN_NAME"]?.ToString();
                if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    if (row["DATA_TYPE"] != DBNull.Value)
                    {
                        return Convert.ToInt32(row["DATA_TYPE"]);
                    }
                }
            }
        }
        catch
        {
            // ignore; fall back to name heuristics only
        }

        return null;
    }

    private object ConvertStatusToBoolean(string statusValue)
    {
        // Convert status string to boolean for Access boolean column
        // Access boolean columns typically use True/False or 0/-1
        if (bool.TryParse(statusValue, out var boolValue))
        {
            return boolValue;
        }
        else if (int.TryParse(statusValue, out var intValue))
        {
            return intValue != 0;
        }
        else
        {
            // Try common boolean string representations
            var upperStatus = statusValue.ToUpperInvariant();
            if (upperStatus == "N" || upperStatus == "FALSE" || upperStatus == "0" || upperStatus == "NO" || upperStatus == "P")
            {
                return false;
            }
            else if (upperStatus == "S" || upperStatus == "TRUE" || upperStatus == "1" || upperStatus == "YES" || 
                     upperStatus == "E" || upperStatus == "G" || upperStatus == "SENT" || upperStatus == "GENERATED")
            {
                return true;
            }
            else
            {
                // Default to false if unknown
                return false;
            }
        }
    }

    private string BuildConnectionString(string databasePath)
    {
        // Use ACE.OLEDB.12.0 for .MDB files (Access 2007+)
        // For older .MDB files, may need to use Microsoft.Jet.OLEDB.4.0
        return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Persist Security Info=False;";
    }

    private async Task<List<string>> GetAvailableTablesAsync(string connectionString, CancellationToken cancellationToken)
    {
        var tables = new List<string>();
        try
        {
            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Get schema information for tables
            // Note: GetSchema is synchronous, but we're already in an async context
            var schema = connection.GetSchema("Tables");
            
            foreach (System.Data.DataRow row in schema.Rows)
            {
                var tableName = row["TABLE_NAME"]?.ToString();
                var tableType = row["TABLE_TYPE"]?.ToString();
                
                // Only include user tables (exclude system tables and views)
                if (!string.IsNullOrEmpty(tableName) && 
                    (tableType == "TABLE" || tableType == "ACCESS TABLE"))
                {
                    tables.Add(tableName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve table list from Access database");
        }
        
        return tables;
    }

    private async Task<List<string>> GetAvailableColumnsAsync(string connectionString, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        try
        {
            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Get schema information for columns
            var restrictions = new string?[] { null, null, tableName, null };
            var schema = connection.GetSchema("Columns", restrictions);
            
            foreach (System.Data.DataRow row in schema.Rows)
            {
                var columnName = row["COLUMN_NAME"]?.ToString();
                if (!string.IsNullOrEmpty(columnName))
                {
                    columns.Add(columnName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve column list from Access database for table {TableName}", tableName);
        }
        
        return columns;
    }

    private Rps MapToRps(OleDbDataReader reader)
    {
        // TODO: Adjust column names and data types based on actual Access database schema
        // This is a template that should be adjusted to match your actual database structure

        var numeroRps = Convert.ToInt64(reader["NumeroRps"]);
        var serieRps = reader["Serie"]?.ToString() ?? "A";
        
        // Get InscricaoPrestador from database or configuration
        var imPrestadorRaw = reader["ImPrestador"];
        long inscricaoPrestador;

        // Try to get from database first
        if (imPrestadorRaw != null && imPrestadorRaw != DBNull.Value)
        {
            var imValue = Convert.ToInt64(imPrestadorRaw);
            if (imValue > 0)
            {
                inscricaoPrestador = imValue;
            }
            else if (!string.IsNullOrEmpty(_nfeOptions.DefaultIssuerIm) && 
                     long.TryParse(_nfeOptions.DefaultIssuerIm, out var configIm))
            {
                inscricaoPrestador = configIm;
                _logger.LogWarning("Using DefaultIssuerIm from configuration ({Im}) because database value is 0", configIm);
            }
            else
            {
                throw new InvalidOperationException(
                    "InscricaoPrestador is required. Please set DefaultIssuerIm in appsettings.json or ensure the database has a valid ImPrestador value.");
            }
        }
        else if (!string.IsNullOrEmpty(_nfeOptions.DefaultIssuerIm) && 
                 long.TryParse(_nfeOptions.DefaultIssuerIm, out var configIm))
        {
            inscricaoPrestador = configIm;
            _logger.LogInformation("Using DefaultIssuerIm from configuration ({Im}) because database column is null or empty", configIm);
        }
        else
        {
            throw new InvalidOperationException(
                "InscricaoPrestador is required. Please set DefaultIssuerIm in appsettings.json or ensure the database has a valid ImPrestador value.");
        }

        var chaveRps = new RpsKey(inscricaoPrestador, numeroRps, serieRps);

        var tipoRps = ParseTipoRps(reader["TipoRPS"]?.ToString() ?? "RPS");
        var dataEmissao = Convert.ToDateTime(reader["DataEmissao"]);
        var statusRps = ParseStatusRps(reader["StatusRPS"]?.ToString() ?? "N");
        var tributacaoRps = ParseTipoTributacao(reader["TributacaoRPS"]?.ToString() ?? "T");

        // Prestador: sempre o emitente configurado (não confundir com [CNPJ] do MDB, que é do tomador).
        var cnpjPrestador = _nfeOptions.DefaultIssuerCnpj?.Trim();
        if (string.IsNullOrEmpty(cnpjPrestador))
        {
            throw new InvalidOperationException(
                "DefaultIssuerCnpj is required for Access RPS (prestador). Configure NfeService:DefaultIssuerCnpj.");
        }

        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj(cnpjPrestador),
            inscricaoPrestador,
            "PRESTADOR",
            null,
            null);

        // Item
        var valorServicos = Convert.ToDecimal(reader["ValorServico"]);
        var valorDeducoes = Convert.ToDecimal(reader["ValorDeducao"] ?? 0);
        var codigoServico = Convert.ToInt32(reader["CodigoServico"]);
        var discriminacao = reader["Discriminacao"]?.ToString() ?? throw new InvalidOperationException("Discriminacao is required");
        var aliquotaISS = Convert.ToDecimal(reader["AliquotaISS"] ?? 0);
        var issRetido = Convert.ToBoolean(reader["ISSRetido"] ?? false);

        var item = new RpsItem(
            codigoServico,
            discriminacao,
            Money.Create(valorServicos),
            Money.Create(valorDeducoes),
            Aliquota.Create(aliquotaISS),
            issRetido ? IssRetido.Sim : IssRetido.Nao,
            tributacaoRps
        );

        // Tomador: coluna dedicada (se existir) ou [CNPJ] AS CnpjTomador + e-mail etc. no MDB.
        ServiceCustomer? tomador = null;
        var cpfCnpjTomador = GetStringOrNull(reader, "CpfCnpjTomador");
        if (string.IsNullOrEmpty(cpfCnpjTomador))
        {
            cpfCnpjTomador = GetStringOrNull(reader, "CnpjTomador");
        }

        if (!string.IsNullOrEmpty(cpfCnpjTomador))
        {
            var nomeTomador = GetStringOrNull(reader, "NomeTomador");
            var enderecoTomador = CreateAddress(
                GetStringOrNull(reader, "TipoLogradouroTomador"),
                GetStringOrNull(reader, "LogradouroTomador"),
                GetStringOrNull(reader, "NumeroTomador"),
                GetStringOrNull(reader, "ComplementoTomador"),
                GetStringOrNull(reader, "BairroTomador"),
                GetIntOrNull(reader, "CodigoMunicipioTomador"),
                GetStringOrNull(reader, "UFTomador"),
                GetIntOrNull(reader, "CEPTomador"));
            tomador = new ServiceCustomer(
                cpfCnpjTomador.Length == 11
                    ? CpfCnpj.CreateFromCpf(cpfCnpjTomador)
                    : CpfCnpj.CreateFromCnpj(cpfCnpjTomador),
                null,
                GetLongOrNull(reader, "InscricaoEstadualTomador"),
                nomeTomador ?? "TOMADOR",
                enderecoTomador,
                GetStringOrNull(reader, "EmailTomador")
            );
        }

        var rps = new Rps(
            chaveRps,
            tipoRps,
            DateOnly.FromDateTime(dataEmissao),
            statusRps,
            tributacaoRps,
            item,
            prestador,
            tomador
        );

        // Layout 2: IBSCBS cClassTrib (coluna Access: IBSCBS_CClassTrib)
        var cClassTribRaw = GetStringOrNull(reader, "IbsCbsCClassTrib");

        // Fail-fast por item (antes de enviar): no layout 2, cClassTrib é obrigatório e deve ser numérico.
        // Isso evita derrubar o lote inteiro no SOAP client e mantém o item pendente no Access para correção.
        var versaoSchemaStr = _nfeOptions.Versao?.Replace(".", "") ?? "0";
        var versaoSchema = int.TryParse(versaoSchemaStr, out var v) ? v : 0;
        if (versaoSchema >= 2)
        {
            // No layout 2, cClassTrib é obrigatório para evitar erro 628.
            // Porém, para compatibilidade com bases antigas/linhas incompletas, fazemos fallback para "000001".
            string normalized;
            try
            {
                normalized = IbsCbsCClassTribValidator.ValidateAndGet(cClassTribRaw);
            }
            catch (Exception)
            {
                normalized = "000001";
                var rowId = reader["RecordId"];
                _logger.LogWarning(
                    "IBSCBS_CClassTrib inválido/ausente no Access. Usando fallback {Fallback}. Table={Table}, RowId={RowId}, Numero_RPS={NumeroRps}, Codigo_ISS={CodigoServico}, Valor='{Valor}'. VersaoSchema={VersaoSchema}",
                    normalized,
                    _options.RpsTableName,
                    rowId,
                    numeroRps,
                    codigoServico,
                    cClassTribRaw ?? "NULL",
                    _nfeOptions.Versao);
            }

            // grava normalizado (trim) preservando zeros à esquerda
            rps.SetIbsCbsCClassTrib(normalized);
            cClassTribRaw = normalized;
        }

        // Layout 2: IBSCBS cIndOp (coluna Access: IBSCBS_CIndOp)
        // Regra PMSP: deve ter 6 dígitos numéricos. Se vazio/nulo/inválido => fallback 100301.
        var cIndOpRaw = GetStringOrNull(reader, "IbsCbsCIndOp");
        var cIndOpNormalized = IbsCbsCIndOpNormalizer.Normalize(cIndOpRaw);
        var cIndOpFinal = cIndOpNormalized ?? IbsCbsCIndOpNormalizer.DefaultCIndOp;
        rps.SetIbsCbsCIndOp(cIndOpFinal);

        var rowIdForIndOp = reader["RecordId"];
        if (cIndOpNormalized == null)
        {
            _logger.LogWarning(
                "IBSCBS_CIndOp vazio/nulo/inválido no Access. Usando fallback {Fallback}. Table={Table}, RowId={RowId}, Numero_RPS={NumeroRps}, Codigo_ISS={CodigoServico}, Valor='{Valor}'",
                cIndOpFinal,
                _options.RpsTableName,
                rowIdForIndOp,
                numeroRps,
                codigoServico,
                cIndOpRaw ?? "NULL");
        }
        else if (!string.Equals(cIndOpNormalized, cIndOpRaw, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "IBSCBS_CIndOp sanitizado. Raw='{Raw}' => Final='{Final}'. Table={Table}, RowId={RowId}, Numero_RPS={NumeroRps}, Codigo_ISS={CodigoServico}",
                cIndOpRaw ?? "NULL",
                cIndOpFinal,
                _options.RpsTableName,
                rowIdForIndOp,
                numeroRps,
                codigoServico);
        }

        var tributos = CreateTaxInfo(reader, valorServicos);
        if (tributos != null)
        {
            rps.SetTributos(tributos);
        }

        var ibsCbsInfo = CreateIbsCbsInfo(
            reader,
            cClassTribRaw,
            cIndOpFinal,
            tomador);
        if (ibsCbsInfo != null)
        {
            rps.SetIbsCbs(ibsCbsInfo);
        }

        return rps;
    }

    private static RpsTaxInfo? CreateTaxInfo(OleDbDataReader reader, decimal valorServicos)
    {
        var valorPis = GetDecimalOrNull(reader, "ValorPIS");
        var valorCofins = GetDecimalOrNull(reader, "ValorCOFINS");
        var valorInss = GetDecimalOrNull(reader, "ValorINSS");
        var valorIr = GetDecimalOrNull(reader, "ValorIR");
        var valorCsll = GetDecimalOrNull(reader, "ValorCSLL");
        var valorIpi = GetDecimalOrNull(reader, "ValorIPI");
        var valorCargaTributaria = GetDecimalOrNull(reader, "ValorCargaTributaria");
        var percentualCargaTributaria = GetDecimalOrNull(reader, "PercentualCargaTributaria");
        var fonteCargaTributaria = GetStringOrNull(reader, "FonteCargaTributaria");
        var valorTotalRecebido = GetDecimalOrNull(reader, "ValorTotalRecebido");
        var valorFinalCobrado = GetDecimalOrNull(reader, "ValorFinalCobrado");
        var valorMulta = GetDecimalOrNull(reader, "ValorMulta");
        var valorJuros = GetDecimalOrNull(reader, "ValorJuros");
        var ncm = GetStringOrNull(reader, "NCM");

        var hasAnyValue =
            valorPis.HasValue ||
            valorCofins.HasValue ||
            valorInss.HasValue ||
            valorIr.HasValue ||
            valorCsll.HasValue ||
            valorIpi.HasValue ||
            valorCargaTributaria.HasValue ||
            percentualCargaTributaria.HasValue ||
            !string.IsNullOrWhiteSpace(fonteCargaTributaria) ||
            valorTotalRecebido.HasValue ||
            valorFinalCobrado.HasValue ||
            valorMulta.HasValue ||
            valorJuros.HasValue ||
            !string.IsNullOrWhiteSpace(ncm);

        if (!hasAnyValue)
        {
            return null;
        }

        var valorFinalCobradoEfetivo = valorFinalCobrado is null
            ? valorServicos
            : valorFinalCobrado == 0m && valorServicos != 0m
                ? valorServicos
                : valorFinalCobrado.Value;

        return new RpsTaxInfo(
            MapMoney(valorPis),
            MapMoney(valorCofins),
            MapMoney(valorInss),
            MapMoney(valorIr),
            MapMoney(valorCsll),
            MapMoney(valorIpi),
            MapMoney(valorCargaTributaria),
            percentualCargaTributaria,
            fonteCargaTributaria,
            MapMoney(valorTotalRecebido),
            MapMoney(valorFinalCobradoEfetivo),
            MapMoney(valorMulta),
            MapMoney(valorJuros),
            ncm);
    }

    private static RpsIbsCbsInfo? CreateIbsCbsInfo(
        OleDbDataReader reader,
        string? cClassTrib,
        string? cIndOp,
        ServiceCustomer? tomador)
    {
        var finNfSe = GetIntOrNull(reader, "IbsCbsFinNFSe");
        var indFinal = GetIntOrNull(reader, "IbsCbsIndFinal");
        var tpOper = GetIntOrNull(reader, "IbsCbsTpOper");
        var tpEnteGov = GetIntOrNull(reader, "IbsCbsTpEnteGov");
        var indDest = GetIntOrNull(reader, "IbsCbsIndDest");
        var cClassTribReg = GetStringOrNull(reader, "IbsCbsCClassTribReg");
        var nbs = GetStringOrNull(reader, "NBS");
        var cLocPrestacao = GetIntOrNull(reader, "CLocPrestacao");

        var hasAnyValue =
            finNfSe.HasValue ||
            indFinal.HasValue ||
            !string.IsNullOrWhiteSpace(cIndOp) ||
            tpOper.HasValue ||
            tpEnteGov.HasValue ||
            indDest.HasValue ||
            !string.IsNullOrWhiteSpace(cClassTrib) ||
            !string.IsNullOrWhiteSpace(cClassTribReg) ||
            !string.IsNullOrWhiteSpace(nbs) ||
            cLocPrestacao.HasValue ||
            tomador != null;

        if (!hasAnyValue)
        {
            return null;
        }

        return new RpsIbsCbsInfo(
            finNfSe,
            indFinal,
            cIndOp,
            tpOper,
            null,
            tpEnteGov,
            indDest,
            tomador != null ? CreateIbsCbsPersonInfo(tomador) : null,
            cClassTrib,
            cClassTribReg,
            nbs,
            cLocPrestacao,
            null);
    }

    private static RpsIbsCbsPersonInfo CreateIbsCbsPersonInfo(ServiceCustomer tomador)
    {
        var cpfCnpj = tomador.CpfCnpj?.GetValue();

        return new RpsIbsCbsPersonInfo(
            cpf: cpfCnpj?.Length == 11 ? cpfCnpj : null,
            cnpj: cpfCnpj?.Length == 14 ? cpfCnpj : null,
            nif: null,
            naoNif: null,
            razaoSocial: tomador.RazaoSocial ?? string.Empty,
            endereco: tomador.Endereco,
            email: tomador.Email);
    }

    private static Address? CreateAddress(
        string? tipoLogradouro,
        string? logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        int? codigoMunicipio,
        string? uf,
        int? cep)
    {
        var hasAnyValue =
            !string.IsNullOrWhiteSpace(tipoLogradouro) ||
            !string.IsNullOrWhiteSpace(logradouro) ||
            !string.IsNullOrWhiteSpace(numero) ||
            !string.IsNullOrWhiteSpace(complemento) ||
            !string.IsNullOrWhiteSpace(bairro) ||
            codigoMunicipio.HasValue ||
            !string.IsNullOrWhiteSpace(uf) ||
            cep.HasValue;

        if (!hasAnyValue)
        {
            return null;
        }

        return new Address(tipoLogradouro, logradouro, numero, complemento, bairro, codigoMunicipio, uf, cep);
    }

    private static Money? MapMoney(decimal? value) => value.HasValue ? Money.Create(value.Value) : null;

    private static string? GetStringOrNull(OleDbDataReader reader, string columnName)
    {
        var value = reader[columnName];
        return value == DBNull.Value ? null : value?.ToString();
    }

    private static decimal? GetDecimalOrNull(OleDbDataReader reader, string columnName)
    {
        var value = reader[columnName];
        return value == DBNull.Value ? null : Convert.ToDecimal(value);
    }

    private static int? GetIntOrNull(OleDbDataReader reader, string columnName)
    {
        var value = reader[columnName];
        return value == DBNull.Value ? null : Convert.ToInt32(value);
    }

    private static long? GetLongOrNull(OleDbDataReader reader, string columnName)
    {
        var value = reader[columnName];
        return value == DBNull.Value ? null : Convert.ToInt64(value);
    }

    private TipoRps ParseTipoRps(string? value)
    {
        return value?.ToUpperInvariant() switch
        {
            "RPS" => TipoRps.RPS,
            "RPS-M" or "RPSM" => TipoRps.RPS_M,
            "RPS-C" or "RPSC" => TipoRps.RPS_C,
            _ => TipoRps.RPS
        };
    }

    private StatusRps ParseStatusRps(string? value)
    {
        return value?.ToUpperInvariant() switch
        {
            "N" or "NORMAL" => StatusRps.Normal,
            "C" or "CANCELADO" => StatusRps.Cancelado,
            "E" or "EXTRAVIADO" => StatusRps.Extraviado,
            _ => StatusRps.Normal
        };
    }

    private TipoTributacao ParseTipoTributacao(string? value)
    {
        return value?.ToUpperInvariant() switch
        {
            "T" => TipoTributacao.TributacaoMunicipio,
            "F" => TipoTributacao.TributacaoForaMunicipio,
            "I" => TipoTributacao.Isento,
            "J" => TipoTributacao.IssSuspensoJudicial,
            _ => TipoTributacao.TributacaoMunicipio
        };
    }
}

