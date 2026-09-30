using GuvenleAlSat.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace GuvenleAlSat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
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