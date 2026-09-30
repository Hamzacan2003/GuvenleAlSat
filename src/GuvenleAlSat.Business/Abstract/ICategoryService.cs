using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;

namespace GuvenleAlSat.Business.Abstract;

public interface ICategoryService
{
    Task<IDataResult<List<CategoryDto>>> GetRootCategoriesAsync();
    Task<IDataResult<List<CategoryDto>>> GetSubCategoriesAsync(Guid parentId);
    Task<IDataResult<List<CategoryDto>>> GetCategoryBreadcrumbAsync(Guid categoryId);
    Task<IDataResult<VehicleModelPresetDto>> GetModelPresetDetailsAsync(Guid modelCategoryId);
    Task<IDataResult<VehicleFilterOptionsDto>> GetVehicleFilterOptionsAsync();
}