using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.API.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehicleMetadataController : ControllerBase
{
    private readonly AppDbContext _context;

    public VehicleMetadataController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands([FromQuery] string? vehicleType = "Otomobil")
    {
        var brands = await _context.VehicleBrands
            .Where(b => string.IsNullOrEmpty(vehicleType) || b.VehicleType == vehicleType)
            .Select(b => new { b.Id, b.Name })
            .OrderBy(b => b.Name)
            .ToListAsync();
        return Ok(brands);
    }

    [HttpGet("series/{brandId}")]
    public async Task<IActionResult> GetSeries(int brandId)
    {
        var series = await _context.VehicleSeries
            .Where(s => s.BrandId == brandId)
            .Select(s => new { s.Id, s.Name })
            .OrderBy(s => s.Name)
            .ToListAsync();
        return Ok(series);
    }

    [HttpGet("trims/{seriesId}")]
    public async Task<IActionResult> GetTrims(int seriesId)
    {
        var trims = await _context.VehicleModelTrims
            .Where(t => t.SeriesId == seriesId)
            .Select(t => new { t.Id, t.Name })
            .OrderBy(t => t.Name)
            .ToListAsync();
        return Ok(trims);
    }
}