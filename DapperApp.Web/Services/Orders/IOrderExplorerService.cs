using DapperApp.Web.Models.Sales;

namespace DapperApp.Web.Services.Orders
{
    public interface IOrderExplorerService
    {
        Task<OrderExplorerViewModel> SearchAsync(OrderQuery query);
        Task<IReadOnlyList<OrderListItemDto>> ExportAsync(OrderQuery query);
        Task<OrderDetailDto?> GetDetailAsync(int orderId);
        Task<AdminChromeDto> GetChromeAsync();
        Task<IReadOnlyList<CityOptionDto>> GetCitiesAsync();
    }
}
