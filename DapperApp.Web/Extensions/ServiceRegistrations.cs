using DapperApp.Web.Context;
using DapperApp.Web.Services.Categories;
using DapperApp.Web.Services.Dashboard;
using DapperApp.Web.Services.Orders;
using DapperApp.Web.Services.Products;
using DapperApp.Web.Services.Sales;

namespace DapperApp.Web.Extensions
{
    public static class ServiceRegistrations
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<DapperDbContext>();

            services.AddScoped<ICategoryServices, CategoryServices>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IOrderExplorerService, OrderExplorerService>();
            services.AddScoped<ISalesIntelligenceService, SalesIntelligenceService>();
        }
    }
}