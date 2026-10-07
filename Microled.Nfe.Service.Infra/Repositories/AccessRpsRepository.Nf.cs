using System.Data;
using System.Data.OleDb;
using Microsoft.Extensions.Logging;

namespace Microled.Nfe.Service.Infra.Repositories;

/// <summary>
/// Gravacao da tabela NF (notas emitidas) a partir do RPS de origem, apos autorizacao da prefeitura.
/// </summary>
public partial class AccessRpsRepository
{
    /// <summary>Colunas do RPS que nao sao copiadas por nome para a NF (tem tratamento proprio ou nao se aplicam).</summary>
    private static readonly HashSet<string> NfColunasNaoCopiadas = new(StringComparer.OrdinalIgnoreCase)
    {
        "index", "numero", "dt_emissao", "cod_cli", "nome_cli", "Pago", "dt_pgto", "valor_depositado"
    };

    private sealed record AccessColumnInfo(string Name, OleDbType Type, int? MaxLength);

    public async Task<AccessNfInsertResult> InsertNfFromRpsAsync(
        AccessNfEmitida nota,
        CancellationToken cancellationToken)
    {
        // NF.numero e Text(6): grava o numero da NFS-e sem zeros a esquerda (ex.: 00300287 -> 300287).
        if (!long.TryParse(nota.NumeroNota?.Trim(), out var numeroNotaValue)
            || !int.TryParse(nota.NumeroRps?.Trim(), out var numeroRpsValue))
        {
            _logger.LogWarning(
                "NF nao gravada: numero da nota/RPS invalido. NumeroNota={NumeroNota}, NumeroRps={NumeroRps}",
                nota.NumeroNota,
                nota.NumeroRps);
            return AccessNfInsertResult.Skipped;
        }

        var numeroNf = numeroNotaValue.ToString();
        var connectionString = BuildConnectionString(_options.DatabasePath);
        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var nfColumns = GetColumnInfo(connection, _options.NfTableName);
        if (nfColumns.Count == 0)
        {
            _logger.LogWarning("NF nao gravada: tabela {Table} nao encontrada no Access.", _options.NfTableName);
            return AccessNfInsertResult.Skipped;
        }

        if (nfColumns.TryGetValue("numero", out var numeroColumn)
            && numeroColumn.MaxLength is { } maxNumero
            && numeroNf.Length > maxNumero)
        {
            _logger.LogError(
                "NF nao gravada: numero da NFS-e {Numero} excede o tamanho da coluna {Table}.numero ({Max}).",
                numeroNf,
                _options.NfTableName,
                maxNumero);
            return AccessNfInsertResult.Skipped;
        }

        if (await NfExistsAsync(connection, numeroNf, cancellationToken))
        {
            _logger.LogInformation("NF {Numero} ja existe na tabela {Table}; nada a gravar.", numeroNf, _options.NfTableName);
            return AccessNfInsertResult.AlreadyExists;
        }

        var rpsRow = await LoadRpsRowAsync(connection, numeroRpsValue, cancellationToken);
        if (rpsRow is null)
        {
            _logger.LogInformation(
                "NF {Numero} nao gravada: RPS {NumeroRps} nao encontrado na tabela {Table} (nota emitida fora do Access).",
                numeroNf,
                numeroRpsValue,
                _options.RpsTableName);
            return AccessNfInsertResult.RpsNotFound;
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Colunas com o mesmo nome no RPS e na NF: valor, valor_liquido, vencimento, BC, IR, IF, PIS,
        // COFINS, CSLL, discr5..discr10, lista, foi, dt1/P1..dt3/P3, Contrato.
        foreach (DataColumn column in rpsRow.Table.Columns)
        {
            if (!NfColunasNaoCopiadas.Contains(column.ColumnName) && nfColumns.ContainsKey(column.ColumnName))
            {
                values[column.ColumnName] = rpsRow[column];
            }
        }

        values["numero"] = numeroNf;
        values["dt_emissao"] = nota.DataEmissao?.Date ?? GetRpsValue(rpsRow, "Dt_emissao") ?? DateTime.Today;
        values["discr1"] = GetRpsValue(rpsRow, "Discriminacao");
        values["Pago"] = false;
        values["valor_depositado"] = 0d;

        var cnpj = Convert.ToString(GetRpsValue(rpsRow, "CNPJ"));
        var cliente = await FindClienteByCnpjAsync(connection, cnpj, cancellationToken);
        if (cliente is not null)
        {
            values["cod_cli"] = cliente.Value.Codigo;
            values["nome_cli"] = cliente.Value.Nome;
        }
        else
        {
            _logger.LogWarning(
                "NF {Numero}: CNPJ {Cnpj} nao encontrado na tabela {Table}; cod_cli/nome_cli ficarao vazios.",
                numeroNf,
                cnpj,
                _options.ClientesTableName);
        }

        // Booleanos NOT NULL da NF (lista/foi) nao podem ir nulos.
        foreach (var info in nfColumns.Values.Where(c => c.Type == OleDbType.Boolean))
        {
            if (!values.TryGetValue(info.Name, out var current) || current is null or DBNull)
            {
                values[info.Name] = false;
            }
        }

        var insertColumns = values.Keys.Where(nfColumns.ContainsKey).ToList();
        var sql = $"INSERT INTO [{_options.NfTableName}] ({string.Join(", ", insertColumns.Select(c => $"[{nfColumns[c].Name}]"))}) " +
                  $"VALUES ({string.Join(", ", insertColumns.Select(_ => "?"))})";

        using var command = new OleDbCommand(sql, connection);
        foreach (var column in insertColumns)
        {
            var info = nfColumns[column];
            command.Parameters.Add(new OleDbParameter
            {
                OleDbType = info.Type,
                Value = ConvertForColumn(values[column], info, numeroNf)
            });
        }

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "NF {Numero} gravada na tabela {Table} a partir do RPS {NumeroRps}. Colunas={Columns}",
            numeroNf,
            _options.NfTableName,
            numeroRpsValue,
            string.Join(", ", insertColumns));
        return AccessNfInsertResult.Inserted;
    }

    public async Task<bool> UpdateNfPagamentoAsync(
        string numeroNota,
        bool pago,
        DateTime? dataPagamento,
        decimal? valorDepositado,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(numeroNota?.Trim(), out var numeroNotaValue))
        {
            throw new ArgumentException("Numero da NFS-e invalido.", nameof(numeroNota));
        }

        // Mesma regra da gravacao: NF.numero sem zeros a esquerda.
        var numeroNf = numeroNotaValue.ToString();
        var connectionString = BuildConnectionString(_options.DatabasePath);
        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Desmarcar "pago" limpa data e valor, como o sistema faz na nota (NotaFiscal.SetPagamento).
        using var command = new OleDbCommand(
            $"UPDATE [{_options.NfTableName}] SET [Pago] = ?, [dt_pgto] = ?, [valor_depositado] = ? WHERE [numero] = ?",
            connection);
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Boolean, Value = pago });
        command.Parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.Date,
            Value = pago && dataPagamento.HasValue ? dataPagamento.Value.Date : DBNull.Value
        });
        command.Parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.Double,
            Value = pago && valorDepositado.HasValue
                ? (double)Math.Round(valorDepositado.Value, 2, MidpointRounding.AwayFromZero)
                : 0d
        });
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.VarWChar, Value = numeroNf });

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation(
            "Pagamento da NF {Numero} atualizado no Access: Pago={Pago}, dt_pgto={Data}, valor_depositado={Valor}, Linhas={Affected}",
            numeroNf,
            pago,
            dataPagamento?.ToString("yyyy-MM-dd"),
            valorDepositado,
            affected);
        return affected > 0;
    }

    private async Task<bool> NfExistsAsync(OleDbConnection connection, string numeroNf, CancellationToken cancellationToken)
    {
        using var command = new OleDbCommand($"SELECT COUNT(*) FROM [{_options.NfTableName}] WHERE [numero] = ?", connection);
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.VarWChar, Value = numeroNf });
        var count = await command.ExecuteScalarAsync(cancellationToken);
        return count is not null and not DBNull && Convert.ToInt32(count) > 0;
    }

    /// <summary>Ultimo registro do RPS com o numero informado (o mais recente, caso o numero se repita).</summary>
    private async Task<DataRow?> LoadRpsRowAsync(OleDbConnection connection, int numeroRps, CancellationToken cancellationToken)
    {
        var connectionString = BuildConnectionString(_options.DatabasePath);
        var availableColumns = await GetAvailableColumnsAsync(connectionString, _options.RpsTableName, cancellationToken);
        var numeroColumn = new[] { "Numero_RPS", "NumeroRps", "NumeroRPS" }
            .FirstOrDefault(c => availableColumns.Any(ac => string.Equals(ac, c, StringComparison.OrdinalIgnoreCase)));
        if (numeroColumn is null)
        {
            return null;
        }

        var primaryKeyColumn = ResolveRequiredColumnName(
            availableColumns,
            new[] { _options.PrimaryKeyColumn, "index", "Id", "ID" },
            "PrimaryKeyColumn");

        using var command = new OleDbCommand(
            $"SELECT TOP 1 * FROM [{_options.RpsTableName}] WHERE [{numeroColumn}] = ? ORDER BY [{primaryKeyColumn}] DESC",
            connection);
        command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Integer, Value = numeroRps });

        using var adapter = new OleDbDataAdapter(command);
        var table = new DataTable();
        adapter.Fill(table);
        return table.Rows.Count > 0 ? table.Rows[0] : null;
    }

    private async Task<(string Codigo, string? Nome)?> FindClienteByCnpjAsync(
        OleDbConnection connection,
        string? cnpj,
        CancellationToken cancellationToken)
    {
        var digits = new string((cnpj ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0 || GetColumnInfo(connection, _options.ClientesTableName).Count == 0)
        {
            return null;
        }

        // CNPJ pode estar gravado com ou sem mascara: compara somente os digitos.
        using var command = new OleDbCommand(
            $"SELECT [codigo], [nome_pop], [cnpj] FROM [{_options.ClientesTableName}]",
            connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var rowDigits = new string((Convert.ToString(reader["cnpj"]) ?? string.Empty).Where(char.IsDigit).ToArray());
            if (rowDigits == digits)
            {
                return (Convert.ToString(reader["codigo"]) ?? string.Empty, Convert.ToString(reader["nome_pop"]));
            }
        }

        return null;
    }

    private static Dictionary<string, AccessColumnInfo> GetColumnInfo(OleDbConnection connection, string tableName)
    {
        var result = new Dictionary<string, AccessColumnInfo>(StringComparer.OrdinalIgnoreCase);
        var schema = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object?[] { null, null, tableName, null });
        if (schema is null)
        {
            return result;
        }

        foreach (DataRow row in schema.Rows)
        {
            var name = Convert.ToString(row["COLUMN_NAME"]) ?? string.Empty;
            var type = (OleDbType)Convert.ToInt32(row["DATA_TYPE"]);
            int? maxLength = row["CHARACTER_MAXIMUM_LENGTH"] is long len and > 0 and < int.MaxValue ? (int)len : null;
            result[name] = new AccessColumnInfo(name, type, maxLength);
        }

        return result;
    }

    private static object? GetRpsValue(DataRow row, string column)
    {
        var match = row.Table.Columns.Cast<DataColumn>()
            .FirstOrDefault(c => string.Equals(c.ColumnName, column, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return null;
        }

        var value = row[match];
        return value is DBNull ? null : value;
    }

    private object ConvertForColumn(object? value, AccessColumnInfo column, string numeroNf)
    {
        if (value is null or DBNull)
        {
            return DBNull.Value;
        }

        switch (column.Type)
        {
            case OleDbType.WChar:
            case OleDbType.VarWChar:
            case OleDbType.LongVarWChar:
            case OleDbType.Char:
            case OleDbType.VarChar:
            case OleDbType.LongVarChar:
                var text = Convert.ToString(value) ?? string.Empty;
                if (column.MaxLength is { } max && text.Length > max)
                {
                    _logger.LogWarning(
                        "NF {Numero}: valor de {Column} truncado de {Length} para {Max} caracteres.",
                        numeroNf,
                        column.Name,
                        text.Length,
                        max);
                    text = text[..max];
                }

                return text;
            case OleDbType.Date:
            case OleDbType.DBDate:
            case OleDbType.DBTimeStamp:
                return Convert.ToDateTime(value);
            case OleDbType.Boolean:
                return Convert.ToBoolean(value);
            case OleDbType.Integer:
            case OleDbType.SmallInt:
            case OleDbType.UnsignedTinyInt:
                return Convert.ToInt32(value);
            default:
                return Convert.ToDouble(value);
        }
    }
}
