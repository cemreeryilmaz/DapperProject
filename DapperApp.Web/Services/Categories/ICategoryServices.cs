using DapperApp.Web.DTOs.Categories;

namespace DapperApp.Web.Services.Categories
{
    public interface ICategoryServices
    {
        Task<IEnumerable<ResultCategoryDto>> GetAllAsync();
        Task<UpdateCategoryDto> GetByIdAsync(int id);
        Task CreateAsync(CreateCategoryDto createCategoryDto);
        Task UpdateAsync(UpdateCategoryDto updateCategoryDto);
        Task DeleteAsync(int id);
    }
}
