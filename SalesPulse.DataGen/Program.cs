using Microsoft.Data.SqlClient;

namespace SalesPulse.DataGen;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var server = @"CEMRE\SQLEXPRESS";
        var force = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--force")
                force = true;
            if (args[i] == "--server" && i + 1 < args.Length)
                server = args[++i];
        }

        var root = FindRepoRoot();
        var masterCs = $"Data Source={server};Initial Catalog=master;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;Connect Timeout=30";
        var dbCs = $"Data Source={server};Initial Catalog=SalesPulseDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;Connect Timeout=60";

        Console.WriteLine($"SQL Server: {server}");
        try
        {
            await using (var probe = new SqlConnection(masterCs))
            {
                await probe.OpenAsync();
                Console.WriteLine("Bağlantı başarılı.");
            }

            if (force)
                await DropDatabaseAsync(masterCs);

            var orders = await CountOrdersAsync(masterCs, dbCs);
            if (orders != BulkGenerator.OrderCount)
            {
                if (orders > 0)
                    await DropDatabaseAsync(masterCs);

                Console.WriteLine("Şema oluşturuluyor...");
                await ScriptRunner.ExecuteFileAsync(masterCs, dbCs, Path.Combine(root, "database", "01_schema.sql"));
                await GrowFilesAsync(masterCs, dbCs);

                await using var connection = await ScriptRunner.OpenDatabaseAsync(dbCs);
                await BulkGenerator.GenerateAsync(connection);
                await BulkGenerator.ValidateAsync(connection);
                await ScriptRunner.ExecuteAsync(connection, """
                    UPDATE STATISTICS dbo.Orders;
                    UPDATE STATISTICS dbo.OrderDetails;
                    UPDATE STATISTICS dbo.Customers;
                    UPDATE STATISTICS dbo.Payments;
                    UPDATE STATISTICS dbo.Products;
                    """);
            }
            else
            {
                Console.WriteLine("2.000.000 sipariş zaten var. Üretim atlandı. Sıfırdan üretmek için --force kullanın.");
            }

            Console.WriteLine("Saklı yordamlar yazılıyor...");
            await ScriptRunner.ExecuteFileAsync(masterCs, dbCs, Path.Combine(root, "database", "02_stored_procedures.sql"));

            await using var measureConnection = await ScriptRunner.OpenDatabaseAsync(dbCs);
            var indexed = await IndexExistsAsync(measureConnection);
            IReadOnlyList<ProbeResult> before;
            IReadOnlyList<ProbeResult> after;
            if (!indexed)
            {
                Console.WriteLine("İndeks öncesi ölçüm...");
                before = await PerformanceStudy.MeasureAsync(measureConnection, "Önce");
                Console.WriteLine("İndeksler oluşturuluyor...");
                await ScriptRunner.ExecuteFileAsync(masterCs, dbCs, Path.Combine(root, "database", "03_indexes.sql"));
                Console.WriteLine("İndeks sonrası ölçüm...");
                after = await PerformanceStudy.MeasureAsync(measureConnection, "Sonra");
            }
            else
            {
                Console.WriteLine("İndeksler zaten var. Yalnızca güncel süre ölçülüyor.");
                after = await PerformanceStudy.MeasureAsync(measureConnection, "Sonra");
                before = after;
            }

            var version = await ReadVersionAsync(measureConnection);
            var markdown = PerformanceStudy.ToMarkdown(before, after, version);
            var reportPath = Path.Combine(root, "database", "performance-results.md");
            await File.WriteAllTextAsync(reportPath, markdown);
            Console.WriteLine($"Rapor: {reportPath}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task DropDatabaseAsync(string masterConnectionString)
    {
        Console.WriteLine("SalesPulseDb siliniyor...");
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        await ScriptRunner.ExecuteAsync(connection, """
            IF DB_ID(N'SalesPulseDb') IS NOT NULL
            BEGIN
                ALTER DATABASE SalesPulseDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE SalesPulseDb;
            END
            """);
    }

    private static async Task<long> CountOrdersAsync(string masterConnectionString, string databaseConnectionString)
    {
        try
        {
            await using var master = new SqlConnection(masterConnectionString);
            await master.OpenAsync();
            await using var exists = new SqlCommand("SELECT CASE WHEN DB_ID(N'SalesPulseDb') IS NULL THEN 0 ELSE 1 END", master);
            if (Convert.ToInt32(await exists.ExecuteScalarAsync()) == 0)
                return 0;

            SqlConnection.ClearAllPools();
            await using var connection = new SqlConnection(databaseConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
                IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
                    SELECT CAST(0 AS bigint);
                ELSE
                    SELECT COUNT_BIG(*) FROM dbo.Orders;
                """, connection)
            {
                CommandTimeout = 0
            };
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        catch (SqlException)
        {
            return 0;
        }
    }

    private static async Task<bool> IndexExistsAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand("""
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_Orders_OrderDate_Status' AND object_id = OBJECT_ID(N'dbo.Orders')
            ) THEN 1 ELSE 0 END
            """, connection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 1;
    }

    private static async Task<string> ReadVersionAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand("SELECT @@VERSION", connection);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? "";
    }

    private static async Task GrowFilesAsync(string masterConnectionString, string databaseConnectionString)
    {
        try
        {
            await using var database = new SqlConnection(databaseConnectionString);
            await database.OpenAsync();
            var files = new List<(string Name, byte Type)>();
            await using (var command = new SqlCommand("SELECT name, type FROM sys.database_files", database))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    files.Add((reader.GetString(0), reader.GetByte(1)));
            }

            await using var master = new SqlConnection(masterConnectionString);
            await master.OpenAsync();
            foreach (var file in files)
            {
                var size = file.Type == 0 ? "4096MB" : "1024MB";
                var growth = file.Type == 0 ? "512MB" : "256MB";
                var name = file.Name.Replace("]", "]]", StringComparison.Ordinal);
                await ScriptRunner.ExecuteAsync(master,
                    $"ALTER DATABASE SalesPulseDb MODIFY FILE (NAME = [{name}], SIZE = {size}, FILEGROWTH = {growth});");
            }

            Console.WriteLine("Veritabanı dosyaları büyütüldü.");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Dosya boyutu ayarlanamadı, varsayılan büyüme kullanılacak: {exception.Message}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "database", "01_schema.sql")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("database klasörü bulunamadı.");
    }
}
