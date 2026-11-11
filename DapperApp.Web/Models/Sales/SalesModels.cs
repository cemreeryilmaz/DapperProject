using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace DapperApp.Web.Models.Sales
{
    public readonly record struct DateWindow(DateTime Start, DateTime EndInclusive, DateTime EndExclusive)
    {
        public DateWindow Previous()
        {
            var days = Math.Max(1, (EndExclusive - Start).Days);
            var previousEnd = Start;
            return new DateWindow(previousEnd.AddDays(-days), previousEnd.AddDays(-1), previousEnd);
        }

        public static DateWindow Resolve(string? startText, string? endText)
        {
            var today = DateTime.Today;
            var end = Parse(endText) ?? today;
            var start = Parse(startText) ?? end.AddMonths(-12);
            if (start > end)
                (start, end) = (end, start);
            if (start < end.AddYears(-3))
                start = end.AddYears(-3);
            return new DateWindow(start, end, end.AddDays(1));
        }

        private static DateTime? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return DateTime.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.Date
                : null;
        }
    }

    public static class SalesFormat
    {
        private static readonly CultureInfo Turkish = new("tr-TR");

        public static string Money(decimal value) => value.ToString("C2", Turkish);

        public static string CompactMoney(decimal value)
        {
            var absolute = Math.Abs(value);
            if (absolute >= 1_000_000_000m)
                return string.Create(Turkish, $"{value / 1_000_000_000m:N2} milyar ₺");
            if (absolute >= 1_000_000m)
                return string.Create(Turkish, $"{value / 1_000_000m:N1} milyon ₺");
            return value.ToString("C0", Turkish);
        }

        public static string Number(long value) => value.ToString("N0", Turkish);

        public static string Percent(decimal value) => value.ToString("N1", Turkish) + "%";

        public static string Delta(decimal current, decimal previous)
        {
            if (previous == 0)
                return "—";
            var change = (current - previous) / previous * 100m;
            var sign = change > 0 ? "+" : string.Empty;
            return sign + change.ToString("N1", Turkish) + "%";
        }

        public static string DeltaClass(decimal current, decimal previous)
        {
            if (previous == 0 || current == previous)
                return "is-flat";
            return current > previous ? "is-up" : "is-down";
        }

        public static string StatusClass(string? status) => status switch
        {
            "Tamamlandı" => "is-done",
            "Kargoda" => "is-ship",
            "Beklemede" => "is-wait",
            "İptal" => "is-cancel",
            _ => "is-return"
        };

        public static string MonthLabel(int year, int month) =>
            new DateTime(year, month, 1).ToString("MMM yy", Turkish);
    }

    public static class OrderStatuses
    {
        public static readonly string[] All = ["Tamamlandı", "Kargoda", "Beklemede", "İptal", "İade"];

        public static string? Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;
            return All.FirstOrDefault(item => item.Equals(status.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }
    }

    public class DashboardKpiDto
    {
        public long TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageBasket { get; set; }
    }

    public class MonthlyPointDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public long OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryRevenueDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
    }

    public class CitySalesDto
    {
        public string CityName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public long OrderCount { get; set; }
        public decimal AverageBasket { get; set; }
    }

    public class StatusSliceDto
    {
        public string Status { get; set; } = string.Empty;
        public long OrderCount { get; set; }
    }

    public class ProductRankDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public long QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ElapsedMs { get; set; }
        public DashboardKpiDto Kpi { get; set; } = new();
        public List<MonthlyPointDto> Monthly { get; set; } = [];
        public List<CategoryRevenueDto> Categories { get; set; } = [];
        public List<CitySalesDto> Cities { get; set; } = [];
        public List<StatusSliceDto> Statuses { get; set; } = [];
        public List<ProductRankDto> ProductsByQuantity { get; set; } = [];
        public List<ProductRankDto> ProductsByRevenue { get; set; } = [];
    }

    public class OrderQuery
    {
        public string? Search { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CityId { get; set; }
        public string? Status { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string SortColumn { get; set; } = "OrderDate";
        public string SortDirection { get; set; } = "DESC";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        public RouteValueDictionary ToRoute(int? page = null, string? sortColumn = null, string? sortDirection = null, int? pageSize = null)
        {
            return new RouteValueDictionary
            {
                ["search"] = Search,
                ["start"] = StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["end"] = EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["cityId"] = CityId,
                ["status"] = Status,
                ["minAmount"] = MinAmount?.ToString(CultureInfo.InvariantCulture),
                ["maxAmount"] = MaxAmount?.ToString(CultureInfo.InvariantCulture),
                ["sort"] = sortColumn ?? SortColumn,
                ["dir"] = sortDirection ?? SortDirection,
                ["page"] = page ?? Page,
                ["pageSize"] = pageSize ?? PageSize
            };
        }
    }

    public class OrderListItemDto
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class OrderLineDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
        public DateTime PaidAt { get; set; }
        public List<OrderLineDto> Lines { get; set; } = [];
    }

    public class CityOptionDto
    {
        public int CityId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PendingOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class AdminChromeDto
    {
        public long PendingCount { get; set; }
        public List<PendingOrderDto> Recent { get; set; } = [];
    }

    public class OrderExplorerViewModel
    {
        public OrderQuery Query { get; set; } = new();
        public List<OrderListItemDto> Items { get; set; } = [];
        public List<CityOptionDto> Cities { get; set; } = [];
        public long TotalCount { get; set; }
        public int ElapsedMs { get; set; }
        public int TotalPages => Query.PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)Query.PageSize);
    }

    public class PeriodMetricsDto
    {
        public long TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AverageBasket { get; set; }
    }

    public class RepeatRateDto
    {
        public long ActiveCustomers { get; set; }
        public long RepeatCustomers { get; set; }
        public decimal? RepeatRate { get; set; }
    }

    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public long OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageBasket { get; set; }
    }

    public class CategoryMonthDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
    }

    public class CategoryMatrixRow
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal[] Values { get; set; } = [];
        public decimal Total { get; set; }
    }

    public class SalesIntelligenceViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime PreviousStart { get; set; }
        public DateTime PreviousEnd { get; set; }
        public int ElapsedMs { get; set; }
        public PeriodMetricsDto Current { get; set; } = new();
        public PeriodMetricsDto Previous { get; set; } = new();
        public RepeatRateDto CurrentRepeat { get; set; } = new();
        public RepeatRateDto PreviousRepeat { get; set; } = new();
        public List<TopCustomerDto> TopCustomers { get; set; } = [];
        public List<CitySalesDto> Cities { get; set; } = [];
        public List<string> MatrixMonths { get; set; } = [];
        public List<CategoryMatrixRow> Matrix { get; set; } = [];
        public List<ProductRankDto> TopProducts { get; set; } = [];
    }
}
