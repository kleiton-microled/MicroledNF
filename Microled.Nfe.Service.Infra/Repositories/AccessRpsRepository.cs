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
    private readonly ILogger<AccessRpsRepository> _logger;

    public AccessRpsRepository(
        IOptions<AccessDatabaseOptions> options,
        ILogger<AccessRpsRepository> logger)
    {
        _options = options.Value;
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
            // TODO: Adjust column names based on actual Access database schema
            var query = $@"
                SELECT TOP {batchSize} 
                    [{_options.PrimaryKeyColumn}],
                    [NumeroRps],
                    [Serie],
                    [DataEmissao],
                    [CnpjPrestador],
                    [ImPrestador],
                    [RazaoSocialPrestador],
                    [CpfCnpjTomador],
                    [NomeTomador],
                    [ValorServico],
                    [ValorDeducao],
                    [CodigoServico],
                    [Discriminacao],
                    [AliquotaISS],
                    [ISSRetido],
                    [TipoRPS],
                    [StatusRPS],
                    [TributacaoRPS]
                FROM [{_options.RpsTableName}]
                WHERE [{_options.StatusColumn}] = ?
                ORDER BY [{_options.PrimaryKeyColumn}]";

            using var command = new OleDbCommand(query, connection);
            command.Parameters.AddWithValue("@Status", _options.PendingStatus);

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
                command.Parameters.AddWithValue("@Status", _options.SentStatus);
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

    private string BuildConnectionString(string databasePath)
    {
        // Use ACE.OLEDB.12.0 for .MDB files (Access 2007+)
        // For older .MDB files, may need to use Microsoft.Jet.OLEDB.4.0
        return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Persist Security Info=False;";
    }

    private Rps MapToRps(OleDbDataReader reader)
    {
        // TODO: Adjust column names and data types based on actual Access database schema
        // This is a template that should be adjusted to match your actual database structure

        var numeroRps = Convert.ToInt64(reader["NumeroRps"]);
        var serieRps = reader["Serie"]?.ToString() ?? "A";
        var inscricaoPrestador = Convert.ToInt64(reader["ImPrestador"]);

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

