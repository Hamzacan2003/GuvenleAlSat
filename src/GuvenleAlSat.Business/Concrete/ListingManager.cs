using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Listings;
using GuvenleAlSat.DataAccess.Enums;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.Business.Concrete;

public class ListingManager : IListingService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;

    public ListingManager(AppDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<IDataResult<Listing>> GetByListingNoAsync(long listingNo)
    {
        var listing = await _context.Listings
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.User)
            .Include(l => l.VehicleDetail)
            .Include(l => l.RealEstateDetail)
            .Include(l => l.DamageReport)
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.ListingNo == listingNo && !l.IsDeleted);

        if (listing == null)
            return new ErrorDataResult<Listing>("İlan bulunamadı.");

        if (listing.User != null)
        {
            listing.User.Listings = null!;
            listing.User.RefreshTokens = null!;
        }

        if (listing.Category != null)
        {
            listing.Category.Listings = null!;
        }

        return new SuccessDataResult<Listing>(listing);
    }

    public async Task<IDataResult<long>> CreateListingAsync(CreateListingDto dto, Guid userId)
    {
        var lastListing = await _context.Listings
            .OrderByDescending(l => l.ListingNo)
            .FirstOrDefaultAsync();

        long nextListingNo = (lastListing != null && lastListing.ListingNo >= 100000000)
            ? lastListing.ListingNo + 1
            : 100000001;

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            ListingNo = nextListingNo,
            UserId = userId,
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            // HATA ÇÖZÜMÜ: int -> CurrencyType açık (explicit) tür dönüşümü:
            Currency = (CurrencyType)dto.Currency,
            City = dto.City,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            Status = ListingStatus.Active,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // 1. Vasıta Bilgisi Varsa Ekle
        if (dto.VehicleDetail != null)
        {
            listing.VehicleDetail = new VehicleDetail
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                Year = dto.VehicleDetail.Year,
                Kilometer = dto.VehicleDetail.Kilometer,
                FuelType = dto.VehicleDetail.FuelType,
                Transmission = dto.VehicleDetail.Transmission,
                BodyType = dto.VehicleDetail.BodyType,
                Color = dto.VehicleDetail.Color,
                EnginePowerHp = dto.VehicleDetail.EnginePowerHp,
                EngineCapacityCc = dto.VehicleDetail.EngineCapacityCc,
                HeavyDamageRegistered = dto.VehicleDetail.HeavyDamageRegistered
            };
        }

        // 2. Ekspertiz Raporu Varsa Ekle
        if (dto.DamageReport != null)
        {
            listing.DamageReport = new VehicleDamageReport
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                FrontBumper = dto.DamageReport.FrontBumper,
                RearBumper = dto.DamageReport.RearBumper,
                Hood = dto.DamageReport.Hood,
                Roof = dto.DamageReport.Roof,
                TrunkLid = dto.DamageReport.TrunkLid,
                FrontLeftFender = dto.DamageReport.FrontLeftFender,
                FrontRightFender = dto.DamageReport.FrontRightFender,
                RearLeftFender = dto.DamageReport.RearLeftFender,
                RearRightFender = dto.DamageReport.RearRightFender,
                FrontLeftDoor = dto.DamageReport.FrontLeftDoor,
                FrontRightDoor = dto.DamageReport.FrontRightDoor,
                RearLeftDoor = dto.DamageReport.RearLeftDoor,
                RearRightDoor = dto.DamageReport.RearRightDoor
            };
        }

        // 3. Emlak Bilgisi Varsa Ekle
        if (dto.RealEstateDetail != null)
        {
            listing.RealEstateDetail = new RealEstateDetail
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                GrossSquareMeters = dto.RealEstateDetail.GrossSquareMeters,
                NetSquareMeters = dto.RealEstateDetail.NetSquareMeters,
                RoomCount = dto.RealEstateDetail.RoomCount,
                BuildingAge = dto.RealEstateDetail.BuildingAge,
                FloorLocation = dto.RealEstateDetail.FloorLocation,
                TotalFloors = dto.RealEstateDetail.TotalFloors,
                HeatingType = dto.RealEstateDetail.HeatingType,
                BathroomCount = dto.RealEstateDetail.BathroomCount,
                HasBalcony = dto.RealEstateDetail.HasBalcony,
                IsFurnished = dto.RealEstateDetail.IsFurnished,
                InSite = dto.RealEstateDetail.InSite
            };
        }

        // 4. Görselleri Ekle
        if (dto.Images != null && dto.Images.Count > 0)
        {
            listing.Images = dto.Images
                .Where(img => img != null && !string.IsNullOrWhiteSpace(img.ImageUrl))
                .Select((img, index) => new ListingImage
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    ImageUrl = img.ImageUrl,
                    IsMain = img.IsMain || index == 0,
                    DisplayOrder = img.DisplayOrder > 0 ? img.DisplayOrder : index + 1
                }).ToList();
        }

        await _context.Listings.AddAsync(listing);
        await _context.SaveChangesAsync();

        return new SuccessDataResult<long>(listing.ListingNo, "İlan başarıyla oluşturuldu.");
    }

    public async Task<IDataResult<List<Listing>>> GetActiveListingsAsync(int page = 1, int pageSize = 20)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var listings = await _context.Listings
            .Include(l => l.User)
            .Include(l => l.VehicleDetail)
            .Include(l => l.RealEstateDetail)
            .Include(l => l.Images.Where(i => i.IsMain))
            // KURAL: Silinmemiş, Aktif ve (Galericiyse süresiz / Bireysele 30 gün)
            .Where(l => l.Status == ListingStatus.Active && !l.IsDeleted &&
                        (l.User.UserType == UserType.Corporate || l.PublishedAt >= thirtyDaysAgo))
            .OrderByDescending(l => l.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new SuccessDataResult<List<Listing>>(listings);
    }

    public async Task<IDataResult<List<ListingCardDto>>> GetUserListingsAsync(Guid userId)
    {
        var listings = await _context.Listings
            .AsNoTracking()
            .Include(l => l.VehicleDetail)
            .Include(l => l.Images)
            .Where(l => l.UserId == userId && !l.IsDeleted) // Kendi ilanları süresi dolsa da görünür
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new ListingCardDto
            {
                Id = l.Id,
                ListingNo = l.ListingNo,
                Title = l.Title,
                Price = l.Price,
                Currency = l.Currency,
                City = l.City,
                District = l.District,
                PublishedAt = l.PublishedAt,
                MainImageUrl = l.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                               ?? l.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                Year = l.VehicleDetail != null ? l.VehicleDetail.Year : null,
                Kilometer = l.VehicleDetail != null ? l.VehicleDetail.Kilometer : null,
                Transmission = l.VehicleDetail != null ? l.VehicleDetail.Transmission : null,
                FuelType = l.VehicleDetail != null ? l.VehicleDetail.FuelType : null,
                HeavyDamageRegistered = l.VehicleDetail != null && l.VehicleDetail.HeavyDamageRegistered
            })
            .ToListAsync();

        return new SuccessDataResult<List<ListingCardDto>>(listings);
    }

    public Task<IDataResult<UploadImageResponseDto>> GenerateImageUploadUrlAsync(UploadImageRequestDto dto, Guid userId)
    {
        var uploadUrl = _storageService.GetPresignedUploadUrl(dto.FileName, dto.ContentType, out string storageKey);
        var finalUrl = _storageService.GetFileUrl(storageKey);

        var response = new UploadImageResponseDto
        {
            UploadUrl = uploadUrl,
            StorageKey = storageKey,
            FinalImageUrl = finalUrl
        };

        return Task.FromResult<IDataResult<UploadImageResponseDto>>(new SuccessDataResult<UploadImageResponseDto>(response));
    }

    public async Task<IDataResult<PagedListingResultDto>> SearchListingsAsync(ListingFilterRequestDto filter)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var query = _context.Listings
            .Include(l => l.VehicleDetail)
            .Include(l => l.RealEstateDetail)
            .Include(l => l.Images)
            .Include(l => l.User)
            .Where(l => l.Status == ListingStatus.Active && !l.IsDeleted &&
                        (l.User.UserType == UserType.Corporate || l.PublishedAt >= thirtyDaysAgo))
            .AsNoTracking();

        if (filter.CategoryId.HasValue)
        {
            var categoryIds = await GetChildCategoryIdsAsync(filter.CategoryId.Value);
            query = query.Where(l => categoryIds.Contains(l.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var term = filter.SearchQuery.Trim().ToLower();
            if (long.TryParse(term, out var searchNo))
            {
                query = query.Where(l => l.ListingNo == searchNo || l.Title.ToLower().Contains(term));
            }
            else
            {
                query = query.Where(l => l.Title.ToLower().Contains(term) || l.City.ToLower().Contains(term) || l.District.ToLower().Contains(term));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(l => l.City.ToLower() == filter.City.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.District))
            query = query.Where(l => l.District.ToLower() == filter.District.ToLower());

        if (filter.MinPrice.HasValue)
            query = query.Where(l => l.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(l => l.Price <= filter.MaxPrice.Value);

        if (filter.MinYear.HasValue)
            query = query.Where(l => l.VehicleDetail != null && l.VehicleDetail.Year >= filter.MinYear.Value);

        if (filter.MaxYear.HasValue)
            query = query.Where(l => l.VehicleDetail != null && l.VehicleDetail.Year <= filter.MaxYear.Value);

        if (filter.MinKm.HasValue)
            query = query.Where(l => l.VehicleDetail != null && l.VehicleDetail.Kilometer >= filter.MinKm.Value);

        if (filter.MaxKm.HasValue)
            query = query.Where(l => l.VehicleDetail != null && l.VehicleDetail.Kilometer <= filter.MaxKm.Value);

        if (filter.FuelTypes != null && filter.FuelTypes.Any())
            query = query.Where(l => l.VehicleDetail != null && filter.FuelTypes.Contains(l.VehicleDetail.FuelType));

        if (filter.Transmissions != null && filter.Transmissions.Any())
            query = query.Where(l => l.VehicleDetail != null && filter.Transmissions.Contains(l.VehicleDetail.Transmission));

        if (filter.BodyTypes != null && filter.BodyTypes.Any())
            query = query.Where(l => l.VehicleDetail != null && filter.BodyTypes.Contains(l.VehicleDetail.BodyType));

        if (filter.TractionTypes != null && filter.TractionTypes.Any())
            query = query.Where(l => l.VehicleDetail != null && filter.TractionTypes.Contains(l.VehicleDetail.TractionType));

        if (filter.ExcludeHeavyDamage == true)
            query = query.Where(l => l.VehicleDetail == null || !l.VehicleDetail.HeavyDamageRegistered);

        query = (filter.SortBy ?? "date_desc").ToLower() switch
        {
            "price_asc" => query.OrderBy(l => l.Price),
            "price_desc" => query.OrderByDescending(l => l.Price),
            "km_asc" => query.OrderBy(l => l.VehicleDetail != null ? l.VehicleDetail.Kilometer : 0),
            "km_desc" => query.OrderByDescending(l => l.VehicleDetail != null ? l.VehicleDetail.Kilometer : 0),
            "year_desc" => query.OrderByDescending(l => l.VehicleDetail != null ? l.VehicleDetail.Year : 0),
            _ => query.OrderByDescending(l => l.PublishedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new ListingCardDto
            {
                Id = l.Id,
                ListingNo = l.ListingNo,
                Title = l.Title,
                Price = l.Price,
                Currency = l.Currency,
                City = l.City,
                District = l.District,
                PublishedAt = l.PublishedAt,
                MainImageUrl = l.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                               ?? l.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                Year = l.VehicleDetail != null ? l.VehicleDetail.Year : null,
                Kilometer = l.VehicleDetail != null ? l.VehicleDetail.Kilometer : null,
                Transmission = l.VehicleDetail != null ? l.VehicleDetail.Transmission : null,
                FuelType = l.VehicleDetail != null ? l.VehicleDetail.FuelType : null,
                HeavyDamageRegistered = l.VehicleDetail != null && l.VehicleDetail.HeavyDamageRegistered,
                IsStore = l.User.UserType == UserType.Corporate,
                StoreName = l.User.StoreName
            })
            .ToListAsync();

        var result = new PagedListingResultDto
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = filter.Page,
            PageSize = filter.PageSize
        };

        return new SuccessDataResult<PagedListingResultDto>(result);
    }

    private async Task<List<Guid>> GetChildCategoryIdsAsync(Guid parentId)
    {
        var ids = new List<Guid> { parentId };
        var childIds = await _context.Categories
            .Where(c => c.ParentCategoryId == parentId && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var childId in childIds)
        {
            ids.AddRange(await GetChildCategoryIdsAsync(childId));
        }
        return ids;
    }

    public async Task<IDataResult<string>> GetSellerPhoneNumberAsync(long listingNo)
    {
        var listing = await _context.Listings
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.ListingNo == listingNo && !l.IsDeleted);

        if (listing == null)
            return new ErrorDataResult<string>("İlan bulunamadı.");

        if (string.IsNullOrWhiteSpace(listing.User?.PhoneNumber))
            return new ErrorDataResult<string>("Satıcıya ait telefon numarası bulunmuyor.");

        return new SuccessDataResult<string>(listing.User.PhoneNumber, "Telefon numarası başarıyla alındı.");
    }
}