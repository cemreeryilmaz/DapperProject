using DapperApp.Web.Models.Sales;
using DapperApp.Web.Services.Orders;
using Microsoft.AspNetCore.Mvc;

namespace DapperApp.Web.Components
{
    public class AdminChromeViewComponent : ViewComponent
    {
        private readonly IOrderExplorerService _orders;

        public AdminChromeViewComponent(IOrderExplorerService orders)
        {
            _orders = orders;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var model = await _orders.GetChromeAsync();
                return View(model);
            }
            catch
            {
                return View(new AdminChromeDto());
            }
        }
    }
}
