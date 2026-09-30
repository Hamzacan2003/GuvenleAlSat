using GuvenleAlSat.DataAccess.Entities.Listings;
using GuvenleAlSat.DataAccess.Entities.Subscriptions;
using GuvenleAlSat.DataAccess.Enums;
using Microsoft.AspNetCore.Identity;
using System.Reflection;

namespace GuvenleAlSat.DataAccess.Entities.Users;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserType UserType { get; set; } = UserType.Individual;

    public string? NationalIdNumber { get; set; }
    public int? BirthYear { get; set; }
    public bool IsNviVerified { get; set; } = false;
    public DateTime? NviVerifiedAt { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string? StoreName { get; set; }
    public string? StoreSlug { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? CommercialRegistrationNo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? EmailVerificationCode { get; set; }
    public DateTime? EmailVerificationCodeExpiresAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<ListingFavorite> Favorites { get; set; } = new List<ListingFavorite>();
}