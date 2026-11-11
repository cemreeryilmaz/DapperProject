using DapperApp.Web.DTOs.Products;
using DapperApp.Web.Services.Categories;
using DapperApp.Web.Services.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DapperApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController(IProductService _productService, ICategoryServices _categoryService) : Controller
    {
        private async Task GetCategoriesAsync()
        {
            var categoryList = await _categoryService.GetAllAsync();
            ViewBag.Categories = (from category in categoryList
                                  select new SelectListItem
                                  {
                                      Text = category.Name,
                                      Value = category.CategoryId.ToString()
                                  }).ToList();
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllAsync();

            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            await GetCategoriesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto createProductDto)
        {
            await _productService.CreateAsync(createProductDto);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int id)
        {
            await GetCategoriesAsync();
            var product = await _productService.GetByIdAsync(id);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Update(UpdateProductDto updateProductDto)
        {
            await _productService.UpdateAsync(updateProductDto);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ChangeStatus(int id)
        {
            await _productService.ChangeStatusAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
