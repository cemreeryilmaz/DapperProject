using DapperApp.Web.DTOs.Categories;
using DapperApp.Web.DTOs.Products;

namespace DapperApp.Web.Services.Products
{
    public interface IProductService
    {
        Task<IEnumerable<ResultProductDto>> GetAllAsync();
        Task<UpdateProductDto> GetByIdAsync(int id);
        Task CreateAsync(CreateProductDto createProductDto);
        Task UpdateAsync(UpdateProductDto updateProductDto);
        Task DeleteAsync(int id);
        Task ChangeStatusAsync(int id); 
    }
}
