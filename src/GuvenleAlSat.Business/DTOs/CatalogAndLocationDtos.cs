namespace GuvenleAlSat.Business.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsLeaf { get; set; }
}

public class VehicleModelPresetDto
{
    public Guid CategoryId { get; set; }
    public string CategoryFullName { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public string TractionType { get; set; } = string.Empty;
    public int EnginePowerHp { get; set; }
    public int EngineCapacityCc { get; set; }
}

public class CityDto
{
    public Guid Id { get; set; }
    public int PlateCode { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DistrictDto
{
    public Guid Id { get; set; }
    public Guid CityId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NeighborhoodDto
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Sahibinden Sol Menü / İlan Verme Parametre Seçenekleri
public class VehicleFilterOptionsDto
{
    public List<string> FuelTypes { get; set; } = new() { "Benzin", "Dizel", "LPG & Benzin", "Hibrit", "Elektrik" };
    public List<string> Transmissions { get; set; } = new() { "Manuel", "Otomatik", "Yarı Otomatik" };
    public List<string> BodyTypes { get; set; } = new() { "Sedan", "Hatchback 5 Kapı", "Hatchback 3 Kapı", "Station Wagon", "SUV", "Crossover", "Coupe", "Cabrio", "Pick-up", "Panelvan", "Minibüs" };
    public List<string> TractionTypes { get; set; } = new() { "Önden Çekiş", "Arkadan İtiş", "4x4" };
    public List<string> EngineCapacities { get; set; } = new() { "1300 cc'ye kadar", "1301 - 1600 cc", "1601 - 1800 cc", "1801 - 2000 cc", "2001 - 2500 cc", "2501 - 3000 cc", "3001 cc ve üzeri" };
    public List<string> EnginePowers { get; set; } = new() { "75 HP'ye kadar", "76 - 100 HP", "101 - 125 HP", "126 - 150 HP", "151 - 175 HP", "176 - 200 HP", "201 - 250 HP", "251 - 300 HP", "301 HP ve üzeri" };
}