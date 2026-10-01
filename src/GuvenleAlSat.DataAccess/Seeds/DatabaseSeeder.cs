using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Categories;
using GuvenleAlSat.DataAccess.Entities.Locations;
using GuvenleAlSat.DataAccess.Entities.Subscriptions;
using GuvenleAlSat.DataAccess.Enums;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.DataAccess.Seeds;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Abonelik Planları
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var plans = new List<SubscriptionPlan>
            {
                new() { Name = "Bireysel Standart", TargetUserType = UserType.Individual, MonthlyPrice = 0, MaxActiveListingCount = 1, MaxPhotosPerListing = 10, CanUploadVideo = false, HasStorefront = false, HasShowcasePriority = false, IsActive = true },
                new() { Name = "Bireysel Premium", TargetUserType = UserType.Individual, MonthlyPrice = 450, MaxActiveListingCount = 5, MaxPhotosPerListing = 20, CanUploadVideo = true, HasStorefront = false, HasShowcasePriority = true, IsActive = true },
                new() { Name = "Kurumsal Galeri Pro", TargetUserType = UserType.Corporate, MonthlyPrice = 2500, MaxActiveListingCount = 100, MaxPhotosPerListing = 35, CanUploadVideo = true, HasStorefront = true, HasShowcasePriority = true, IsActive = true }
            };
            await context.SubscriptionPlans.AddRangeAsync(plans);
            await context.SaveChangesAsync();
        }

        // 2. 81 İl, İlçe ve Mahalleler
        var cityCount = await context.Cities.CountAsync();
        if (cityCount < 81)
        {
            var existingPlates = await context.Cities.Select(c => c.PlateCode).ToListAsync();
            var allLocations = GetTurkeyLocationCatalog();

            var newCities = new List<City>();
            var newDistricts = new List<District>();
            var newNeighborhoods = new List<Neighborhood>();

            foreach (var loc in allLocations)
            {
                if (existingPlates.Contains(loc.PlateCode)) continue;

                var cityId = Guid.NewGuid();
                newCities.Add(new City { Id = cityId, PlateCode = loc.PlateCode, Name = loc.CityName });

                foreach (var distName in loc.Districts)
                {
                    var distId = Guid.NewGuid();
                    newDistricts.Add(new District { Id = distId, CityId = cityId, Name = distName });

                    var nList = GetRealisticNeighborhoodsForDistrict(loc.CityName, distName);
                    foreach (var nName in nList)
                    {
                        newNeighborhoods.Add(new Neighborhood { Id = Guid.NewGuid(), DistrictId = distId, Name = nName, ZipCode = $"{loc.PlateCode:D2}000" });
                    }
                }
            }

            if (newCities.Count > 0) await context.Cities.AddRangeAsync(newCities);
            if (newDistricts.Count > 0) await context.Districts.AddRangeAsync(newDistricts);
            if (newNeighborhoods.Count > 0) await context.Neighborhoods.AddRangeAsync(newNeighborhoods);

            await context.SaveChangesAsync();
            Console.WriteLine("[SEED]: 81 İl ve İlçeler başarıyla yüklendi.");
        }

        // 3. Kök Kategoriler (Vasıta & Emlak)
        var vasita = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "vasita" && !c.IsDeleted);
        if (vasita == null)
        {
            vasita = new Category { Id = Guid.NewGuid(), Name = "Vasıta", Slug = "vasita", DisplayOrder = 1, IsLeaf = false, IsDeleted = false };
            await context.Categories.AddAsync(vasita);
            await context.SaveChangesAsync();
        }

        var emlak = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "emlak" && !c.IsDeleted);
        if (emlak == null)
        {
            emlak = new Category { Id = Guid.NewGuid(), Name = "Emlak", Slug = "emlak", DisplayOrder = 2, IsLeaf = false, IsDeleted = false };
            await context.Categories.AddAsync(emlak);
            await context.SaveChangesAsync();
        }

        // 4. Otomobil Ağacı
        var otomobilCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "otomobil" && !c.IsDeleted);
        if (otomobilCat == null)
        {
            otomobilCat = new Category { Id = Guid.NewGuid(), Name = "Otomobil", Slug = "otomobil", ParentCategoryId = vasita.Id, DisplayOrder = 1, IsLeaf = false, IsDeleted = false };
            await context.Categories.AddAsync(otomobilCat);
            await context.SaveChangesAsync();
        }

        int otomobilBrandsCount = await context.Categories.CountAsync(c => c.ParentCategoryId == otomobilCat.Id && !c.IsDeleted);
        if (otomobilBrandsCount < 10)
        {
            var rawBrands = GetRawBrandCatalog();
            var allVehicleCategories = new List<Category>();
            int brandOrder = 1;

            foreach (var kvp in rawBrands)
            {
                string brandName = kvp.Key;
                string[] seriesList = kvp.Value;

                var brandCategory = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = brandName,
                    Slug = Slugify($"oto-{brandName}"),
                    ParentCategoryId = otomobilCat.Id,
                    DisplayOrder = brandOrder++,
                    IsLeaf = false,
                    IsDeleted = false
                };
                allVehicleCategories.Add(brandCategory);

                int seriesOrder = 1;
                foreach (var seriesName in seriesList)
                {
                    var seriesCategory = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = seriesName,
                        Slug = Slugify($"oto-{brandName}-{seriesName}"),
                        ParentCategoryId = brandCategory.Id,
                        DisplayOrder = seriesOrder++,
                        IsLeaf = false,
                        IsDeleted = false
                    };
                    allVehicleCategories.Add(seriesCategory);
                    allVehicleCategories.AddRange(GeneratePackagesForSeries(brandName, seriesName, seriesCategory.Id, false, false));
                }
            }

            await context.Categories.AddRangeAsync(allVehicleCategories);
            await context.SaveChangesAsync();
            Console.WriteLine("[SEED]: Otomobil markaları ve modelleri yüklendi.");
        }

        // 5. Arazi, SUV & Pickup Ağacı
        var suvCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "arazi-suv-pickup" && !c.IsDeleted);
        if (suvCat == null)
        {
            suvCat = new Category { Id = Guid.NewGuid(), Name = "Arazi, SUV & Pickup", Slug = "arazi-suv-pickup", ParentCategoryId = vasita.Id, DisplayOrder = 2, IsLeaf = false, IsDeleted = false };
            await context.Categories.AddAsync(suvCat);
            await context.SaveChangesAsync();
        }

        bool hasSuvBrands = await context.Categories.AnyAsync(c => c.ParentCategoryId == suvCat.Id && !c.IsDeleted);
        if (!hasSuvBrands)
        {
            var suvBrands = new Dictionary<string, string[]>
            {
                ["DACIA"] = new[] { "Duster", "Sandero Stepway", "Bigster" },
                ["NISSAN"] = new[] { "Qashqai", "X-Trail", "Juke" },
                ["CHERY"] = new[] { "Tiggo 7 Pro", "Tiggo 8 Pro", "Omoda 5" },
                ["PEUGEOT"] = new[] { "2008", "3008", "5008" },
                ["VOLKSWAGEN"] = new[] { "Tiguan", "T-Roc", "Taigo", "Touareg" },
                ["HYUNDAI"] = new[] { "Tucson", "Bayon", "Kona", "Santa Fe" },
                ["TOYOTA"] = new[] { "C-HR", "RAV4", "Corolla Cross", "Yaris Cross", "Land Cruiser" },
                ["JEEP"] = new[] { "Renegade", "Compass", "Wrangler", "Grand Cherokee" },
                ["KIA"] = new[] { "Sportage", "Stonic", "XCeed", "Sorento" },
                ["BMW"] = new[] { "X1", "X2", "X3", "X4", "X5", "X6", "X7" },
                ["MERCEDES-BENZ"] = new[] { "GLA", "GLB", "GLC", "GLE", "GLS", "G Serisi" },
                ["AUDI"] = new[] { "Q2", "Q3", "Q5", "Q7", "Q8" }
            };

            var suvList = new List<Category>();
            int order = 1;
            foreach (var kvp in suvBrands)
            {
                var bCat = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = kvp.Key,
                    Slug = Slugify($"suv-{kvp.Key}"),
                    ParentCategoryId = suvCat.Id,
                    DisplayOrder = order++,
                    IsLeaf = false,
                    IsDeleted = false
                };
                suvList.Add(bCat);

                int modelOrder = 1;
                foreach (var model in kvp.Value)
                {
                    var mCat = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = model,
                        Slug = Slugify($"suv-{kvp.Key}-{model}"),
                        ParentCategoryId = bCat.Id,
                        DisplayOrder = modelOrder++,
                        IsLeaf = false,
                        IsDeleted = false
                    };
                    suvList.Add(mCat);
                    suvList.AddRange(GeneratePackagesForSeries(kvp.Key, model, mCat.Id, true, false));
                }
            }

            await context.Categories.AddRangeAsync(suvList);
            await context.SaveChangesAsync();
            Console.WriteLine("[SEED]: Arazi, SUV & Pickup modelleri yüklendi.");
        }

        // 6. Kamyonet & Hafif Ticari Ağacı
        var ticariCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "kamyonet-hafif-ticari" && !c.IsDeleted);
        if (ticariCat == null)
        {
            ticariCat = new Category { Id = Guid.NewGuid(), Name = "Kamyonet & Hafif Ticari", Slug = "kamyonet-hafif-ticari", ParentCategoryId = vasita.Id, DisplayOrder = 3, IsLeaf = false, IsDeleted = false };
            await context.Categories.AddAsync(ticariCat);
            await context.SaveChangesAsync();
        }

        bool hasTicariBrands = await context.Categories.AnyAsync(c => c.ParentCategoryId == ticariCat.Id && !c.IsDeleted);
        if (!hasTicariBrands)
        {
            var comBrands = new Dictionary<string, string[]>
            {
                ["FORD"] = new[] { "Tourneo Courier", "Tourneo Connect", "Tourneo Custom", "Transit", "Transit Custom", "Ranger" },
                ["FIAT"] = new[] { "Doblo Combi", "Doblo Cargo", "Fiorino Combi", "Fiorino Cargo", "Ducato" },
                ["RENAULT"] = new[] { "Kangoo Multix", "Kangoo Express", "Trafic", "Master" },
                ["VOLKSWAGEN"] = new[] { "Caddy", "Transporter", "Caravelle", "Crafter", "Amarok" },
                ["PEUGEOT"] = new[] { "Rifter", "Partner", "Expert", "Boxer" },
                ["CITROEN"] = new[] { "Berlingo", "Jumpy", "Jumper" },
                ["OPEL"] = new[] { "Combo Life", "Combo Cargo", "Vivaro", "Movano" },
                ["TOYOTA"] = new[] { "Hilux", "Proace City" }
            };

            var comList = new List<Category>();
            int order = 1;
            foreach (var kvp in comBrands)
            {
                var bCat = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = kvp.Key,
                    Slug = Slugify($"ticari-{kvp.Key}"),
                    ParentCategoryId = ticariCat.Id,
                    DisplayOrder = order++,
                    IsLeaf = false,
                    IsDeleted = false
                };
                comList.Add(bCat);

                int modelOrder = 1;
                foreach (var model in kvp.Value)
                {
                    var mCat = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = model,
                        Slug = Slugify($"ticari-{kvp.Key}-{model}"),
                        ParentCategoryId = bCat.Id,
                        DisplayOrder = modelOrder++,
                        IsLeaf = false,
                        IsDeleted = false
                    };
                    comList.Add(mCat);
                    comList.AddRange(GeneratePackagesForSeries(kvp.Key, model, mCat.Id, false, true));
                }
            }

            await context.Categories.AddRangeAsync(comList);
            await context.SaveChangesAsync();
            Console.WriteLine("[SEED]: Kamyonet & Hafif Ticari modelleri yüklendi.");
        }
    }

    private static List<Category> GeneratePackagesForSeries(string brand, string series, Guid seriesId, bool isSuv, bool isCommercial)
    {
        var list = new List<Category>();
        string b = brand.ToLowerInvariant();
        string s = series.ToLowerInvariant();

        bool isEv = s.Contains("e-tron") || s.Contains("eq") || s.Contains("ev") || s.Contains("id.") ||
                    s.Contains("taycan") || b == "tesla" || b == "togg" || b == "byd" || s == "ami";

        string body = isCommercial ? "Kamyonet/Van" : (isSuv ? "SUV" : "Sedan");
        string traction = isSuv ? "4x4" : "Önden Çekiş";

        if (isEv)
        {
            list.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = $"{series} Standart Menzil (RWD)",
                Slug = Slugify($"{brand}-{series}-standart-rwd"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                IsDeleted = false,
                DefaultFuelType = "Elektrik",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = "Arkadan İtiş",
                DefaultEngineCapacityCc = 0,
                DefaultEnginePowerHp = 204
            });
            list.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = $"{series} Long Range Dual Motor (AWD)",
                Slug = Slugify($"{brand}-{series}-long-range-awd"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                IsDeleted = false,
                DefaultFuelType = "Elektrik",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = "4x4",
                DefaultEngineCapacityCc = 0,
                DefaultEnginePowerHp = 350
            });
        }
        else
        {
            list.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = $"{series} 1.0 / 1.5 Benzinli Otomatik",
                Slug = Slugify($"{brand}-{series}-benzin-otomatik"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                IsDeleted = false,
                DefaultFuelType = "Benzin",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = traction,
                DefaultEngineCapacityCc = 1498,
                DefaultEnginePowerHp = 150
            });

            list.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = $"{series} 1.5 / 2.0 Dizel Manuel",
                Slug = Slugify($"{brand}-{series}-dizel-manuel"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                IsDeleted = false,
                DefaultFuelType = "Dizel",
                DefaultTransmission = "Manuel",
                DefaultBodyType = body,
                DefaultTractionType = traction,
                DefaultEngineCapacityCc = 1598,
                DefaultEnginePowerHp = 115
            });

            list.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = $"{series} 1.6 / 1.8 Hibrit & ECO Otomatik",
                Slug = Slugify($"{brand}-{series}-hibrit-otomatik"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                IsDeleted = false,
                DefaultFuelType = (b == "dacia" || b == "fiat" || b == "honda") ? "LPG & Benzin" : "Hibrit",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = traction,
                DefaultEngineCapacityCc = 1598,
                DefaultEnginePowerHp = 130
            });
        }

        return list;
    }

    private static Dictionary<string, string[]> GetRawBrandCatalog()
    {
        return new Dictionary<string, string[]>
        {
            ["AUDI"] = new[] { "A1", "A3", "A4", "A5", "A6", "A7", "A8", "RS3", "RS6", "TT", "R8" },
            ["BMW"] = new[] { "1 Serisi", "2 Serisi", "3 Serisi", "4 Serisi", "5 Serisi", "6 Serisi", "7 Serisi", "8 Serisi", "M3", "M4", "M5", "Z4" },
            ["MERCEDES-BENZ"] = new[] { "A Serisi", "B Serisi", "C Serisi", "E Serisi", "S Serisi", "CLA", "CLE", "CLS", "AMG GT", "SL" },
            ["RENAULT"] = new[] { "Clio", "Megane", "Megane Sedan", "Symbol", "Fluence", "Taliant", "Austral", "Toros", "R 12", "R 9", "R 19" },
            ["VOLKSWAGEN"] = new[] { "Polo", "Golf", "Passat", "Passat Variant", "Jetta", "Arteon", "ID.3", "ID.4", "ID.7" },
            ["TOYOTA"] = new[] { "Yaris", "Corolla", "Corolla Cross", "Camry", "Prius", "Auris" },
            ["HONDA"] = new[] { "Civic", "City", "Accord", "Jazz" },
            ["FIAT"] = new[] { "Egea Sedan", "Egea Hatchback", "Egea Cross", "500", "Punto", "Linea", "Bravo", "Palio", "Albea" },
            ["TOFAS"] = new[] { "Şahin", "Doğan", "Kartal", "Murat 131", "Murat 124", "Serçe" },
            ["FORD"] = new[] { "Fiesta", "Focus", "Mondeo", "Mustang" },
            ["HYUNDAI"] = new[] { "i10", "i20", "i30", "Elantra", "Accent Era", "Getz" },
            ["PEUGEOT"] = new[] { "206", "207", "208", "301", "308", "408", "508" },
            ["CITROEN"] = new[] { "C3", "C4", "C4 X", "C5", "C-Elysee", "Ami" },
            ["OPEL"] = new[] { "Corsa", "Astra", "Insignia", "Vectra" },
            ["SKODA"] = new[] { "Fabia", "Scala", "Octavia", "Superb", "Rapid" },
            ["KIA"] = new[] { "Picanto", "Rio", "Ceed", "Cerato" },
            ["VOLVO"] = new[] { "S60", "S90", "V40", "V60", "V90" },
            ["TESLA"] = new[] { "Model 3", "Model S" },
            ["TOGG"] = new[] { "T10F" },
            ["SEAT"] = new[] { "Ibiza", "Leon", "Toledo" },
            ["CUPRA"] = new[] { "Leon", "Born" }
        };
    }

    private static List<string> GetRealisticNeighborhoodsForDistrict(string city, string district)
    {
        if (city == "Ankara" && district == "Çankaya")
            return new List<string> { "Kızılay", "Ayrancı", "Bahçelievler", "Tunalı Hilmi", "Çukurambar", "Ümitköy", "Bilkent", "Gaziosmanpaşa", "Yıldız", "Balgat", "Söğütözü", "Oran", "Maltepe", "Dikmen", "Kavaklıdere" };
        if (city == "Ankara" && district == "Yenimahalle")
            return new List<string> { "Batıkent", "Demetevler", "Çayyolu", "Ostim", "Ergazi", "Şentepe", "İvedik", "Karşıyaka", "Uğur Mumcu", "Yeni Batı" };
        if (city == "İstanbul" && district == "Kadıköy")
            return new List<string> { "Moda", "Caddebostan", "Fenerbahçe", "Suadiye", "Bostancı", "Göztepe", "Erenköy", "Kozyatağı", "Fikirtepe", "Acıbadem" };
        if (city == "İzmir" && district == "Karşıyaka")
            return new List<string> { "Bostanlı", "Mavişehir", "Alaybey", "Aksoy", "Bahçelievler" };

        return new List<string>
        {
            "Merkez Mah.", "Cumhuriyet Mah.", "Yeni Mah.", "Atatürk Mah.", "Fatih Mah.",
            "İnönü Mah.", "Zafer Mah.", "Hürriyet Mah.", "Yıldız Mah.", "Barış Mah.",
            "Gazi Mah.", "Yeşiltepe Mah.", "Bahçelievler Mah.", "Çamlık Mah."
        };
    }

    private static List<(int PlateCode, string CityName, string[] Districts)> GetTurkeyLocationCatalog()
    {
        return new List<(int, string, string[])>
        {
            (1, "Adana", new[] { "Aladağ", "Ceyhan", "Çukurova", "Feke", "İmamoğlu", "Karaisalı", "Karataş", "Kozan", "Pozantı", "Saimbeyli", "Sarıçam", "Seyhan", "Tufanbeyli", "Yumurtalık", "Yüreğir" }),
            (2, "Adıyaman", new[] { "Besni", "Çelikhan", "Gerger", "Gölbaşı", "Kahta", "Merkez", "Samsat", "Sincik", "Tut" }),
            (3, "Afyonkarahisar", new[] { "Başmakçı", "Bayat", "Bolvadin", "Çay", "Çobanlar", "Dazkırı", "Dinar", "Emirdağ", "Evciler", "Hocalar", "İhsaniye", "İscehisar", "Kızılören", "Merkez", "Sandıklı", "Sinanpaşa", "Sultandağı", "Şuhut" }),
            (4, "Ağrı", new[] { "Diyadin", "Doğubayazıt", "Eleşkirt", "Hamur", "Merkez", "Patnos", "Taşlıçay", "Tutak" }),
            (5, "Amasya", new[] { "Göynücek", "Gümüşhacıköy", "Hamamözü", "Merkez", "Merzifon", "Suluova", "Taşova" }),
            (6, "Ankara", new[] { "Akyurt", "Altındağ", "Ayaş", "Bala", "Beypazarı", "Çamlıdere", "Çankaya", "Çubuk", "Elmadağ", "Etimesgut", "Evren", "Gölbaşı", "Güdül", "Haymana", "Kahramankazan", "Kalecik", "Keçiören", "Kızılcahamam", "Mamak", "Nallıhan", "Polatlı", "Pursaklar", "Sincan", "Şereflikoçhisar", "Yenimahalle" }),
            (7, "Antalya", new[] { "Akseki", "Aksu", "Alanya", "Demre", "Döşemealtı", "Elmalı", "Finike", "Gazipaşa", "Gündoğmuş", "İbradı", "Kaş", "Kemer", "Kepez", "Konyaaltı", "Korkuteli", "Kumluca", "Manavgat", "Muratpaşa", "Serik" }),
            (8, "Artvin", new[] { "Ardanuç", "Arhavi", "Borçka", "Hopa", "Kemalpaşa", "Merkez", "Murgul", "Şavşat", "Yusufeli" }),
            (9, "Aydın", new[] { "Bozdoğan", "Buharkent", "Çine", "Didim", "Efeler", "Germencik", "İncirliova", "Karacasu", "Karpuzlu", "Koçarlı", "Köşk", "Kuşadası", "Kuyucak", "Nazilli", "Söke", "Sultanhisar", "Yenipazar" }),
            (10, "Balıkesir", new[] { "Altıeylül", "Ayvalık", "Balya", "Bandırma", "Bigadiç", "Burhaniye", "Dursunbey", "Edremit", "Erdek", "Gömeç", "Gönen", "Havran", "İvrindi", "Karesi", "Kepsut", "Manyas", "Marmara", "Savaştepe", "Sındırgı", "Susurluk" }),
            (11, "Bilecik", new[] { "Bozüyük", "Gölpazarı", "İnhisar", "Merkez", "Osmaneli", "Pazaryeri", "Söğüt", "Yenipazar" }),
            (12, "Bingöl", new[] { "Adaklı", "Genç", "Karlıova", "Kiğı", "Merkez", "Solhan", "Yayladere", "Yedisu" }),
            (13, "Bitlis", new[] { "Adilcevaz", "Ahlat", "Güroymak", "Hizan", "Merkez", "Mutki", "Tatvan" }),
            (14, "Bolu", new[] { "Dörtdivan", "Gerede", "Göynük", "Kıbrıscık", "Mengen", "Merkez", "Mudurnu", "Seben", "Yeniçağa" }),
            (15, "Burdur", new[] { "Ağlasun", "Altınyayla", "Bucak", "Çavdır", "Çeltikçi", "Gölhisar", "Karamanlı", "Kemer", "Merkez", "Tefenni", "Yeşilova" }),
            (16, "Bursa", new[] { "Büyükorhan", "Gemlik", "Gürsu", "Harmancık", "İnegöl", "İznik", "Karacabey", "Keles", "Kestel", "Mudanya", "Mustafakemalpaşa", "Nilüfer", "Orhaneli", "Orhangazi", "Osmangazi", "Yenişehir", "Yıldırım" }),
            (17, "Çanakkale", new[] { "Ayvacık", "Bayramiç", "Biga", "Bozcaada", "Çan", "Eceabat", "Ezine", "Gelibolu", "Gökçeada", "Lapseki", "Merkez", "Yenice" }),
            (18, "Çankırı", new[] { "Atkaracalar", "Bayramören", "Çerkeş", "Eldivan", "Ilgaz", "Kızılırmak", "Korgun", "Kurşunlu", "Merkez", "Orta", "Şabanözü", "Yapraklı" }),
            (19, "Çorum", new[] { "Alaca", "Bayat", "Boğazkale", "Dodurga", "İskilip", "Kargı", "Laçin", "Mecitözü", "Merkez", "Oğuzlar", "Ortaköy", "Osmancık", "Sungurlu", "Uğurludağ" }),
            (20, "Denizli", new[] { "Acıpayam", "Babadağ", "Baklan", "Bekilli", "Beyağaç", "Bozkurt", "Buldan", "Çal", "Çameli", "Çardak", "Çivril", "Güney", "Honaz", "Kale", "Merkezefendi", "Pamukkale", "Sarayköy", "Serinhisar", "Tavas" }),
            (21, "Diyarbakır", new[] { "Bağlar", "Bismil", "Çermik", "Çınar", "Çüngüş", "Dicle", "Eğil", "Ergani", "Hani", "Hazro", "Kayapınar", "Kocaköy", "Kulp", "Lice", "Silvan", "Sur", "Yenişehir" }),
            (22, "Edirne", new[] { "Enez", "Havsa", "İpsala", "Keşan", "Lalapaşa", "Meriç", "Merkez", "Süloğlu", "Uzunköprü" }),
            (23, "Elazığ", new[] { "Ağın", "Alacakaya", "Arıcak", "Baskil", "Karakoçan", "Keban", "Kovancılar", "Maden", "Merkez", "Palu", "Sivrice" }),
            (24, "Erzincan", new[] { "Çayırlı", "İliç", "Kemah", "Kemaliye", "Merkez", "Otlukbeli", "Refahiye", "Tercan", "Üzümlü" }),
            (25, "Erzurum", new[] { "Aşkale", "Aziziye", "Çat", "Hınıs", "Horasan", "İspir", "Karaçoban", "Karayazı", "Köprüköy", "Narman", "Oltu", "Olur", "Palandöken", "Pasinler", "Pazaryolu", "Şenkaya", "Tekman", "Tortum", "Uzundere", "Yakutiye" }),
            (26, "Eskişehir", new[] { "Alpu", "Beylikova", "Çifteler", "Günyüzü", "Han", "İnönü", "Mahmudiye", "Mihalgazi", "Mihalıççık", "Odunpazarı", "Seyitgazi", "Sivrihisar", "Tepebaşı" }),
            (27, "Gaziantep", new[] { "Araban", "İslahiye", "Karkamış", "Nizip", "Nurdağı", "Oğuzeli", "Şahinbey", "Şehitkamil", "Yavuzeli" }),
            (28, "Giresun", new[] { "Alucra", "Bulancak", "Çamoluk", "Çanakçı", "Dereli", "Doğankent", "Espiye", "Eynesil", "Görele", "Güce", "Keşap", "Merkez", "Piraziz", "Şebinkarahisar", "Tirebolu", "Yağlıdere" }),
            (29, "Gümüşhane", new[] { "Kelkit", "Köse", "Kürtün", "Merkez", "Şiran", "Torul" }),
            (30, "Hakkari", new[] { "Çukurca", "Derecik", "Merkez", "Şemdinli", "Yüksekova" }),
            (31, "Hatay", new[] { "Altınözü", "Antakya", "Arsuz", "Belen", "Defne", "Dörtyol", "Erzin", "Hassa", "İskenderun", "Kırıkhan", "Kumlu", "Payas", "Reyhanlı", "Samandağ", "Yayladağı" }),
            (32, "Isparta", new[] { "Aksu", "Atabey", "Eğirdir", "Gelendost", "Gönen", "Keçiborlu", "Merkez", "Senirkent", "Sütçüler", "Şarkikaraağaç", "Uluborlu", "Yalvaç", "Yenişarbademli" }),
            (33, "Mersin", new[] { "Akdeniz", "Anamur", "Aydıncık", "Bozyazı", "Çamlıyayla", "Erdemli", "Gülnar", "Mezitli", "Mut", "Silifke", "Tarsus", "Toroslar", "Yenişehir" }),
            (34, "İstanbul", new[] { "Adalar", "Arnavutköy", "Ataşehir", "Avcılar", "Bağcılar", "Bahçelievler", "Bakırköy", "Başakşehir", "Bayrampaşa", "Beşiktaş", "Beykoz", "Beylikdüzü", "Beyoğlu", "Büyükçekmece", "Çatalca", "Çekmeköy", "Esenler", "Esenyurt", "Eyüpsultan", "Fatih", "Gaziosmanpaşa", "Güngören", "Kadıköy", "Kağıthane", "Kartal", "Küçükçekmece", "Maltepe", "Pendik", "Sancaktepe", "Sarıyer", "Silivri", "Sultanbeyli", "Sultangazi", "Şile", "Şişli", "Tuzla", "Ümraniye", "Üsküdar", "Zeytinburnu" }),
            (35, "İzmir", new[] { "Aliağa", "Balçova", "Bayındır", "Bayraklı", "Bergama", "Beydağ", "Bornova", "Buca", "Çeşme", "Çiğli", "Dikili", "Foça", "Gaziemir", "Güzelbahçe", "Karabağlar", "Karaburun", "Karşıyaka", "Kemalpaşa", "Kınık", "Kiraz", "Konak", "Menderes", "Menemen", "Narlıdere", "Ödemiş", "Seferihisar", "Selçuk", "Tire", "Torbalı", "Urla" }),
            (36, "Kars", new[] { "Akyaka", "Arpaçay", "Digor", "Kağızman", "Merkez", "Saraykent", "Selim", "Susuz" }),
            (37, "Kastamonu", new[] { "Abana", "Ağlı", "Araç", "Bozkurt", "Cide", "Çatalzeytin", "Daday", "Devrekani", "Doğanyurt", "Hanönü", "İhsangazi", "İnebolu", "Küre", "Merkez", "Pınarbaşı", "Seydiler", "Şenpazar", "Taşköprü", "Tosya" }),
            (38, "Kayseri", new[] { "Akkışla", "Bünyan", "Develi", "Felahiye", "Hacılar", "İncesu", "Kocasinan", "Melikgazi", "Özvatan", "Pınarbaşı", "Sarıoğlan", "Sarız", "Talas", "Tomarza", "Yahyalı", "Yeşilhisar" }),
            (39, "Kırklareli", new[] { "Babaeski", "Demirköy", "Kofçaz", "Lüleburgaz", "Merkez", "Pehlivanköy", "Pınarhisar", "Vize" }),
            (40, "Kırşehir", new[] { "Akçakent", "Akpınar", "Boztepe", "Çiçekdağı", "Kaman", "Merkez", "Mucur" }),
            (41, "Kocaeli", new[] { "Başiskele", "Çayırova", "Darıca", "Derince", "Dilovası", "Gebze", "Gölcük", "İzmit", "Kandıra", "Karamürsel", "Kartepe", "Körfez" }),
            (42, "Konya", new[] { "Ahırlı", "Akören", "Akşehir", "Altınekin", "Beyşehir", "Bozkır", "Cihanbeyli", "Çeltik", "Çumra", "Derbent", "Derebucak", "Doğanhisar", "Emirgazi", "Ereğli", "Güneysınır", "Hadim", "Halkapınar", "Hüyük", "Ilgın", "Kadınhanı", "Karapınar", "Karatay", "Kulu", "Meram", "Sarayönü", "Selçuklu", "Seydişehir", "Taşkent", "Tuzlukçu", "Yalıhüyük", "Yunak" }),
            (43, "Kütahya", new[] { "Altıntaş", "Aslanapa", "Çavdarhisar", "Domaniç", "Dumlupınar", "Emet", "Gediz", "Hisarcık", "Merkez", "Pazarlar", "Şaphane", "Simav", "Tavşanlı" }),
            (44, "Malatya", new[] { "Akçadağ", "Arapgir", "Arguvan", "Battalgazi", "Darende", "Doğanşehir", "Doğanyol", "Hekimhan", "Kale", "Kuluncak", "Pütürge", "Yazıhan", "Yeşilyurt" }),
            (45, "Manisa", new[] { "Ahmetli", "Akhisar", "Alaşehir", "Demirci", "Gölmarmara", "Gördes", "Kırkağaç", "Köprübaşı", "Kula", "Salihli", "Sarıgöl", "Saruhanlı", "Selendi", "Soma", "Şehzadeler", "Turgutlu", "Yunusemre" }),
            (46, "Kahramanmaraş", new[] { "Afşin", "Andırın", "Çağlayancerit", "Dulkadiroğlu", "Ekinözü", "Elbistan", "Göksun", "Nurhak", "Onikişubat", "Pazarcık", "Türkoğlu" }),
            (47, "Mardin", new[] { "Artuklu", "Dargeçit", "Derik", "Kızıltepe", "Mazıdağı", "Midyat", "Nusaybin", "Ömerli", "Savur", "Yeşilli" }),
            (48, "Muğla", new[] { "Bodrum", "Dalaman", "Datça", "Fethiye", "Kavaklıdere", "Köyceğiz", "Marmaris", "Menteşe", "Milas", "Ortaca", "Seydikemer", "Ula", "Yatağan" }),
            (49, "Muş", new[] { "Bulanık", "Hasköy", "Korkut", "Malazgirt", "Merkez", "Varto" }),
            (50, "Nevşehir", new[] { "Acıgöl", "Avanos", "Derinkuyu", "Gülşehir", "Hacıbektaş", "Kozaklı", "Merkez", "Ürgüp" }),
            (51, "Niğde", new[] { "Altunhisar", "Bor", "Çamardı", "Çiftlik", "Merkez", "Ulukışla" }),
            (52, "Ordu", new[] { "Akkuş", "Altınordu", "Aybastı", "Çamaş", "Çatalpınar", "Çaybaşı", "Fatsa", "Gölköy", "Gülyalı", "Gürgentepe", "İkizce", "Kabadüz", "Kabataş", "Korgan", "Kumru", "Mesudiye", "Perşembe", "Ulubey", "Ünye" }),
            (53, "Rize", new[] { "Ardeşen", "Çamlıhemşin", "Çayeli", "Derepazarı", "Fındıklı", "Güneysu", "Hemşin", "İkizdere", "İyidere", "Kalkandere", "Merkez", "Pazar" }),
            (54, "Sakarya", new[] { "Adapazarı", "Akyazı", "Arifiye", "Erenler", "Ferizli", "Geyve", "Hendek", "Karapürçek", "Karasu", "Kaynarca", "Kocaali", "Pamukova", "Sapanca", "Serdivan", "Söğütlü", "Taraklı" }),
            (55, "Samsun", new[] { "Alaçam", "Asarcık", "Atakum", "Ayvacık", "Bafra", "Canik", "Çarşamba", "Havza", "İlkadım", "Kavak", "Ladik", "Ondokuzmayıs", "Salıpazarı", "Tekkeköy", "Terme", "Vezirköprü", "Yakakent" }),
            (56, "Siirt", new[] { "Baykan", "Eruh", "Kurtalan", "Merkez", "Pervari", "Şirvan", "Tillo" }),
            (57, "Sinop", new[] { "Ayancık", "Boyabat", "Dikmen", "Durağan", "Erfelek", "Gerze", "Merkez", "Saraydüzü", "Türkeli" }),
            (58, "Sivas", new[] { "Akıncılar", "Altınyayla", "Divriği", "Doğanşar", "Gemerek", "Gölova", "Gürün", "Hafik", "İmranlı", "Kangal", "Koyulhisar", "Merkez", "Suşehri", "Şarkışla", "Ulaş", "Yıldızeli", "Zara" }),
            (59, "Tekirdağ", new[] { "Çerkezköy", "Çorlu", "Ergene", "Hayrabolu", "Kapaklı", "Malkara", "Marmaraereğlisi", "Muratlı", "Saray", "Süleymanpaşa", "Şarköy" }),
            (60, "Tokat", new[] { "Almus", "Artova", "Başçiftlik", "Erbaa", "Merkez", "Niksar", "Pazar", "Reşadiye", "Sulusaray", "Turhal", "Yeşilyurt", "Zile" }),
            (61, "Trabzon", new[] { "Akçaabat", "Araklı", "Arsin", "Beşikdüzü", "Çarşıbaşı", "Çaykara", "Dernekpazarı", "Düzköy", "Hayrat", "Köprübaşı", "Maçka", "Of", "Ortahisar", "Sürmene", "Şalpazarı", "Tonya", "Vakfıkebir", "Yomra" }),
            (62, "Tunceli", new[] { "Çemişgezek", "Hozat", "Mazgirt", "Merkez", "Nazımiye", "Ovacık", "Pertek", "Pülümür" }),
            (63, "Şanlıurfa", new[] { "Akçakale", "Birecik", "Bozova", "Ceylanpınar", "Eyyübiye", "Halfeti", "Haliliye", "Harran", "Hilvan", "Karaköprü", "Siverek", "Suruç", "Viranşehir" }),
            (64, "Uşak", new[] { "Banaz", "Eşme", "Karahallı", "Merkez", "Sivaslı", "Ulubey" }),
            (65, "Van", new[] { "Bahçesaray", "Başkale", "Çaldıran", "Çatak", "Edremit", "Erciş", "Gevaş", "Gürpınar", "İpekyolu", "Muradiye", "Özalp", "Saray", "Tuşba" }),
            (66, "Yozgat", new[] { "Akdağmadeni", "Aydıncık", "Boğazlıyan", "Çandır", "Çayıralan", "Çekerek", "Kadışehri", "Saraykent", "Sarıkaya", "Sorgun", "Şefaatli", "Yenifakılı", "Yerköy", "Merkez" }),
            (67, "Zonguldak", new[] { "Alaplı", "Çaycuma", "Devrek", "Gökçebey", "Karadeniz Ereğli", "Kilimli", "Kozlu", "Merkez" }),
            (68, "Aksaray", new[] { "Ağaçören", "Eskil", "Gülağaç", "Güzelyurt", "Merkez", "Ortaköy", "Sarıyahşi", "Sultanhanı" }),
            (69, "Bayburt", new[] { "Aydıntepe", "Demirözü", "Merkez" }),
            (70, "Karaman", new[] { "Ayrancı", "Başyayla", "Ermenek", "Kazımkarabekir", "Merkez", "Sarıveliler" }),
            (71, "Kırıkkale", new[] { "Bahşılı", "Balışeyh", "Çelebi", "Delice", "Karakeçili", "Keskin", "Merkez", "Sulakyurt", "Yahşihan" }),
            (72, "Batman", new[] { "Beşiri", "Gercüş", "Hasankeyf", "Kozluk", "Merkez", "Sason" }),
            (73, "Şırnak", new[] { "Beytüşşebap", "Cizre", "Güçlükonak", "İdil", "Merkez", "Silopi", "Uludere" }),
            (74, "Bartın", new[] { "Amasra", "Kurucaşile", "Merkez", "Ulus" }),
            (75, "Ardahan", new[] { "Çıldır", "Damal", "Göle", "Hanak", "Merkez", "Posof" }),
            (76, "Iğdır", new[] { "Aralık", "Karakoyunlu", "Merkez", "Tuzluca" }),
            (77, "Yalova", new[] { "Altınova", "Armutlu", "Çınarcık", "Çiftlikköy", "Merkez", "Termal" }),
            (78, "Karabük", new[] { "Eflani", "Eskipazar", "Merkez", "Ovacık", "Safranbolu", "Yenice" }),
            (79, "Kilis", new[] { "Elbeyli", "Merkez", "Musabeyli", "Polateli" }),
            (80, "Osmaniye", new[] { "Bahçe", "Düziçi", "Hasanbeyli", "Kadirli", "Merkez", "Sumbas", "Toprakkale" }),
            (81, "Düzce", new[] { "Akçakoca", "Cumayeri", "Çilimli", "Gölyaka", "Gümüşova", "Kaynaşlı", "Merkez", "Yığılca" })
        };
    }

    private static string Slugify(string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u")
            .Replace("/", "-")
            .Replace(":", "-")
            .Replace(".", "-")
            .Replace("(", "")
            .Replace(")", "");
    }
}