using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.Business.Concrete;

public class LocationManager : ILocationService
{
    private readonly AppDbContext _context;

    public LocationManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IDataResult<List<CityDto>>> GetCitiesAsync()
    {
        var cities = await _context.Cities
            .OrderBy(c => c.PlateCode)
            .Select(c => new CityDto { Id = c.Id, PlateCode = c.PlateCode, Name = c.Name })
            .ToListAsync();

        return new SuccessDataResult<List<CityDto>>(cities);
    }

    public async Task<IDataResult<List<DistrictDto>>> GetDistrictsByCityIdAsync(Guid cityId)
    {
        var districts = await _context.Districts
            .Where(d => d.CityId == cityId)
            .OrderBy(d => d.Name)
            .Select(d => new DistrictDto { Id = d.Id, CityId = d.CityId, Name = d.Name })
            .ToListAsync();

        return new SuccessDataResult<List<DistrictDto>>(districts);
    }

    public async Task<IDataResult<List<NeighborhoodDto>>> GetNeighborhoodsByDistrictIdAsync(Guid districtId)
    {
        var neighborhoods = await _context.Neighborhoods
            .Where(n => n.DistrictId == districtId)
            .OrderBy(n => n.Name)
            .Select(n => new NeighborhoodDto { Id = n.Id, DistrictId = n.DistrictId, Name = n.Name })
            .ToListAsync();

        return new SuccessDataResult<List<NeighborhoodDto>>(neighborhoods);
    }
}