using Dapper;
using DapperApp.Web.Context;
using DapperApp.Web.DTOs.Categories;

namespace DapperApp.Web.Services.Categories
{
    public class CategoryServices : ICategoryServices
    {
        private readonly DapperDbContext _context;

        public CategoryServices(DapperDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(CreateCategoryDto createCategoryDto)
        {
            string query = "INSERT INTO Categories (Name) VALUES (@Name)";
            var parameters = new DynamicParameters();
            parameters.Add("Name", createCategoryDto.Name);

            var connection = _context.CreateConnection();

            await connection.ExecuteAsync(query, parameters);
        }

        public async Task DeleteAsync(int id)
        {
            string query = "DELETE FROM Categories WHERE CategoryId = @CategoryId";
            var parameters = new DynamicParameters();
            parameters.Add("CategoryId", id);

            var connection = _context.CreateConnection();

            await connection.ExecuteAsync(query, parameters);

        }

        public async Task<IEnumerable<ResultCategoryDto>> GetAllAsync()
        {
            string query = "SELECT * FROM Categories";
            var connection = _context.CreateConnection();

            return await connection.QueryAsync<ResultCategoryDto>(query);
        }

        public async Task<UpdateCategoryDto> GetByIdAsync(int id)
        {
            string query = "SELECT * FROM Categories WHERE CategoryId = @CategoryId";
            var parameters = new DynamicParameters();
            parameters.Add("CategoryId", id);

            var connection = _context.CreateConnection();

            return await connection.QueryFirstOrDefaultAsync<UpdateCategoryDto>(query, parameters);
        }

        public async Task UpdateAsync(UpdateCategoryDto updateCategoryDto)
        {
            string query = "UPDATE Categories SET Name = @Name WHERE CategoryId = @CategoryId";
            var parameters = new DynamicParameters();
            parameters.Add("CategoryId", updateCategoryDto.CategoryId);
            parameters.Add("Name", updateCategoryDto.Name);

            var connection = _context.CreateConnection();

            await connection.ExecuteAsync(query, parameters);
        }
    }
}
