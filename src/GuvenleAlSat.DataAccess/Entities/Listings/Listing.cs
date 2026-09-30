using GuvenleAlSat.DataAccess.Entities.Categories;
using GuvenleAlSat.DataAccess.Entities.Common;
using GuvenleAlSat.DataAccess.Entities.Users;
using GuvenleAlSat.DataAccess.Enums;

namespace GuvenleAlSat.DataAccess.Entities.Listings;

public class Listing : BaseEntity
{
    public long ListingNo { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public CurrencyType Currency { get; set; } = CurrencyType.TRY;
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int ViewCount { get; set; } = 0;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    public VehicleDetail? VehicleDetail { get; set; }
    public VehicleDamageReport? DamageReport { get; set; }

    public string? DynamicAttributesJson { get; set; }

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
    public ICollection<ListingFavorite> FavoritedByUsers { get; set; } = new List<ListingFavorite>();
    public virtual RealEstateDetail? RealEstateDetail { get; set; }
}