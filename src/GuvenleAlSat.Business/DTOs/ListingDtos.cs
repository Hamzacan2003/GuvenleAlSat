using GuvenleAlSat.DataAccess.Enums;

namespace GuvenleAlSat.Business.DTOs;

public class CreateListingDto
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Currency { get; set; } = 0;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;

    // Vasıta Seçilirse:
    public CreateVehicleDetailDto? VehicleDetail { get; set; }
    public CreateDamageReportDto? DamageReport { get; set; }

    // Emlak Seçilirse:
    public CreateRealEstateDetailDto? RealEstateDetail { get; set; }

    public List<CreateListingImageDto> Images { get; set; } = new();
}

public class CreateRealEstateDetailDto
{
    public int? GrossSquareMeters { get; set; }
    public int? NetSquareMeters { get; set; }
    public string? RoomCount { get; set; }
    public int? BuildingAge { get; set; }
    public int? FloorLocation { get; set; }
    public int? TotalFloors { get; set; }
    public string? HeatingType { get; set; }
    public int? BathroomCount { get; set; }
    public bool HasBalcony { get; set; }
    public bool IsFurnished { get; set; }
    public bool InSite { get; set; }
}

public class CreateVehicleDetailDto
{
    public int Year { get; set; }
    public int Kilometer { get; set; }
    public string Transmission { get; set; } = string.Empty; // Manuel, Otomatik
    public string FuelType { get; set; } = string.Empty;     // Benzin, Dizel, Hibrit, Elektrik
    public string BodyType { get; set; } = string.Empty;     // Sedan, Hatchback, SUV
    public int EnginePowerHp { get; set; }
    public int EngineCapacityCc { get; set; }
    public string TractionType { get; set; } = string.Empty; // Önden Çekiş, 4x4
    public string Color { get; set; } = string.Empty;
    public bool HeavyDamageRegistered { get; set; } = false; // Ağır Hasar (Pert)
    public decimal TramerDamageAmount { get; set; } = 0;
    public bool Exchangeable { get; set; } = false;
    public bool WarrantyStatus { get; set; } = false;
}

public class CreateDamageReportDto
{
    public PaintCondition FrontBumper { get; set; } = PaintCondition.Original;
    public PaintCondition Hood { get; set; } = PaintCondition.Original;
    public PaintCondition FrontLeftFender { get; set; } = PaintCondition.Original;
    public PaintCondition FrontLeftDoor { get; set; } = PaintCondition.Original;
    public PaintCondition RearLeftDoor { get; set; } = PaintCondition.Original;
    public PaintCondition RearLeftFender { get; set; } = PaintCondition.Original;
    public PaintCondition FrontRightFender { get; set; } = PaintCondition.Original;
    public PaintCondition FrontRightDoor { get; set; } = PaintCondition.Original;
    public PaintCondition RearRightDoor { get; set; } = PaintCondition.Original;
    public PaintCondition RearRightFender { get; set; } = PaintCondition.Original;
    public PaintCondition Roof { get; set; } = PaintCondition.Original;
    public PaintCondition TrunkLid { get; set; } = PaintCondition.Original;
    public PaintCondition RearBumper { get; set; } = PaintCondition.Original;
}

public class CreateListingImageDto
{
    public string StorageKey { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsMain { get; set; }
}

public class UploadImageRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
}

public class UploadImageResponseDto
{
    public string UploadUrl { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string FinalImageUrl { get; set; } = string.Empty;
}