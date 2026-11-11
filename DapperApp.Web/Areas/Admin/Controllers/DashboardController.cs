using System.Diagnostics;
using DapperApp.Web.Models.Sales;
using DapperApp.Web.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace DapperApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboard;

        public DashboardController(IDashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        public async Task<IActionResult> Index(string? start, string? end)
        {
            var window = DateWindow.Resolve(start, end);
            var watch = Stopwatch.StartNew();
            try
            {
                var model = await _dashboard.GetAsync(window);
                watch.Stop();
                model.ElapsedMs = (int)watch.ElapsedMilliseconds;
                return View(model);
            }
            catch (Exception exception)
            {
                return DatabaseProblem(exception);
            }
        }

        private IActionResult DatabaseProblem(Exception exception)
        {
            Response.StatusCode = 503;
            return View("DatabaseUnavailable", exception.Message);
        }
    }
}
