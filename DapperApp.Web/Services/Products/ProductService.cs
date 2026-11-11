using Dapper;
using DapperApp.Web.Context;
using DapperApp.Web.DTOs.Categories;
using DapperApp.Web.DTOs.Products;
using System.Data;

namespace DapperApp.Web.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly DapperDbContext _context;
        private readonly IDbConnection _db;

        public ProductService(DapperDbContext context)
        {
            _context = context;
            _db = context.CreateConnection();
        }

        public async Task ChangeStatusAsync(int id)
        {
            var product = await GetByIdAsync(id);
            var parameters = new DynamicParameters();
            parameters.Add("ProductId", id);

            if (product.IsActive)
            {
               parameters.Add("IsActive", false);
            }
            else
            {
                parameters.Add("IsActive", true);
            }

            string query = "UPDATE Products SET IsActive = @IsActive WHERE ProductId = @ProductId";
            
            await _db.ExecuteAsync(query, parameters);
        }

        public async Task CreateAsync(CreateProductDto createProductDto)
        {
            string query = "INSERT INTO Products (Name, Price, Stock, ImageUrl, IsActive, CategoryId) VALUES (@Name, @Price, @Stock, @ImageUrl, @IsActive, @CategoryId)";
            var parameters = new DynamicParameters(createProductDto);

            await _db.ExecuteAsync(query, parameters);
        }

        public async Task DeleteAsync(int id)
        {
            string query = "DELETE FROM Products WHERE ProductId = @ProductId";
            var parameters = new DynamicParameters();
            parameters.Add("ProductId", id);

            await _db.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<ResultProductDto>> GetAllAsync()
        {
            string query = "SELECT * FROM Products p inner join Categories c on p.CategoryId = c.CategoryId";
            return await _db.QueryAsync<ResultProductDto, ResultCategoryDto, ResultProductDto>(query, (product, category) => { product.Category = category; return product; }, splitOn: "CategoryId");
        }

        public async Task<UpdateProductDto> GetByIdAsync(int id)
        {
            string query = "SELECT * FROM Products WHERE ProductId = @ProductId";
            var parameters = new DynamicParameters();
            parameters.Add("ProductId", id);

            return await _db.QueryFirstOrDefaultAsync<UpdateProductDto>(query, parameters);
        }

        public async Task UpdateAsync(UpdateProductDto updateProductDto)
        {
            string query = "UPDATE Products SET Name = @Name, Price = @Price, Stock = @Stock, ImageUrl = @ImageUrl, IsActive = @IsActive, CategoryId = @CategoryId WHERE ProductId = @ProductId";
            var parameters = new DynamicParameters(updateProductDto);

            await _db.ExecuteAsync(query, parameters);
        }
    }
}
