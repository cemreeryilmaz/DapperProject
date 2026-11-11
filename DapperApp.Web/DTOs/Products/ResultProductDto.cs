using DapperApp.Web.DTOs.Categories;

namespace DapperApp.Web.DTOs.Products
{
    public class ResultProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public ResultCategoryDto Category { get; set; }
    }
}
