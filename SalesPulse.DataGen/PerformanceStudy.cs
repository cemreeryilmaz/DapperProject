using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SalesPulse.DataGen;

public static class PerformanceStudy
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    public static async Task<IReadOnlyList<ProbeResult>> MeasureAsync(SqlConnection connection, string phase)
    {
        await ScriptRunner.ExecuteAsync(connection, "SET STATISTICS IO ON; SET STATISTICS TIME ON;");
        try
        {
            await ClearProcedureCacheAsync(connection);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Prosedür önbelleği temizlenemedi: {exception.Message}");
        }

        var start = new DateTime(2025, 11, 1);
        var end = new DateTime(2026, 11, 6);
        var probes = new (string Name, string Sql, Action<SqlCommand> Bind)[]
        {
            (
                "Sipariş sayfası (tarih + şehir + durum, OFFSET/FETCH)",
                """
                SELECT /* SalesPulseProbeQ1 */
                    o.OrderNo,
                    c.FullName,
                    ci.Name,
                    o.OrderDate,
                    o.TotalAmount,
                    p.Method,
                    o.Status
                FROM dbo.Orders AS o
                INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
                INNER JOIN dbo.Cities AS ci ON ci.CityId = o.CityId
                INNER JOIN dbo.Payments AS p ON p.OrderId = o.OrderId
                WHERE o.OrderDate >= @StartDate
                  AND o.OrderDate < @EndDate
                  AND o.Status = @Status
                  AND o.CityId = @CityId
                ORDER BY o.OrderDate DESC, o.OrderId DESC
                OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY;
                """,
                command =>
                {
                    command.Parameters.Add("@StartDate", System.Data.SqlDbType.DateTime2).Value = start;
                    command.Parameters.Add("@EndDate", System.Data.SqlDbType.DateTime2).Value = end;
                    command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 20).Value = "Tamamlandı";
                    command.Parameters.Add("@CityId", System.Data.SqlDbType.Int).Value = 34;
                }
            ),
            (
                "Aylık satış eğilimi (GROUP BY)",
                """
                SELECT /* SalesPulseProbeQ2 */
                    YEAR(o.OrderDate) AS [Year],
                    MONTH(o.OrderDate) AS [Month],
                    COUNT_BIG(*) AS OrderCount,
                    SUM(o.TotalAmount) AS Revenue
                FROM dbo.Orders AS o
                WHERE o.OrderDate >= @StartDate
                  AND o.OrderDate < @EndDate
                GROUP BY YEAR(o.OrderDate), MONTH(o.OrderDate)
                ORDER BY [Year], [Month];
                """,
                command =>
                {
                    command.Parameters.Add("@StartDate", System.Data.SqlDbType.DateTime2).Value = start;
                    command.Parameters.Add("@EndDate", System.Data.SqlDbType.DateTime2).Value = end;
                }
            ),
            (
                "En yüksek cirolu müşteriler (JOIN, GROUP BY, HAVING)",
                """
                SELECT /* SalesPulseProbeQ3 */ TOP (20)
                    c.CustomerId,
                    c.FullName,
                    SUM(o.TotalAmount) AS Revenue,
                    COUNT_BIG(*) AS OrderCount
                FROM dbo.Orders AS o
                INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
                WHERE o.OrderDate >= @StartDate
                  AND o.OrderDate < @EndDate
                  AND o.Status <> N'İptal'
                GROUP BY c.CustomerId, c.FullName
                HAVING COUNT_BIG(*) >= 2
                ORDER BY Revenue DESC;
                """,
                command =>
                {
                    command.Parameters.Add("@StartDate", System.Data.SqlDbType.DateTime2).Value = start;
                    command.Parameters.Add("@EndDate", System.Data.SqlDbType.DateTime2).Value = end;
                }
            )
        };

        var results = new List<ProbeResult>();
        foreach (var probe in probes)
        {
            Console.WriteLine($"  Ölçülüyor ({phase}): {probe.Name}");
            await RunOnceAsync(connection, probe.Sql, probe.Bind);
            var messages = new StringBuilder();
            void Capture(object? sender, SqlInfoMessageEventArgs args) => messages.AppendLine(args.Message);
            connection.InfoMessage += Capture;
            var elapsed = await RunOnceAsync(connection, probe.Sql, probe.Bind);
            connection.InfoMessage -= Capture;

            var token = probe.Name.StartsWith("Sipariş", StringComparison.Ordinal) ? "SalesPulseProbeQ1"
                : probe.Name.StartsWith("Aylık", StringComparison.Ordinal) ? "SalesPulseProbeQ2"
                : "SalesPulseProbeQ3";
            var stats = await ReadStatsAsync(connection, token);
            var logicalReads = stats.LogicalReads > 0 ? stats.LogicalReads : ParseLogicalReads(messages.ToString());
            var operators = ExtractOperators(stats.PlanXml);
            results.Add(new ProbeResult(phase, probe.Name, token, elapsed, stats.ElapsedMs, stats.CpuMs, logicalReads, operators));
            Console.WriteLine($"    {elapsed.TotalMilliseconds:N0} ms | mantıksal okuma {logicalReads:N0} | {operators}");
        }

        return results;
    }

    public static string ToMarkdown(IReadOnlyList<ProbeResult> before, IReadOnlyList<ProbeResult> after, string sqlVersion)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# SalesPulse performans ölçümü");
        builder.AppendLine();
        builder.AppendLine($"Ölçüm zamanı: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", Turkish)}");
        builder.AppendLine();
        builder.AppendLine("Koşullar:");
        builder.AppendLine();
        builder.AppendLine("- Veri hacmi: 2.000.000 sipariş, 3.500.000 kalem, 100.000 müşteri.");
        builder.AppendLine("- Aralık: 1 Kasım 2025 (dahil) - 6 Kasım 2026 (hariç).");
        builder.AppendLine("- Sipariş sayfası sorgusu: İstanbul (`CityId = 34`), durum `Tamamlandı`, `OFFSET 0 FETCH 25`.");
        builder.AppendLine("- Her sorgu iki kez çalıştırıldı. Süre ve okuma, derleme etkisini ayırmak için ikinci çalıştırmadan alındı.");
        builder.AppendLine("- Önce ölçümünde yalnızca birincil anahtar ve tekillik kısıtlarının indeksleri vardı. Sonra ölçümünde `03_indexes.sql` indeksleri eklendi.");
        builder.AppendLine("- Mantıksal okuma `sys.dm_exec_query_stats.last_logical_reads` değeridir; istatistik mesajı yedek olarak kullanılır.");
        builder.AppendLine("- İptal hariç ciro kuralı müşteri sorgusunda uygulanır. Aylık eğilim sorgusu ham toplamı ölçer.");
        builder.AppendLine();
        builder.AppendLine("SQL Server:");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine(sqlVersion.Trim());
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("| Sorgu | Önce (ms) | Sonra (ms) | Önce okuma | Sonra okuma | Okuma farkı | Plan (sonra) |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |");

        for (var i = 0; i < before.Count; i++)
        {
            var left = before[i];
            var right = after[i];
            var beforeMs = Milliseconds(left);
            var afterMs = Milliseconds(right);
            var drop = left.LogicalReads - right.LogicalReads;
            builder.AppendLine($"| {left.Name} | {beforeMs.ToString("N0", Turkish)} | {afterMs.ToString("N0", Turkish)} | {left.LogicalReads.ToString("N0", Turkish)} | {right.LogicalReads.ToString("N0", Turkish)} | {drop.ToString("N0", Turkish)} | {right.Operators} |");
        }

        builder.AppendLine();
        builder.AppendLine("İndeksler ve gerekçeleri:");
        builder.AppendLine();
        builder.AppendLine("- `IX_Orders_OrderDate_Status` — tarih aralığı, aylık gruplama ve durum filtresi.");
        builder.AppendLine("- `IX_Orders_City_OrderDate` — şehir + tarih sayfalama.");
        builder.AppendLine("- `IX_Orders_Customer_OrderDate` — müşteri cirosu ve tekrar alışveriş oranı.");
        builder.AppendLine("- `IX_Customers_FullName` — sipariş no / müşteri ön ek araması.");
        builder.AppendLine("- `IX_OrderDetails_OrderId` ve `IX_OrderDetails_ProductId` — sipariş detayı ile ürün/kategori cirosu.");
        builder.AppendLine("- `IX_Products_CategoryId` — kategori kırılımı.");
        builder.AppendLine("- `IX_Payments_OrderId_Cover` — ödeme yöntemi için anahtar aramasını kapatan kapak indeks.");
        builder.AppendLine();
        builder.AppendLine("Önce plan özetleri:");
        builder.AppendLine();
        foreach (var result in before)
            builder.AppendLine($"- {result.Name}: {result.Operators}");

        return builder.ToString();
    }

    private static int Milliseconds(ProbeResult result)
    {
        if (result.ServerElapsedMs > 0)
            return (int)Math.Round(result.ServerElapsedMs);
        return (int)Math.Round(result.ClientElapsed.TotalMilliseconds);
    }

    private static async Task<TimeSpan> RunOnceAsync(SqlConnection connection, string sql, Action<SqlCommand> bind)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        bind(command);
        var watch = Stopwatch.StartNew();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
        }

        watch.Stop();
        return watch.Elapsed;
    }

    private static async Task<(long LogicalReads, double ElapsedMs, double CpuMs, string PlanXml)> ReadStatsAsync(SqlConnection connection, string token)
    {
        const string sql = """
            SELECT TOP (1)
                qs.last_logical_reads,
                qs.last_elapsed_time / 1000.0,
                qs.last_worker_time / 1000.0,
                CAST(qp.query_plan AS nvarchar(max))
            FROM sys.dm_exec_query_stats AS qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st
            CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) AS qp
            WHERE st.text LIKE @Token
              AND st.text NOT LIKE N'%sys.dm_exec_query_stats%'
            ORDER BY qs.last_execution_time DESC;
            """;

        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        command.Parameters.Add("@Token", System.Data.SqlDbType.NVarChar, 80).Value = "%" + token + "%";
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return (0, 0, 0, "");

        var reads = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
        var elapsed = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1));
        var cpu = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetValue(2));
        var plan = reader.IsDBNull(3) ? "" : reader.GetString(3);
        return (reads, elapsed, cpu, plan);
    }

    private static async Task ClearProcedureCacheAsync(SqlConnection connection)
    {
        await ScriptRunner.ExecuteAsync(connection, "ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;");
    }

    private static long ParseLogicalReads(string messages)
    {
        long total = 0;
        foreach (Match match in Regex.Matches(messages, @"(?<!lob )(logical reads|mantıksal okuma sayısı) (\d+)", RegexOptions.IgnoreCase))
            total += long.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return total;
    }

    private static string ExtractOperators(string planXml)
    {
        if (string.IsNullOrWhiteSpace(planXml))
            return "plan okunamadı";

        var names = Regex.Matches(planXml, "PhysicalOp=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .Take(8);
        var text = string.Join(", ", names);
        return string.IsNullOrWhiteSpace(text) ? "operatör yok" : text;
    }
}

public sealed record ProbeResult(
    string Phase,
    string Name,
    string Token,
    TimeSpan ClientElapsed,
    double ServerElapsedMs,
    double CpuMs,
    long LogicalReads,
    string Operators);
