namespace GuvenleAlSat.DataAccess.Entities.Listings;

public class RealEstateDetail
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }

    public int? GrossSquareMeters { get; set; } // Brüt m²
    public int? NetSquareMeters { get; set; }   // Net m²
    public string? RoomCount { get; set; }      // 1+1, 2+1, 3+1 vb.
    public int? BuildingAge { get; set; }       // Bina Yaşı
    public int? FloorLocation { get; set; }     // Bulunduğu Kat
    public int? TotalFloors { get; set; }       // Toplam Kat Sayısı
    public string? HeatingType { get; set; }    // Isıtma Tipi
    public int? BathroomCount { get; set; }     // Banyo Sayısı
    public bool HasBalcony { get; set; }        // Balkon Var mı?
    public bool IsFurnished { get; set; }       // Eşyalı mı?
    public bool InSite { get; set; }            // Site İçerisinde mi?

    public virtual Listing Listing { get; set; } = null!;
}