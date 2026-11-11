using Dapper;
using DapperApp.Web.Context;
using DapperApp.Web.Models.Sales;
using System.Data;
using System.Globalization;
using System.Text;

namespace DapperApp.Web.Services.Orders
{
    public class OrderExplorerService : IOrderExplorerService
    {
        private const int ExportLimit = 10_000;
        private static readonly HashSet<string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "OrderNo", "CustomerName", "CityName", "OrderDate", "TotalAmount", "PaymentMethod", "Status"
        };

        private readonly DapperDbContext _context;
        private readonly IDbConnection _db;

        public OrderExplorerService(DapperDbContext context)
        {
            _context = context;
            _db = context.CreateConnection();
        }

        public Task<OrderExplorerViewModel> SearchAsync(OrderQuery query) =>
            QueryAsync(query, null);

        public async Task<IReadOnlyList<OrderListItemDto>> ExportAsync(OrderQuery query)
        {
            query.Page = 1;
            var page = await QueryAsync(query, ExportLimit);
            return page.Items;
        }

        public async Task<OrderDetailDto?> GetDetailAsync(int orderId)
        {
            string headerQuery = @"SELECT o.OrderId, o.OrderNo, o.OrderDate, o.TotalAmount, o.Status,
                                          c.FullName AS CustomerName, c.Email, ci.Name AS CityName,
                                          p.Method AS PaymentMethod, p.Amount AS PaidAmount, p.PaidAt
                                   FROM Orders o
                                   INNER JOIN Customers c ON c.CustomerId = o.CustomerId
                                   INNER JOIN Cities ci ON ci.CityId = o.CityId
                                   INNER JOIN Payments p ON p.OrderId = o.OrderId
                                   WHERE o.OrderId = @OrderId";

            string lineQuery = @"SELECT pr.Name AS ProductName, cat.Name AS CategoryName, od.Quantity, od.UnitPrice, od.LineTotal
                                 FROM OrderDetails od
                                 INNER JOIN Products pr ON pr.ProductId = od.ProductId
                                 INNER JOIN Categories cat ON cat.CategoryId = pr.CategoryId
                                 WHERE od.OrderId = @OrderId
                                 ORDER BY od.OrderDetailId";

            var parameters = new DynamicParameters();
            parameters.Add("OrderId", orderId);

            var detail = await _db.QueryFirstOrDefaultAsync<OrderDetailDto>(headerQuery, parameters);
            if (detail == null)
                return null;

            detail.Lines = (await _db.QueryAsync<OrderLineDto>(lineQuery, parameters)).ToList();
            return detail;
        }

        public async Task<AdminChromeDto> GetChromeAsync()
        {
            string countQuery = "SELECT COUNT_BIG(*) FROM Orders WHERE Status = N'Beklemede'";
            string recentQuery = @"SELECT TOP 5 OrderId, OrderNo, TotalAmount, OrderDate
                                   FROM Orders
                                   WHERE Status = N'Beklemede'
                                   ORDER BY OrderDate DESC, OrderId DESC";

            return new AdminChromeDto
            {
                PendingCount = await _db.QueryFirstAsync<long>(countQuery),
                Recent = (await _db.QueryAsync<PendingOrderDto>(recentQuery)).ToList()
            };
        }

        public async Task<IReadOnlyList<CityOptionDto>> GetCitiesAsync()
        {
            string query = "SELECT CityId, Name FROM Cities ORDER BY Name";
            var cities = await _db.QueryAsync<CityOptionDto>(query);
            return cities.ToList();
        }

        public static string BuildCsv(IEnumerable<OrderListItemDto> rows)
        {
            var turkish = new CultureInfo("tr-TR");
            var builder = new StringBuilder();
            builder.Append('\uFEFF');
            builder.AppendLine("SiparisNo;Musteri;Sehir;Tarih;Tutar;OdemeYontemi;Durum");
            foreach (var row in rows)
            {
                builder.Append(Escape(row.OrderNo)).Append(';')
                    .Append(Escape(row.CustomerName)).Append(';')
                    .Append(Escape(row.CityName)).Append(';')
                    .Append(row.OrderDate.ToString("dd.MM.yyyy HH:mm", turkish)).Append(';')
                    .Append(row.TotalAmount.ToString("0.00", turkish)).Append(';')
                    .Append(Escape(row.PaymentMethod)).Append(';')
                    .Append(Escape(row.Status))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private async Task<OrderExplorerViewModel> QueryAsync(OrderQuery query, int? fetchOverride)
        {
            Normalize(query);
            var fetch = fetchOverride ?? query.PageSize;
            var (likePrefix, likeWord) = BuildLike(query.Search);
            var orderNumber = IsOrderNumber(query.Search);
            var offsetLong = (long)(query.Page - 1) * fetch;
            if (offsetLong > 1_500_000)
            {
                query.Page = 1;
                offsetLong = 0;
            }

            var parameters = new DynamicParameters();
            parameters.Add("Offset", (int)offsetLong);
            parameters.Add("Fetch", fetch);
            var where = new List<string>();
            if (query.StartDate != null)
            {
                where.Add("o.OrderDate >= @StartDate");
                parameters.Add("StartDate", query.StartDate);
            }
            if (query.EndDate != null)
            {
                where.Add("o.OrderDate < @EndDate");
                parameters.Add("EndDate", query.EndDate.Value.Date.AddDays(1));
            }
            if (query.CityId != null)
            {
                where.Add("o.CityId = @CityId");
                parameters.Add("CityId", query.CityId);
            }
            if (query.Status != null)
            {
                where.Add("o.Status = @Status");
                parameters.Add("Status", query.Status);
            }
            if (query.MinAmount != null)
            {
                where.Add("o.TotalAmount >= @MinAmount");
                parameters.Add("MinAmount", query.MinAmount);
            }
            if (query.MaxAmount != null)
            {
                where.Add("o.TotalAmount <= @MaxAmount");
                parameters.Add("MaxAmount", query.MaxAmount);
            }
            if (orderNumber)
            {
                where.Add("o.OrderNo = @OrderNo");
                parameters.Add("OrderNo", query.Search);
            }
            else if (likePrefix != null)
            {
                where.Add("(o.OrderNo LIKE @LikePrefix OR c.FullName LIKE @LikePrefix OR c.FullName LIKE @LikeWord)");
                parameters.Add("LikePrefix", likePrefix);
                parameters.Add("LikeWord", likeWord);
            }
            if (where.Count == 0)
                where.Add("1 = 1");

            string joins = @"FROM Orders o
                             INNER JOIN Customers c ON c.CustomerId = o.CustomerId
                             INNER JOIN Cities ci ON ci.CityId = o.CityId
                             INNER JOIN Payments p ON p.OrderId = o.OrderId";
            string whereSql = " WHERE " + string.Join(" AND ", where);
            string countFrom = likePrefix == null || orderNumber ? "FROM Orders o" : joins;
            string countQuery = "SELECT COUNT_BIG(*) " + countFrom + whereSql;
            string pageQuery = $@"SELECT o.OrderId, o.OrderNo, c.FullName AS CustomerName, ci.Name AS CityName,
                                         o.OrderDate, o.TotalAmount, p.Method AS PaymentMethod, o.Status
                                  {joins}
                                  {whereSql}
                                  ORDER BY {SortSql(query.SortColumn)} {query.SortDirection}, o.OrderId DESC
                                  OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

            var total = await _db.QueryFirstAsync<long>(countQuery, parameters);
            var items = (await _db.QueryAsync<OrderListItemDto>(pageQuery, parameters)).ToList();
            var cities = await GetCitiesAsync();

            return new OrderExplorerViewModel
            {
                Query = query,
                Items = items,
                TotalCount = total,
                Cities = cities.ToList()
            };
        }

        private static string SortSql(string sortColumn) => sortColumn switch
        {
            "OrderNo" => "o.OrderNo",
            "CustomerName" => "c.FullName",
            "CityName" => "ci.Name",
            "TotalAmount" => "o.TotalAmount",
            "PaymentMethod" => "p.Method",
            "Status" => "o.Status",
            _ => "o.OrderDate"
        };

        private static void Normalize(OrderQuery query)
        {
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize switch
            {
                50 => 50,
                100 => 100,
                10_000 => 10_000,
                _ => 25
            };
            query.Status = OrderStatuses.Normalize(query.Status);
            query.CityId = query.CityId is > 0 ? query.CityId : null;
            if (query.MinAmount is < 0)
                query.MinAmount = null;
            if (query.MaxAmount is < 0)
                query.MaxAmount = null;
            if (!SortColumns.Contains(query.SortColumn))
                query.SortColumn = "OrderDate";
            query.SortDirection = string.Equals(query.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                query.Search = query.Search.Trim();
                if (query.Search.Length > 80)
                    query.Search = query.Search[..80];
            }
            else
            {
                query.Search = null;
            }
        }

        private static bool IsOrderNumber(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return false;

            var text = search.Trim();
            if (text.Length < 3 || !text.StartsWith("SP", StringComparison.OrdinalIgnoreCase))
                return false;

            for (var i = 2; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    return false;
            }

            return true;
        }

        private static (string? Prefix, string? Word) BuildLike(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return (null, null);

            var escaped = search.Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
            return (escaped + "%", "% " + escaped + "%");
        }

        private static string Escape(string? value)
        {
            var text = value ?? string.Empty;
            if (text.Contains('"') || text.Contains(';') || text.Contains('\n'))
                return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            return text;
        }
    }
}
