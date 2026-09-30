using GuvenleAlSat.DataAccess.Entities.Common;
using GuvenleAlSat.DataAccess.Entities.Listings;

namespace GuvenleAlSat.DataAccess.Entities.Categories;

public class Category : BaseEntity
{
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsLeaf { get; set; } = false;

    // Sahibinden gibi: Paket seçildiğinde otomatik gelebilecek fabrika şablonları
    public string? DefaultFuelType { get; set; }
    public string? DefaultTransmission { get; set; }
    public string? DefaultBodyType { get; set; }
    public string? DefaultTractionType { get; set; }
    public int? DefaultEngineCapacityCc { get; set; }
    public int? DefaultEnginePowerHp { get; set; }

    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
}