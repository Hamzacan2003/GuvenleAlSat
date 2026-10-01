using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly AppDbContext _context;

    public CategoriesController(ICategoryService categoryService, AppDbContext context)
    {
        _categoryService = categoryService;
        _context = context;
    }

    [HttpGet("roots")]
    public async Task<IActionResult> GetRootCategories()
    {
        var result = await _categoryService.GetRootCategoriesAsync();
        return Ok(result);
    }

    [HttpGet("{parentId:guid}/subcategories")]
    public async Task<IActionResult> GetSubCategories(Guid parentId)
    {
        var result = await _categoryService.GetSubCategoriesAsync(parentId);
        return Ok(result);
    }

    // Slug'a göre alt kategorileri getirme (Örn: /api/Categories/by-slug/otomobil/subcategories)
    [HttpGet("by-slug/{slug}/subcategories")]
    public async Task<IActionResult> GetSubCategoriesBySlug(string slug)
    {
        var parent = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (parent == null) return NotFound(new { success = false, message = "Kategori bulunamadı." });

        var subs = await _context.Categories
            .Where(c => c.ParentCategoryId == parent.Id)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Slug, c.IsLeaf })
            .ToListAsync();

        return Ok(new { success = true, data = subs });
    }

    [HttpGet("{categoryId:guid}/breadcrumb")]
    public async Task<IActionResult> GetBreadcrumb(Guid categoryId)
    {
        var result = await _categoryService.GetCategoryBreadcrumbAsync(categoryId);
        return Ok(result);
    }

    [HttpGet("{modelCategoryId:guid}/preset")]
    public async Task<IActionResult> GetModelPresetDetails(Guid modelCategoryId)
    {
        var result = await _categoryService.GetModelPresetDetailsAsync(modelCategoryId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions()
    {
        var result = await _categoryService.GetVehicleFilterOptionsAsync();
        return Ok(result);
    }
}