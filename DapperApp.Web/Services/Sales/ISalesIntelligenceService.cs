using DapperApp.Web.Models.Sales;

namespace DapperApp.Web.Services.Sales
{
    public interface ISalesIntelligenceService
    {
        Task<SalesIntelligenceViewModel> GetAsync(DateWindow window);
    }
}
