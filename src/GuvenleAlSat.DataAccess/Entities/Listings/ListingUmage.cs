using GuvenleAlSat.DataAccess.Entities.Common;
using GuvenleAlSat.DataAccess.Entities.Users;

namespace GuvenleAlSat.DataAccess.Entities.Listings;

public class ListingImage : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public string StorageKey { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public bool IsMain { get; set; } = false;
}

public class ListingFavorite : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}