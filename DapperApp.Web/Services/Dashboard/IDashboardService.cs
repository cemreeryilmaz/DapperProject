using DapperApp.Web.Models.Sales;

namespace DapperApp.Web.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetAsync(DateWindow window);
    }
}
