using Dapper;
using DapperApp.Web.Context;
using DapperApp.Web.Models.Sales;

namespace DapperApp.Web.Services.Sales
{
    public class SalesIntelligenceService : ISalesIntelligenceService
    {
        private readonly DapperDbContext _context;

        public SalesIntelligenceService(DapperDbContext context)
        {
            _context = context;
        }

        public async Task<SalesIntelligenceViewModel> GetAsync(DateWindow window)
        {
            var previous = window.Previous();
            var currentTask = ReadWindowAsync(window);
            var previousTask = ReadWindowAsync(previous);
            var ordersTask = ReadOrdersAsync(window);
            var monthsTask = ReadCategoryMonthsAsync(window);
            var productsTask = ReadProductsAsync(window);
            await Task.WhenAll(currentTask, previousTask, ordersTask, monthsTask, productsTask);
            var current = currentTask.Result;
            var prior = previousTask.Result;
            var orders = ordersTask.Result;
            var months = monthsTask.Result;
            var products = productsTask.Result;
            var topCategories = TopCategoryNames(months);
            var matrix = BuildMatrix(window, months.Where(item => topCategories.Contains(item.CategoryName)).ToList());

            return new SalesIntelligenceViewModel
            {
                StartDate = window.Start,
                EndDate = window.EndInclusive,
                PreviousStart = previous.Start,
                PreviousEnd = previous.EndInclusive,
                Current = current.Period,
                Previous = prior.Period,
                CurrentRepeat = current.Repeat,
                PreviousRepeat = prior.Repeat,
                TopCustomers = orders.Customers,
                Cities = orders.Cities,
                MatrixMonths = matrix.Months,
                Matrix = matrix.Rows,
                TopProducts = products
            };
        }

        private async Task<(PeriodMetricsDto Period, RepeatRateDto Repeat)> ReadWindowAsync(DateWindow window)
        {
            string query = @"WITH PerCustomer AS
                             (
                                 SELECT CustomerId,
                                        COUNT_BIG(*) AS OrderCount,
                                        SUM(CASE WHEN Status <> N'İptal' THEN 1 ELSE 0 END) AS PaidCount,
                                        SUM(CASE WHEN Status <> N'İptal' THEN TotalAmount ELSE 0 END) AS Revenue
                                 FROM Orders
                                 WHERE OrderDate >= @StartDate AND OrderDate < @EndDate
                                 GROUP BY CustomerId
                             )
                             SELECT CAST(ISNULL(SUM(OrderCount), 0) AS bigint) AS TotalOrders,
                                    CAST(ISNULL(SUM(Revenue), 0) AS decimal(18,2)) AS TotalRevenue,
                                    CAST(COUNT_BIG(*) AS int) AS ActiveCustomers,
                                    CAST(ISNULL(SUM(Revenue) / NULLIF(SUM(PaidCount), 0), 0) AS decimal(18,2)) AS AverageBasket,
                                    CAST(ISNULL(SUM(CASE WHEN PaidCount > 0 THEN 1 ELSE 0 END), 0) AS bigint) AS PayingCustomers,
                                    CAST(ISNULL(SUM(CASE WHEN PaidCount >= 2 THEN 1 ELSE 0 END), 0) AS bigint) AS RepeatCustomers,
                                    CAST(100.0 * ISNULL(SUM(CASE WHEN PaidCount >= 2 THEN 1 ELSE 0 END), 0)
                                         / NULLIF(SUM(CASE WHEN PaidCount > 0 THEN 1 ELSE 0 END), 0) AS decimal(6,2)) AS RepeatRate
                             FROM PerCustomer";

            using var connection = _context.CreateConnection();
            var row = await connection.QueryFirstAsync<WindowRow>(query, Parameters(window));
            return (
                new PeriodMetricsDto
                {
                    TotalOrders = row.TotalOrders,
                    TotalRevenue = row.TotalRevenue,
                    ActiveCustomers = row.ActiveCustomers,
                    AverageBasket = row.AverageBasket
                },
                new RepeatRateDto
                {
                    ActiveCustomers = row.PayingCustomers,
                    RepeatCustomers = row.RepeatCustomers,
                    RepeatRate = row.RepeatRate
                });
        }

        private async Task<(List<TopCustomerDto> Customers, List<CitySalesDto> Cities)> ReadOrdersAsync(DateWindow window)
        {
            string query = @"SELECT TOP 20 c.CustomerId, c.FullName AS CustomerName, ci.Name AS CityName,
                                    COUNT_BIG(*) AS OrderCount,
                                    CAST(SUM(o.TotalAmount) AS decimal(18,2)) AS Revenue,
                                    CAST(AVG(o.TotalAmount) AS decimal(18,2)) AS AverageBasket
                             FROM Orders o
                             INNER JOIN Customers c ON c.CustomerId = o.CustomerId
                             INNER JOIN Cities ci ON ci.CityId = o.CityId
                             WHERE o.OrderDate >= @StartDate AND o.OrderDate < @EndDate AND o.Status <> N'İptal'
                             GROUP BY c.CustomerId, c.FullName, ci.Name
                             HAVING COUNT_BIG(*) >= 2
                             ORDER BY Revenue DESC;

                             SELECT TOP 10 ci.Name AS CityName,
                                    CAST(SUM(o.TotalAmount) AS decimal(18,2)) AS Revenue,
                                    COUNT_BIG(*) AS OrderCount,
                                    CAST(AVG(o.TotalAmount) AS decimal(18,2)) AS AverageBasket
                             FROM Orders o
                             INNER JOIN Cities ci ON ci.CityId = o.CityId
                             WHERE o.OrderDate >= @StartDate AND o.OrderDate < @EndDate AND o.Status <> N'İptal'
                             GROUP BY ci.CityId, ci.Name
                             HAVING COUNT_BIG(*) >= 50
                             ORDER BY Revenue DESC;";

            using var connection = _context.CreateConnection();
            using var grid = await connection.QueryMultipleAsync(query, Parameters(window));
            var customers = (await grid.ReadAsync<TopCustomerDto>()).ToList();
            var cities = (await grid.ReadAsync<CitySalesDto>()).ToList();
            return (customers, cities);
        }

        private async Task<List<CategoryMonthDto>> ReadCategoryMonthsAsync(DateWindow window)
        {
            string query = @"SELECT c.Name AS CategoryName, YEAR(o.OrderDate) AS [Year], MONTH(o.OrderDate) AS [Month],
                                    CAST(SUM(od.LineTotal) AS decimal(18,2)) AS Revenue,
                                    CAST(SUM(od.Quantity) AS bigint) AS Quantity
                             FROM OrderDetails od
                             INNER JOIN Orders o ON o.OrderId = od.OrderId
                             INNER JOIN Products p ON p.ProductId = od.ProductId
                             INNER JOIN Categories c ON c.CategoryId = p.CategoryId
                             WHERE o.OrderDate >= @StartDate AND o.OrderDate < @EndDate AND o.Status <> N'İptal'
                             GROUP BY c.CategoryId, c.Name, YEAR(o.OrderDate), MONTH(o.OrderDate)";

            using var connection = _context.CreateConnection();
            var rows = await connection.QueryAsync<CategoryMonthDto>(query, Parameters(window));
            return rows.ToList();
        }

        private async Task<List<ProductRankDto>> ReadProductsAsync(DateWindow window)
        {
            string query = @"SELECT TOP 10 p.Name AS ProductName, c.Name AS CategoryName,
                                    CAST(SUM(od.Quantity) AS bigint) AS QuantitySold,
                                    CAST(SUM(od.LineTotal) AS decimal(18,2)) AS Revenue
                             FROM OrderDetails od
                             INNER JOIN Orders o ON o.OrderId = od.OrderId
                             INNER JOIN Products p ON p.ProductId = od.ProductId
                             INNER JOIN Categories c ON c.CategoryId = p.CategoryId
                             WHERE o.OrderDate >= @StartDate AND o.OrderDate < @EndDate AND o.Status <> N'İptal'
                             GROUP BY p.ProductId, p.Name, c.Name
                             ORDER BY Revenue DESC";

            using var connection = _context.CreateConnection();
            var rows = await connection.QueryAsync<ProductRankDto>(query, Parameters(window));
            return rows.ToList();
        }

        private static HashSet<string> TopCategoryNames(List<CategoryMonthDto> months)
        {
            return months
                .GroupBy(item => item.CategoryName)
                .Select(group => new { Name = group.Key, Total = group.Sum(item => item.Revenue) })
                .OrderByDescending(item => item.Total)
                .Take(8)
                .Select(item => item.Name)
                .ToHashSet(StringComparer.CurrentCulture);
        }

        private static DynamicParameters Parameters(DateWindow window)
        {
            var parameters = new DynamicParameters();
            parameters.Add("StartDate", window.Start);
            parameters.Add("EndDate", window.EndExclusive);
            return parameters;
        }

        private static (List<string> Months, List<CategoryMatrixRow> Rows) BuildMatrix(DateWindow window, List<CategoryMonthDto> source)
        {
            var months = new List<(int Year, int Month)>();
            var cursor = new DateTime(window.Start.Year, window.Start.Month, 1);
            var last = window.EndExclusive.AddDays(-1);
            var limit = new DateTime(last.Year, last.Month, 1);
            while (cursor <= limit && months.Count < 18)
            {
                months.Add((cursor.Year, cursor.Month));
                cursor = cursor.AddMonths(1);
            }

            var rows = source
                .GroupBy(item => item.CategoryName)
                .Select(group =>
                {
                    var values = new decimal[months.Count];
                    for (var i = 0; i < months.Count; i++)
                    {
                        var point = group.FirstOrDefault(item => item.Year == months[i].Year && item.Month == months[i].Month);
                        values[i] = point?.Revenue ?? 0;
                    }

                    return new CategoryMatrixRow
                    {
                        CategoryName = group.Key,
                        Values = values,
                        Total = values.Sum()
                    };
                })
                .OrderByDescending(row => row.Total)
                .ToList();

            return (months.Select(item => SalesFormat.MonthLabel(item.Year, item.Month)).ToList(), rows);
        }

        private sealed class WindowRow
        {
            public long TotalOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public int ActiveCustomers { get; set; }
            public decimal AverageBasket { get; set; }
            public long PayingCustomers { get; set; }
            public long RepeatCustomers { get; set; }
            public decimal? RepeatRate { get; set; }
        }
    }
}
