namespace GuvenleAlSat.Business.DTOs;

public class UpdateListingDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public UpdateVehicleDetailDto? VehicleDetail { get; set; }
    public UpdateDamageReportDto? DamageReport { get; set; }
    public List<UpdateListingImageDto>? Images { get; set; }
}

public class UpdateVehicleDetailDto
{
    public int Year { get; set; }
    public int Kilometer { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool HeavyDamageRegistered { get; set; }
}

public class UpdateDamageReportDto
{
    public int FrontBumper { get; set; }
    public int RearBumper { get; set; }
    public int Hood { get; set; }
    public int Roof { get; set; }
    public int TrunkLid { get; set; }
    public int FrontLeftFender { get; set; }
    public int FrontRightFender { get; set; }
    public int RearLeftFender { get; set; }
    public int RearRightFender { get; set; }
    public int FrontLeftDoor { get; set; }
    public int FrontRightDoor { get; set; }
    public int RearLeftDoor { get; set; }
    public int RearRightDoor { get; set; }
}

public class UpdateListingImageDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}