namespace GuvenleAlSat.DataAccess.Entities.Vehicles;

public class VehicleBrand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Otomobil"; // Otomobil, SUV, Ticari
    public ICollection<VehicleSeries> Series { get; set; } = new List<VehicleSeries>();
}

public class VehicleSeries
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public VehicleBrand Brand { get; set; } = null!;
    public string Name { get; set; } = string.Empty; // Clio, Megane, Passat, Egea...
    public ICollection<VehicleModelTrim> Trims { get; set; } = new List<VehicleModelTrim>();
}

public class VehicleModelTrim
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public VehicleSeries Series { get; set; } = null!;
    public string Name { get; set; } = string.Empty; // "1.0 TCe Touch", "1.3 TCe Icon", "1.6 Multijet Easy"...
}