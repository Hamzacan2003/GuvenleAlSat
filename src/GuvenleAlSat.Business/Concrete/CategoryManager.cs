using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.Business.Concrete;

public class CategoryManager : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IDataResult<List<CategoryDto>>> GetRootCategoriesAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.ParentCategoryId == null && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                DisplayOrder = c.DisplayOrder,
                IsLeaf = c.IsLeaf
            })
            .ToListAsync();

        return new SuccessDataResult<List<CategoryDto>>(categories);
    }

    public async Task<IDataResult<List<CategoryDto>>> GetSubCategoriesAsync(Guid parentId)
    {
        var subCategories = await _context.Categories
            .Where(c => c.ParentCategoryId == parentId && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                ParentCategoryId = c.ParentCategoryId,
                Name = c.Name,
                Slug = c.Slug,
                DisplayOrder = c.DisplayOrder,
                IsLeaf = c.IsLeaf
            })
            .ToListAsync();

        return new SuccessDataResult<List<CategoryDto>>(subCategories);
    }

    public async Task<IDataResult<List<CategoryDto>>> GetCategoryBreadcrumbAsync(Guid categoryId)
    {
        var breadcrumbs = new List<CategoryDto>();
        Guid? currentId = categoryId;

        while (currentId.HasValue)
        {
            var category = await _context.Categories
                .Where(c => c.Id == currentId.Value && !c.IsDeleted)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    ParentCategoryId = c.ParentCategoryId,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsLeaf = c.IsLeaf
                })
                .FirstOrDefaultAsync();

            if (category == null) break;

            breadcrumbs.Insert(0, category);
            currentId = category.ParentCategoryId;
        }

        return new SuccessDataResult<List<CategoryDto>>(breadcrumbs);
    }

    public async Task<IDataResult<VehicleModelPresetDto>> GetModelPresetDetailsAsync(Guid modelCategoryId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == modelCategoryId && !c.IsDeleted);

        if (category == null || !category.IsLeaf)
            return new ErrorDataResult<VehicleModelPresetDto>("Geçerli bir araç modeli/paketi bulunamadı.");

        var breadcrumbResult = await GetCategoryBreadcrumbAsync(modelCategoryId);
        var fullName = string.Join(" > ", breadcrumbResult.Data?.Select(b => b.Name) ?? Array.Empty<string>());

        var preset = new VehicleModelPresetDto
        {
            CategoryId = category.Id,
            CategoryFullName = fullName,
            FuelType = category.DefaultFuelType ?? "Benzin",
            Transmission = category.DefaultTransmission ?? "Manuel",
            BodyType = category.DefaultBodyType ?? "Sedan",
            TractionType = category.DefaultTractionType ?? "Önden Çekiş",
            EngineCapacityCc = category.DefaultEngineCapacityCc ?? 1598,
            EnginePowerHp = category.DefaultEnginePowerHp ?? 100
        };

        return new SuccessDataResult<VehicleModelPresetDto>(preset, "Araç fabrika teknik verileri başarıyla yüklendi.");
    }

    public Task<IDataResult<VehicleFilterOptionsDto>> GetVehicleFilterOptionsAsync()
    {
        return Task.FromResult<IDataResult<VehicleFilterOptionsDto>>(
            new SuccessDataResult<VehicleFilterOptionsDto>(new VehicleFilterOptionsDto()));
    }
}