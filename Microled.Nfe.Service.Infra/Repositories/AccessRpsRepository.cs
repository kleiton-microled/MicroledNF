using System.Data;
using System.Data.OleDb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Configuration;

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

            // Query to get pending RPS
            // Using actual column names from Access database
            var query = $@"
                SELECT TOP {batchSize} 
                    [{_options.PrimaryKeyColumn}],
                    [Numero_RPS] AS NumeroRps,
                    'A' AS Serie,
                    [Dt_emissao] AS DataEmissao,
                    [CNPJ] AS CnpjPrestador,
                    0 AS ImPrestador,
                    '' AS RazaoSocialPrestador,
                    '' AS CpfCnpjTomador,
                    '' AS NomeTomador,
                    [Valor] AS ValorServico,
                    0 AS ValorDeducao,
                    [Codigo_ISS] AS CodigoServico,
                    [Discriminacao],
                    0 AS AliquotaISS,
                    False AS ISSRetido,
                    'RPS' AS TipoRPS,
                    'N' AS StatusRPS,
                    'T' AS TributacaoRPS
                FROM [{_options.RpsTableName}]
                WHERE [{_options.StatusColumn}] = ?
                ORDER BY [{_options.PrimaryKeyColumn}]";

            using var command = new OleDbCommand(query, connection);
            // Try to convert PendingStatus to appropriate type
            // Access boolean columns typically use True/False or 0/-1
            object statusValue;
            if (bool.TryParse(_options.PendingStatus, out var boolValue))
            {
                statusValue = boolValue;
            }
            else if (int.TryParse(_options.PendingStatus, out var intValue))
            {
                statusValue = intValue;
            }
            else
            {
                // Try common boolean string representations
                var upperStatus = _options.PendingStatus.ToUpperInvariant();
                if (upperStatus == "N" || upperStatus == "FALSE" || upperStatus == "0" || upperStatus == "NO")
                {
                    statusValue = false;
                }
                else if (upperStatus == "S" || upperStatus == "TRUE" || upperStatus == "1" || upperStatus == "YES")
                {
                    statusValue = true;
                }
                else
                {
                    statusValue = _options.PendingStatus;
                }
            }
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
                        Id = Convert.ToInt32(reader[_options.PrimaryKeyColumn]),
                        Rps = rps
                    };
                    rpsRecords.Add(record);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error mapping RPS record with Id {Id}", reader[_options.PrimaryKeyColumn]);
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

            foreach (var record in records)
            {
                var updateQuery = $@"
                    UPDATE [{_options.RpsTableName}]
                    SET [{_options.StatusColumn}] = ?
                    WHERE [{_options.PrimaryKeyColumn}] = ?";

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

            foreach (var record in records)
            {
                var updateQuery = $@"
                    UPDATE [{_options.RpsTableName}]
                    SET [{_options.StatusColumn}] = ?
                    WHERE [{_options.PrimaryKeyColumn}] = ?";

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

        // Prestador (can use defaults from configuration or read from MDB)
        var cnpjPrestador = reader["CnpjPrestador"]?.ToString() ?? throw new InvalidOperationException("CnpjPrestador is required");
        var prestador = new ServiceProvider(
            CpfCnpj.CreateFromCnpj(cnpjPrestador),
            inscricaoPrestador,
            reader["RazaoSocialPrestador"]?.ToString() ?? "PRESTADOR",
            null,
            null
        );

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

        // Tomador (optional)
        ServiceCustomer? tomador = null;
        var cpfCnpjTomador = reader["CpfCnpjTomador"]?.ToString();
        if (!string.IsNullOrEmpty(cpfCnpjTomador))
        {
            var nomeTomador = reader["NomeTomador"]?.ToString();
            tomador = new ServiceCustomer(
                cpfCnpjTomador.Length == 11
                    ? CpfCnpj.CreateFromCpf(cpfCnpjTomador)
                    : CpfCnpj.CreateFromCnpj(cpfCnpjTomador),
                null,
                null,
                nomeTomador ?? "TOMADOR",
                null,
                null
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

        return rps;
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

