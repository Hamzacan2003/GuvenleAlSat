using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;

namespace GuvenleAlSat.Business.Abstract;

public interface ILocationService
{
    Task<IDataResult<List<CityDto>>> GetCitiesAsync();
    Task<IDataResult<List<DistrictDto>>> GetDistrictsByCityIdAsync(Guid cityId);
    Task<IDataResult<List<NeighborhoodDto>>> GetNeighborhoodsByDistrictIdAsync(Guid districtId);
}