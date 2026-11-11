using System.Diagnostics;
using DapperApp.Web.Models.Sales;
using DapperApp.Web.Services.Sales;
using Microsoft.AspNetCore.Mvc;

namespace DapperApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SalesIntelligenceController : Controller
    {
        private readonly ISalesIntelligenceService _sales;

        public SalesIntelligenceController(ISalesIntelligenceService sales)
        {
            _sales = sales;
        }

        public async Task<IActionResult> Index(string? start, string? end)
        {
            var window = DateWindow.Resolve(start, end);
            var watch = Stopwatch.StartNew();
            try
            {
                var model = await _sales.GetAsync(window);
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
    }
}
