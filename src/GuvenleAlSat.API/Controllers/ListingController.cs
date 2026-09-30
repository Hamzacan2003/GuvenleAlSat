using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GuvenleAlSat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;
    private readonly AppDbContext _context;

    public ListingsController(IListingService listingService, AppDbContext context)
    {
        _listingService = listingService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetListings([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _listingService.GetActiveListingsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{listingNo:long}")]
    public async Task<IActionResult> GetByListingNo(long listingNo)
    {
        var result = await _listingService.GetByListingNoAsync(listingNo);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("generate-upload-url")]
    public async Task<IActionResult> GenerateUploadUrl([FromBody] UploadImageRequestDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _listingService.GenerateImageUploadUrlAsync(dto, userId);
        return Ok(result);
    }
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteListing(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized();

        var listing = await _context.Listings
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == currentUserId && !l.IsDeleted);

        if (listing == null)
            return NotFound(new { success = false, message = "İlan bulunamadı veya silme yetkiniz yok." });

        listing.IsDeleted = true;
        listing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "İlanınız başarıyla silindi ve yayından kaldırıldı." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateListing([FromBody] CreateListingDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _listingService.CreateListingAsync(dto, userId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(new { success = true, data = new { listingNo = result.Data }, message = result.Message });
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateListing(Guid id, [FromBody] UpdateListingDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized();

        var listing = await _context.Listings
            .Include(l => l.VehicleDetail)
            .Include(l => l.DamageReport)
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == currentUserId && !l.IsDeleted);

        if (listing == null)
            return NotFound(new { success = false, message = "İlan bulunamadı veya yetkiniz yok." });

        listing.Title = dto.Title;
        listing.Description = dto.Description;
        listing.Price = dto.Price;
        listing.City = dto.City;
        listing.District = dto.District;
        listing.Neighborhood = dto.Neighborhood;
        listing.UpdatedAt = DateTime.UtcNow;

        if (listing.VehicleDetail != null && dto.VehicleDetail != null)
        {
            listing.VehicleDetail.Year = dto.VehicleDetail.Year;
            listing.VehicleDetail.Kilometer = dto.VehicleDetail.Kilometer;
            listing.VehicleDetail.FuelType = dto.VehicleDetail.FuelType;
            listing.VehicleDetail.Transmission = dto.VehicleDetail.Transmission;
            listing.VehicleDetail.BodyType = dto.VehicleDetail.BodyType;
            listing.VehicleDetail.Color = dto.VehicleDetail.Color;
            listing.VehicleDetail.HeavyDamageRegistered = dto.VehicleDetail.HeavyDamageRegistered;
        }

        if (listing.DamageReport != null && dto.DamageReport != null)
        {
            // Veritabanı modeline uygun doğrudan int veya dynamic atama
            dynamic dr = listing.DamageReport;
            dr.FrontBumper = (dynamic)dto.DamageReport.FrontBumper;
            dr.RearBumper = (dynamic)dto.DamageReport.RearBumper;
            dr.Hood = (dynamic)dto.DamageReport.Hood;
            dr.Roof = (dynamic)dto.DamageReport.Roof;
            dr.TrunkLid = (dynamic)dto.DamageReport.TrunkLid;
            dr.FrontLeftFender = (dynamic)dto.DamageReport.FrontLeftFender;
            dr.FrontRightFender = (dynamic)dto.DamageReport.FrontRightFender;
            dr.RearLeftFender = (dynamic)dto.DamageReport.RearLeftFender;
            dr.RearRightFender = (dynamic)dto.DamageReport.RearRightFender;
            dr.FrontLeftDoor = (dynamic)dto.DamageReport.FrontLeftDoor;
            dr.FrontRightDoor = (dynamic)dto.DamageReport.FrontRightDoor;
            dr.RearLeftDoor = (dynamic)dto.DamageReport.RearLeftDoor;
            dr.RearRightDoor = (dynamic)dto.DamageReport.RearRightDoor;
        }

        if (dto.Images != null && dto.Images.Count > 0)
        {
            _context.ListingImages.RemoveRange(listing.Images);
            listing.Images = dto.Images.Select((img, index) => new ListingImage
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                ImageUrl = img.ImageUrl,
                IsMain = img.IsMain || index == 0,
                DisplayOrder = index + 1
            }).ToList();
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "İlan başarıyla güncellendi." });
    }

    [Authorize]
    [HttpPost("{id:guid}/boost-premium")]
    public async Task<IActionResult> BoostPremium(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == id && l.UserId == currentUserId && !l.IsDeleted);
        if (listing == null)
            return NotFound(new { success = false, message = "İlan bulunamadı." });

        listing.PublishedAt = DateTime.UtcNow;
        listing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "İlanınız Premium Doping ile vitrinde öne çıkarıldı!" });
    }

    [Authorize]
    [HttpGet("my-listings")]
    public async Task<IActionResult> GetMyListings()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Kullanıcı oturumu doğrulanamadı." });

        var result = await _listingService.GetUserListingsAsync(currentUserId);
        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchListings([FromBody] ListingFilterRequestDto filter)
    {
        var result = await _listingService.SearchListingsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{listingNo:long}/phone")]
    public async Task<IActionResult> GetSellerPhoneNumber(long listingNo)
    {
        var result = await _listingService.GetSellerPhoneNumberAsync(listingNo);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("id")?.Value;

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}