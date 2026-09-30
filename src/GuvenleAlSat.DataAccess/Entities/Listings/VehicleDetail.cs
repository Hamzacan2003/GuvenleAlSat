using GuvenleAlSat.DataAccess.Entities.Common;
using GuvenleAlSat.DataAccess.Enums;

namespace GuvenleAlSat.DataAccess.Entities.Listings;

public class VehicleDetail : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public int Year { get; set; }
    public int Kilometer { get; set; }
    public string Transmission { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public int EnginePowerHp { get; set; }
    public int EngineCapacityCc { get; set; }
    public string TractionType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool HeavyDamageRegistered { get; set; } = false;
    public decimal TramerDamageAmount { get; set; } = 0;
    public bool Exchangeable { get; set; } = false;
    public bool WarrantyStatus { get; set; } = false;
}

public class VehicleDamageReport : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

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