using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace SalesPulse.DataGen;

public static class BulkGenerator
{
    public const int CustomerCount = 100_000;
    public const int ProductCount = 5_000;
    public const int OrderCount = 2_000_000;
    public const int ExpectedDetailCount = 3_500_000;

    private const int OrderBatchSize = 40_000;

    public static async Task GenerateAsync(SqlConnection connection)
    {
        if (Catalog.Cities.Length != 81)
            throw new InvalidOperationException("Şehir kataloğu 81 kayıt olmalı.");
        if (Catalog.Categories.Length != 30 || Catalog.ProductItems.Length != 30)
            throw new InvalidOperationException("Kategori kataloğu 30 kayıt olmalı.");

        var random = new Random(20261105);
        var totalWeight = Catalog.Cities.Sum(city => city.Weight);
        var cumulative = new int[Catalog.Cities.Length];
        var running = 0;
        for (var i = 0; i < Catalog.Cities.Length; i++)
        {
            running += Catalog.Cities[i].Weight;
            cumulative[i] = running;
        }

        Console.WriteLine("Referans veriler yazılıyor...");
        await WriteReferenceAsync(connection, random, cumulative, totalWeight);

        var customerCities = await LoadCustomerCitiesAsync(connection);
        var prices = await LoadPricesAsync(connection);
        var start = new DateTime(2024, 1, 1);
        var end = DateTime.Today < start ? new DateTime(2026, 11, 5) : DateTime.Today;
        var days = BuildDayPicker(start, end);

        Console.WriteLine($"Siparişler üretiliyor: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}");
        await WriteOrdersAsync(connection, random, customerCities, prices, days, end);
    }

    public static async Task ValidateAsync(SqlConnection connection)
    {
        Console.WriteLine("Kısıtlar doğrulanıyor...");
        foreach (var table in new[] { "dbo.Products", "dbo.Customers", "dbo.Orders", "dbo.OrderDetails", "dbo.Payments" })
        {
            await ScriptRunner.ExecuteAsync(connection, $"ALTER TABLE {table} WITH CHECK CHECK CONSTRAINT ALL;");
        }

        const string sql = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.Cities) AS Cities,
                (SELECT COUNT_BIG(*) FROM dbo.Categories) AS Categories,
                (SELECT COUNT_BIG(*) FROM dbo.Products) AS Products,
                (SELECT COUNT_BIG(*) FROM dbo.Customers) AS Customers,
                (SELECT COUNT_BIG(*) FROM dbo.Orders) AS Orders,
                (SELECT COUNT_BIG(*) FROM dbo.OrderDetails) AS Details,
                (SELECT COUNT_BIG(*) FROM dbo.Payments) AS Payments,
                (SELECT COUNT_BIG(*) FROM dbo.Orders AS o WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.OrderDetails AS d WHERE d.OrderId = o.OrderId)) AS OrdersWithoutLines,
                (SELECT COUNT_BIG(*)
                 FROM dbo.Orders AS o
                 INNER JOIN (
                     SELECT OrderId, SUM(LineTotal) AS LineSum
                     FROM dbo.OrderDetails
                     GROUP BY OrderId
                 ) AS d ON d.OrderId = o.OrderId
                 WHERE ABS(o.TotalAmount - d.LineSum) > 0.05) AS AmountMismatches;
            """;

        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        var cities = reader.GetInt64(0);
        var categories = reader.GetInt64(1);
        var products = reader.GetInt64(2);
        var customers = reader.GetInt64(3);
        var orders = reader.GetInt64(4);
        var details = reader.GetInt64(5);
        var payments = reader.GetInt64(6);
        var missingLines = reader.GetInt64(7);
        var mismatches = reader.GetInt64(8);

        Console.WriteLine($"Şehir {cities:N0} | Kategori {categories:N0} | Ürün {products:N0} | Müşteri {customers:N0}");
        Console.WriteLine($"Sipariş {orders:N0} | Kalem {details:N0} | Ödeme {payments:N0}");
        Console.WriteLine($"Kalemsiz sipariş {missingLines:N0} | Tutar uyuşmazlığı {mismatches:N0}");

        if (cities != 81 || categories != 30 || products != ProductCount || customers != CustomerCount
            || orders != OrderCount || details != ExpectedDetailCount || payments != OrderCount
            || missingLines != 0 || mismatches != 0)
        {
            throw new InvalidOperationException("Üretilen veri hedeflenen hacim veya tutar bütünlüğü ile uyuşmuyor.");
        }
    }

    private static async Task WriteReferenceAsync(SqlConnection connection, Random random, int[] cumulative, int totalWeight)
    {
        var cities = Table("CityId", "Name", "Region");
        foreach (var city in Catalog.Cities)
            cities.Rows.Add(city.CityId, city.Name, city.Region);
        await BulkAsync(connection, "dbo.Cities", cities);

        var categories = Table("CategoryId", "Name");
        for (var i = 0; i < Catalog.Categories.Length; i++)
            categories.Rows.Add(i + 1, Catalog.Categories[i]);
        await BulkAsync(connection, "dbo.Categories", categories);

        var products = Table("ProductId", "CategoryId", "Name", "UnitPrice");
        products.Columns["UnitPrice"]!.DataType = typeof(decimal);
        for (var productId = 1; productId <= ProductCount; productId++)
        {
            var categoryId = ((productId - 1) % 30) + 1;
            var items = Catalog.ProductItems[categoryId - 1];
            var brand = Catalog.Brands[(productId - 1) % Catalog.Brands.Length];
            var item = items[(productId / 30) % items.Length];
            var factor = 0.72m + (decimal)random.NextDouble() * 0.70m;
            var price = Math.Round(Catalog.BasePrice[categoryId] * factor, 2, MidpointRounding.AwayFromZero);
            products.Rows.Add(productId, categoryId, $"{brand} {item} {productId}", price);
        }

        await BulkAsync(connection, "dbo.Products", products);

        var customers = Table("CustomerId", "FullName", "Email", "CityId", "CreatedAt");
        customers.Columns["CreatedAt"]!.DataType = typeof(DateTime);
        var createdOrigin = new DateTime(2023, 1, 1);
        for (var customerId = 1; customerId <= CustomerCount; customerId++)
        {
            var first = Catalog.FirstNames[customerId % Catalog.FirstNames.Length];
            var last = Catalog.LastNames[(customerId / Catalog.FirstNames.Length) % Catalog.LastNames.Length];
            var cityIndex = PickWeighted(random, cumulative, totalWeight);
            var createdAt = createdOrigin.AddDays(random.Next(0, 360)).AddHours(random.Next(8, 20));
            customers.Rows.Add(customerId, $"{first} {last}", $"musteri{customerId}@salespulse.example", Catalog.Cities[cityIndex].CityId, createdAt);

            if (customers.Rows.Count == 25_000)
            {
                await BulkAsync(connection, "dbo.Customers", customers);
                customers.Rows.Clear();
            }
        }

        if (customers.Rows.Count > 0)
            await BulkAsync(connection, "dbo.Customers", customers);
    }

    private static async Task WriteOrdersAsync(
        SqlConnection connection,
        Random random,
        int[] customerCities,
        decimal[] prices,
        DayPicker days,
        DateTime today)
    {
        var orders = CreateOrderTable();
        var details = CreateDetailTable();
        var payments = CreatePaymentTable();
        var chosen = new int[4];
        var detailId = 0;
        var watch = Stopwatch.StartNew();

        for (var orderId = 1; orderId <= OrderCount; orderId++)
        {
            var customerId = PickCustomer(random);
            var day = days.Pick(random);
            var orderDate = day.AddHours(PickHour(random)).AddMinutes(random.Next(0, 60));
            var lineCount = LineCount(orderId);
            var filled = 0;
            var guard = 0;
            while (filled < lineCount && guard < 40)
            {
                guard++;
                var productId = PickProduct(random);
                var exists = false;
                for (var i = 0; i < filled; i++)
                {
                    if (chosen[i] == productId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    chosen[filled++] = productId;
            }

            while (filled < lineCount)
                chosen[filled++] = PickProduct(random);

            decimal total = 0;
            for (var i = 0; i < lineCount; i++)
            {
                var productId = chosen[i];
                var quantity = PickQuantity(random);
                var unitPrice = prices[productId];
                var lineTotal = Math.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
                total += lineTotal;
                detailId++;
                details.Rows.Add(detailId, orderId, productId, quantity, unitPrice, lineTotal);
            }

            var status = StatusFor(random, orderDate, today);
            orders.Rows.Add(orderId, $"SP{orderId:00000000}", customerId, customerCities[customerId], orderDate, total, status);
            payments.Rows.Add(orderId, orderId, PaymentMethod(random), total, orderDate);

            if (orders.Rows.Count == OrderBatchSize)
            {
                await FlushOrdersAsync(connection, orders, details, payments);
                Console.WriteLine($"  {orderId:N0} / {OrderCount:N0} sipariş  ({watch.Elapsed:hh\\:mm\\:ss})");
            }
        }

        if (orders.Rows.Count > 0)
            await FlushOrdersAsync(connection, orders, details, payments);

        if (detailId != ExpectedDetailCount)
            throw new InvalidOperationException($"Kalem sayısı {detailId:N0}; beklenen {ExpectedDetailCount:N0}.");

        Console.WriteLine($"Sipariş yüklemesi bitti ({watch.Elapsed:hh\\:mm\\:ss}).");
    }

    private static async Task FlushOrdersAsync(SqlConnection connection, DataTable orders, DataTable details, DataTable payments)
    {
        await BulkAsync(connection, "dbo.Orders", orders);
        await BulkAsync(connection, "dbo.OrderDetails", details);
        await BulkAsync(connection, "dbo.Payments", payments);
        orders.Rows.Clear();
        details.Rows.Clear();
        payments.Rows.Clear();
    }

    private static async Task<int[]> LoadCustomerCitiesAsync(SqlConnection connection)
    {
        var cities = new int[CustomerCount + 1];
        await using var command = new SqlCommand("SELECT CustomerId, CityId FROM dbo.Customers", connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            cities[reader.GetInt32(0)] = reader.GetInt32(1);
        return cities;
    }

    private static async Task<decimal[]> LoadPricesAsync(SqlConnection connection)
    {
        var prices = new decimal[ProductCount + 1];
        await using var command = new SqlCommand("SELECT ProductId, UnitPrice FROM dbo.Products", connection) { CommandTimeout = 0 };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            prices[reader.GetInt32(0)] = reader.GetDecimal(1);
        return prices;
    }

    private static async Task BulkAsync(SqlConnection connection, string table, DataTable data)
    {
        if (data.Rows.Count == 0)
            return;

        using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock, null)
        {
            DestinationTableName = table,
            BatchSize = data.Rows.Count,
            BulkCopyTimeout = 0,
            EnableStreaming = true
        };

        foreach (DataColumn column in data.Columns)
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulk.WriteToServerAsync(data);
    }

    private static DataTable Table(params string[] columns)
    {
        var table = new DataTable();
        foreach (var column in columns)
        {
            var type = column is "Name" or "Region" or "FullName" or "Email" ? typeof(string) : typeof(int);
            table.Columns.Add(column, type);
        }

        return table;
    }

    private static DataTable CreateOrderTable()
    {
        var table = new DataTable();
        table.Columns.Add("OrderId", typeof(int));
        table.Columns.Add("OrderNo", typeof(string));
        table.Columns.Add("CustomerId", typeof(int));
        table.Columns.Add("CityId", typeof(int));
        table.Columns.Add("OrderDate", typeof(DateTime));
        table.Columns.Add("TotalAmount", typeof(decimal));
        table.Columns.Add("Status", typeof(string));
        return table;
    }

    private static DataTable CreateDetailTable()
    {
        var table = new DataTable();
        table.Columns.Add("OrderDetailId", typeof(int));
        table.Columns.Add("OrderId", typeof(int));
        table.Columns.Add("ProductId", typeof(int));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("UnitPrice", typeof(decimal));
        table.Columns.Add("LineTotal", typeof(decimal));
        return table;
    }

    private static DataTable CreatePaymentTable()
    {
        var table = new DataTable();
        table.Columns.Add("PaymentId", typeof(int));
        table.Columns.Add("OrderId", typeof(int));
        table.Columns.Add("Method", typeof(string));
        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("PaidAt", typeof(DateTime));
        return table;
    }

    private static int PickWeighted(Random random, int[] cumulative, int total)
    {
        var ticket = random.Next(total);
        var low = 0;
        var high = cumulative.Length - 1;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (cumulative[mid] <= ticket)
                low = mid + 1;
            else
                high = mid;
        }

        return low;
    }

    private static int PickCustomer(Random random)
    {
        var roll = random.Next(100);
        if (roll < 42)
            return random.Next(1, 15_001);
        if (roll < 78)
            return random.Next(15_001, 50_001);
        return random.Next(50_001, CustomerCount + 1);
    }

    private static int PickProduct(Random random)
    {
        var id = (int)(Math.Pow(random.NextDouble(), 2.15) * ProductCount) + 1;
        if (id < 1)
            return 1;
        if (id > ProductCount)
            return ProductCount;
        return id;
    }

    private static int PickQuantity(Random random)
    {
        var roll = random.Next(10);
        if (roll < 6)
            return 1;
        if (roll < 9)
            return 2;
        return 3;
    }

    private static int LineCount(int orderId)
    {
        var mod = orderId % 100;
        if (mod < 45)
            return 1;
        if (mod < 83)
            return 2;
        if (mod < 97)
            return 3;
        return 4;
    }

    private static int PickHour(Random random)
    {
        ReadOnlySpan<int> hours =
        [
            9, 10, 10, 11, 11, 11, 12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14,
            15, 15, 16, 16, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20, 20, 20, 20, 21, 21, 21, 22, 22
        ];
        return hours[random.Next(hours.Length)];
    }

    private static string StatusFor(Random random, DateTime orderDate, DateTime today)
    {
        var roll = random.NextDouble();
        var age = (today.Date - orderDate.Date).TotalDays;
        if (age <= 7)
        {
            if (roll < 0.30) return "Tamamlandı";
            if (roll < 0.65) return "Kargoda";
            if (roll < 0.90) return "Beklemede";
            if (roll < 0.97) return "İptal";
            return "İade";
        }

        if (age <= 21)
        {
            if (roll < 0.70) return "Tamamlandı";
            if (roll < 0.85) return "Kargoda";
            if (roll < 0.90) return "Beklemede";
            if (roll < 0.97) return "İptal";
            return "İade";
        }

        if (roll < 0.90) return "Tamamlandı";
        if (roll < 0.97) return "İptal";
        return "İade";
    }

    private static string PaymentMethod(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.54) return "Kredi Kartı";
        if (roll < 0.70) return "Banka Kartı";
        if (roll < 0.83) return "Havale/EFT";
        if (roll < 0.93) return "Kapıda Ödeme";
        return "Cüzdan";
    }

    private static DayPicker BuildDayPicker(DateTime start, DateTime end)
    {
        var count = (end - start).Days + 1;
        var cumulative = new double[count];
        double total = 0;
        for (var i = 0; i < count; i++)
        {
            total += Weight(start.AddDays(i));
            cumulative[i] = total;
        }

        return new DayPicker(start, cumulative, total);
    }

    private static double Weight(DateTime day)
    {
        var weight = day.Month switch
        {
            1 => 0.82,
            2 => 0.78,
            3 => 0.90,
            4 => 0.95,
            5 => 1.02,
            6 => 1.08,
            7 => 1.12,
            8 => 1.10,
            9 => 1.00,
            10 => 1.15,
            11 => 1.48,
            12 => 1.62,
            _ => 1
        };

        if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            weight *= 1.22;
        if (day.Day is 1 or 15)
            weight *= 1.12;
        if (day.Month == 11 && day.Day == 11)
            weight *= 2.6;
        if (day.Month == 11 && day.DayOfWeek == DayOfWeek.Friday && day.Day is >= 23 and <= 29)
            weight *= 3.2;
        if (day.Month == 12 && day.Day >= 15)
            weight *= 1.35;
        return weight;
    }

    private readonly record struct DayPicker(DateTime Start, double[] Cumulative, double Total)
    {
        public DateTime Pick(Random random)
        {
            var target = random.NextDouble() * Total;
            var low = 0;
            var high = Cumulative.Length - 1;
            while (low < high)
            {
                var mid = (low + high) / 2;
                if (Cumulative[mid] < target)
                    low = mid + 1;
                else
                    high = mid;
            }

            return Start.AddDays(low);
        }
    }
}
