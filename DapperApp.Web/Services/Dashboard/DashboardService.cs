using Dapper;
using DapperApp.Web.Context;
using DapperApp.Web.Models.Sales;
using System.Data;

namespace DapperApp.Web.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly DapperDbContext _context;
        private readonly IDbConnection _db;

        public DashboardService(DapperDbContext context)
        {
            _context = context;
            _db = context.CreateConnection();
        }

        public async Task<DashboardViewModel> GetAsync(DateWindow window)
        {
            var orders = await ReadOrdersAsync(window);
            var ranks = RankProducts(await ReadProductsAsync(window));

            return new DashboardViewModel
            {
                StartDate = window.Start,
                EndDate = window.EndInclusive,
                Kpi = orders.Kpi,
                Monthly = FillMonths(window, orders.Monthly),
                Categories = ranks.Categories,
                Cities = orders.Cities,
                Statuses = orders.Statuses,
                ProductsByQuantity = ranks.ByQuantity,
                ProductsByRevenue = ranks.ByRevenue
            };
        }

        private async Task<(DashboardKpiDto Kpi, List<MonthlyPointDto> Monthly, List<CitySalesDto> Cities, List<StatusSliceDto> Statuses)> ReadOrdersAsync(DateWindow window)
        {
            var parameters = Parameters(window);
            string query = @"SELECT COUNT_BIG(*) AS TotalOrders,
                                    CAST(ISNULL(SUM(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18,2)) AS TotalRevenue,
                                    COUNT(DISTINCT CustomerId) AS TotalCustomers,
                                    CAST(ISNULL(AVG(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18,2)) AS AverageBasket
                             FROM Orders
                             WHERE OrderDate >= @StartDate AND OrderDate < @EndDate;

                             SELECT YEAR(OrderDate) AS [Year],
                                    MONTH(OrderDate) AS [Month],
                                    COUNT_BIG(*) AS OrderCount,
                                    CAST(ISNULL(SUM(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18,2)) AS Revenue
                             FROM Orders
                             WHERE OrderDate >= @StartDate AND OrderDate < @EndDate
                             GROUP BY YEAR(OrderDate), MONTH(OrderDate)
                             ORDER BY [Year], [Month];

                             SELECT TOP 10 ci.Name AS CityName,
                                    CAST(SUM(o.TotalAmount) AS decimal(18,2)) AS Revenue,
                                    COUNT_BIG(*) AS OrderCount
                             FROM Orders o
                             INNER JOIN Cities ci ON ci.CityId = o.CityId
                             WHERE o.OrderDate >= @StartDate AND o.OrderDate < @EndDate AND o.Status <> N'İptal'
                             GROUP BY ci.CityId, ci.Name
                             ORDER BY Revenue DESC;

                             SELECT Status, COUNT_BIG(*) AS OrderCount
                             FROM Orders
                             WHERE OrderDate >= @StartDate AND OrderDate < @EndDate
                             GROUP BY Status
                             ORDER BY OrderCount DESC;";

            using var grid = await _db.QueryMultipleAsync(query, parameters);
            var kpi = await grid.ReadFirstAsync<DashboardKpiDto>();
            var monthly = (await grid.ReadAsync<MonthlyPointDto>()).ToList();
            var cities = (await grid.ReadAsync<CitySalesDto>()).ToList();
            var statuses = (await grid.ReadAsync<StatusSliceDto>()).ToList();
            return (kpi, monthly, cities, statuses);
        }

        private async Task<List<ProductRankDto>> ReadProductsAsync(DateWindow window)
        {
            string query = @"SELECT p.Name AS ProductName,
                                    c.Name AS CategoryName,
                                    CAST(SUM(od.Quantity) AS bigint) AS QuantitySold,
                                    CAST(SUM(od.LineTotal) AS decimal(18,2)) AS Revenue
                             FROM OrderDetails od
                             INNER JOIN Orders o ON o.OrderId = od.OrderId
                             INNER JOIN Products p ON p.ProductId = od.ProductId
                             INNER JOIN Categories c ON c.CategoryId = p.CategoryId
                             WHERE o.OrderDate >= @StartDate AND o.OrderDate < @EndDate AND o.Status <> N'İptal'
                             GROUP BY p.ProductId, p.Name, c.Name";

            var rows = await _db.QueryAsync<ProductRankDto>(query, Parameters(window));
            return rows.ToList();
        }

        private static (List<CategoryRevenueDto> Categories, List<ProductRankDto> ByQuantity, List<ProductRankDto> ByRevenue) RankProducts(List<ProductRankDto> rows)
        {
            var categories = rows
                .GroupBy(row => row.CategoryName)
                .Select(group => new CategoryRevenueDto
                {
                    CategoryName = group.Key,
                    Revenue = group.Sum(row => row.Revenue),
                    Quantity = group.Sum(row => row.QuantitySold)
                })
                .OrderByDescending(row => row.Revenue)
                .Take(10)
                .ToList();

            return (
                categories,
                rows.OrderByDescending(row => row.QuantitySold).Take(10).ToList(),
                rows.OrderByDescending(row => row.Revenue).Take(10).ToList());
        }

        private static DynamicParameters Parameters(DateWindow window)
        {
            var parameters = new DynamicParameters();
            parameters.Add("StartDate", window.Start);
            parameters.Add("EndDate", window.EndExclusive);
            return parameters;
        }

        private static List<MonthlyPointDto> FillMonths(DateWindow window, List<MonthlyPointDto> rows)
        {
            var map = rows.ToDictionary(row => (row.Year, row.Month));
            var filled = new List<MonthlyPointDto>();
            var cursor = new DateTime(window.Start.Year, window.Start.Month, 1);
            var last = window.EndExclusive.AddDays(-1);
            var limit = new DateTime(last.Year, last.Month, 1);
            while (cursor <= limit && filled.Count < 37)
            {
                filled.Add(map.TryGetValue((cursor.Year, cursor.Month), out var row)
                    ? row
                    : new MonthlyPointDto { Year = cursor.Year, Month = cursor.Month });
                cursor = cursor.AddMonths(1);
            }

            return filled;
        }
    }
}
