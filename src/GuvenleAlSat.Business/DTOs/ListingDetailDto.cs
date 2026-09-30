using GuvenleAlSat.DataAccess.Entities.Listings;

namespace GuvenleAlSat.Business.DTOs;

public class ListingDetailDto
{
    public Guid Id { get; set; }
    public long ListingNo { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public UserSummaryDto? User { get; set; }
    public VehicleDetail? VehicleDetail { get; set; }

    // Var olmayan DamageReport entity'si yerine DTO'daki CreateDamageReportDto'yu kullanıyoruz:
    public CreateDamageReportDto? DamageReport { get; set; }

    public List<ListingImageDto> Images { get; set; } = new();
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class ListingImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}