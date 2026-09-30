using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LocationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Locations/cities
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await _context.Cities
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.PlateCode })
            .ToListAsync();

        return Ok(new { success = true, data = cities });
    }

    // GET /api/Locations/districts/{cityId} VE GET /api/Locations/cities/{cityId}/districts
    [HttpGet("districts/{cityId:guid}")]
    [HttpGet("cities/{cityId:guid}/districts")]
    public async Task<IActionResult> GetDistricts(Guid cityId)
    {
        var districts = await _context.Districts
            .Where(d => d.CityId == cityId)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.CityId })
            .ToListAsync();

        return Ok(new { success = true, data = districts });
    }

    // GET /api/Locations/neighborhoods/{districtId} VE GET /api/Locations/districts/{districtId}/neighborhoods
    [HttpGet("neighborhoods/{districtId:guid}")]
    [HttpGet("districts/{districtId:guid}/neighborhoods")]
    public async Task<IActionResult> GetNeighborhoods(Guid districtId)
    {
        var neighborhoods = await _context.Neighborhoods
            .Where(n => n.DistrictId == districtId)
            .OrderBy(n => n.Name)
            .Select(n => new { n.Id, n.Name, n.DistrictId })
            .ToListAsync();

        return Ok(new { success = true, data = neighborhoods });
    }
}