using GuvenleAlSat.DataAccess.Enums;

namespace GuvenleAlSat.Business.DTOs;

public class ListingFilterRequestDto
{
    // Kategori
    public Guid? CategoryId { get; set; }

    // Lokasyon
    public string? City { get; set; }
    public string? District { get; set; }

    // Aralık Filtreleri
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public int? MinKm { get; set; }
    public int? MaxKm { get; set; }

    // Araç Teknik Filtreleri (Çoklu Seçim Desteği)
    public List<string>? FuelTypes { get; set; }
    public List<string>? Transmissions { get; set; }
    public List<string>? BodyTypes { get; set; }
    public List<string>? TractionTypes { get; set; }

    // Ekspertiz ve Hasar Filtreleri
    public bool? OnlyUndamaged { get; set; } // Tamamen Hatasız/Boyasız
    public bool? NoReplacedParts { get; set; } // Değişensiz
    public bool? ExcludeHeavyDamage { get; set; } // Ağır hasar kayıtlıları gizle

    // Metin Arama (Başlıkta veya İlan No'da ara)
    public string? SearchQuery { get; set; }

    // Sıralama (price_asc, price_desc, date_desc, km_asc)
    public string SortBy { get; set; } = "date_desc";

    // Sayfalama
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ListingCardDto
{
    public Guid Id { get; set; }
    public long ListingNo { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public CurrencyType Currency { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }

    // Araç Özet Bilgileri (Kart üzerinde görünen)
    public int? Year { get; set; }
    public int? Kilometer { get; set; }
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public bool HeavyDamageRegistered { get; set; }

    // Kurumsal Galeri Bilgisi
    public bool IsStore { get; set; }
    public string? StoreName { get; set; }
}

public class PagedListingResultDto
{
    public List<ListingCardDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public int PageSize { get; set; }

    // Sol Menü Dinamik Sayaçları (Faceted Count)
    public Dictionary<string, int> FuelTypeCounts { get; set; } = new();
    public Dictionary<string, int> TransmissionCounts { get; set; } = new();
}