using GuvenleAlSat.DataAccess.Entities.Common;

namespace GuvenleAlSat.DataAccess.Entities.Locations;

public class City : BaseEntity
{
    public int PlateCode { get; set; } // 06
    public string Name { get; set; } = string.Empty; // Ankara
    public ICollection<District> Districts { get; set; } = new List<District>();
}

public class District : BaseEntity
{
    public Guid CityId { get; set; }
    public City City { get; set; } = null!;
    public string Name { get; set; } = string.Empty; // Beypazarı, Çankaya vb.
    public ICollection<Neighborhood> Neighborhoods { get; set; } = new List<Neighborhood>();
}

public class Neighborhood : BaseEntity
{
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;
    public string Name { get; set; } = string.Empty; // Başağaç Mah., Hacıkara Mah. vb.
    public string? ZipCode { get; set; }
}