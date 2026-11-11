using DapperApp.Web.DTOs.Categories;
using DapperApp.Web.Services.Categories;
using Microsoft.AspNetCore.Mvc;

namespace DapperApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController(ICategoryServices _categoryServices) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryServices.GetAllAsync();

            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCategoryDto createCategoryDto)
        {
            await _categoryServices.CreateAsync(createCategoryDto);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _categoryServices.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int id)
        {
            var category = await _categoryServices.GetByIdAsync(id);
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Update(UpdateCategoryDto updateCategoryDto)
        {
            await _categoryServices.UpdateAsync(updateCategoryDto);
            return RedirectToAction(nameof(Index));
        }
    }
}
