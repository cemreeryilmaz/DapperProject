using System.Text;
using Microsoft.Data.SqlClient;

namespace SalesPulse.DataGen;

public static class ScriptRunner
{
    public static async Task ExecuteFileAsync(string masterConnectionString, string databaseConnectionString, string path)
    {
        var sql = await File.ReadAllTextAsync(path, Encoding.UTF8);
        await using var master = new SqlConnection(masterConnectionString);
        await master.OpenAsync();

        SqlConnection? database = null;
        try
        {
            foreach (var batch in SplitBatches(sql))
            {
                var onMaster = ContainsDatabaseCommand(batch);
                if (onMaster)
                {
                    await ExecuteAsync(master, batch);
                    continue;
                }

                database ??= await OpenDatabaseAsync(databaseConnectionString);
                await ExecuteAsync(database, batch);
            }
        }
        finally
        {
            if (database != null)
                await database.DisposeAsync();
        }
    }

    public static async Task<SqlConnection> OpenDatabaseAsync(string connectionString)
    {
        SqlConnection.ClearAllPools();
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task ExecuteAsync(SqlConnection connection, string sql, int timeoutSeconds = 0)
    {
        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = timeoutSeconds
        };
        await command.ExecuteNonQueryAsync();
    }

    public static IEnumerable<string> SplitBatches(string sql)
    {
        var builder = new StringBuilder();
        using var reader = new StringReader(sql);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                var batch = builder.ToString().Trim();
                if (batch.Length > 0)
                    yield return batch;
                builder.Clear();
                continue;
            }

            builder.AppendLine(line);
        }

        var last = builder.ToString().Trim();
        if (last.Length > 0)
            yield return last;
    }

    private static bool ContainsDatabaseCommand(string batch)
    {
        return batch.Contains("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)
            || batch.Contains("ALTER DATABASE", StringComparison.OrdinalIgnoreCase)
            || batch.Contains("DROP DATABASE", StringComparison.OrdinalIgnoreCase);
    }
}
