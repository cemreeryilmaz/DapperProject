using System.Diagnostics;
using System.Globalization;
using System.Text;
using DapperApp.Web.Models.Sales;
using DapperApp.Web.Services.Orders;
using Microsoft.AspNetCore.Mvc;

namespace DapperApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderExplorerController : Controller
    {
        private readonly IOrderExplorerService _orders;

        public OrderExplorerController(IOrderExplorerService orders)
        {
            _orders = orders;
        }

        public async Task<IActionResult> Index(
            string? search, string? start, string? end, int? cityId, string? status,
            string? minAmount, string? maxAmount, string? sort, string? dir, int page = 1, int pageSize = 25)
        {
            var query = Bind(search, start, end, cityId, status, minAmount, maxAmount, sort, dir, page, pageSize);
            var watch = Stopwatch.StartNew();
            try
            {
                var model = await _orders.SearchAsync(query);
                watch.Stop();
                model.ElapsedMs = (int)watch.ElapsedMilliseconds;
                return View(model);
            }
            catch (Exception exception)
            {
                Response.StatusCode = 503;
                return View("DatabaseUnavailable", exception.Message);
            }
        }

        public async Task<IActionResult> Export(
            string? search, string? start, string? end, int? cityId, string? status,
            string? minAmount, string? maxAmount, string? sort, string? dir)
        {
            var query = Bind(search, start, end, cityId, status, minAmount, maxAmount, sort, dir, 1, 25);
            try
            {
                var rows = await _orders.ExportAsync(query);
                var csv = OrderExplorerService.BuildCsv(rows);
                var fileName = $"siparisler_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
            }
            catch (Exception exception)
            {
                Response.StatusCode = 503;
                return View("DatabaseUnavailable", exception.Message);
            }
        }

        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var detail = await _orders.GetDetailAsync(id);
                if (detail == null)
                    return NotFound();
                return Json(detail);
            }
            catch (Exception exception)
            {
                return StatusCode(503, new { message = exception.Message });
            }
        }

        private static OrderQuery Bind(
            string? search, string? start, string? end, int? cityId, string? status,
            string? minAmount, string? maxAmount, string? sort, string? dir, int page, int pageSize)
        {
            var window = DateWindow.Resolve(start, end);
            return new OrderQuery
            {
                Search = search,
                StartDate = window.Start,
                EndDate = window.EndInclusive,
                CityId = cityId,
                Status = status,
                MinAmount = ParseAmount(minAmount),
                MaxAmount = ParseAmount(maxAmount),
                SortColumn = string.IsNullOrWhiteSpace(sort) ? "OrderDate" : sort,
                SortDirection = dir ?? "DESC",
                Page = page,
                PageSize = pageSize
            };
        }

        private static decimal? ParseAmount(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
                return invariant;
            if (decimal.TryParse(text, NumberStyles.Number, new CultureInfo("tr-TR"), out var turkish))
                return turkish;
            return null;
        }
    }
}
