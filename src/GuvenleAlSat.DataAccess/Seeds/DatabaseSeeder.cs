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
        // 1. Abonelik Paketleri
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

        // 2. 81 İl ve Tüm İlçelerinin Seed Edilmesi
        if (await context.Cities.CountAsync() < 81)
        {
            var existingCities = await context.Cities.Include(c => c.Districts).ToListAsync();
            var allLocations = GetTurkeyLocationCatalog();

            foreach (var loc in allLocations)
            {
                var city = existingCities.FirstOrDefault(c => c.PlateCode == loc.PlateCode);
                if (city == null)
                {
                    city = new City
                    {
                        Id = Guid.NewGuid(),
                        PlateCode = loc.PlateCode,
                        Name = loc.CityName
                    };
                    await context.Cities.AddAsync(city);
                    await context.SaveChangesAsync();
                }

                var existingDistricts = await context.Districts
                    .Where(d => d.CityId == city.Id)
                    .Select(d => d.Name.ToLower())
                    .ToListAsync();

                var newDistricts = new List<District>();
                foreach (var distName in loc.Districts)
                {
                    if (!existingDistricts.Contains(distName.ToLower()))
                    {
                        newDistricts.Add(new District
                        {
                            Id = Guid.NewGuid(),
                            CityId = city.Id,
                            Name = distName
                        });
                    }
                }

                if (newDistricts.Count > 0)
                {
                    await context.Districts.AddRangeAsync(newDistricts);
                    await context.SaveChangesAsync();

                    // Her ilçeye standart ana mahalleleri ekle
                    var newNeighborhoods = new List<Neighborhood>();
                    foreach (var d in newDistricts)
                    {
                        newNeighborhoods.Add(new Neighborhood { Id = Guid.NewGuid(), DistrictId = d.Id, Name = "Merkez Mah.", ZipCode = $"{loc.PlateCode:D2}000" });
                        newNeighborhoods.Add(new Neighborhood { Id = Guid.NewGuid(), DistrictId = d.Id, Name = "Cumhuriyet Mah.", ZipCode = $"{loc.PlateCode:D2}001" });
                        newNeighborhoods.Add(new Neighborhood { Id = Guid.NewGuid(), DistrictId = d.Id, Name = "Yeni Mah.", ZipCode = $"{loc.PlateCode:D2}002" });
                    }
                    await context.Neighborhoods.AddRangeAsync(newNeighborhoods);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 3. Vasıta Kategori Ağacı ve Tüm Araçlar
        if (!await context.Categories.AnyAsync())
        {
            var vasita = new Category { Name = "Vasıta", Slug = "vasita", DisplayOrder = 1, IsLeaf = false };
            var emlak = new Category { Name = "Emlak", Slug = "emlak", DisplayOrder = 2, IsLeaf = false };
            await context.Categories.AddRangeAsync(vasita, emlak);
            await context.SaveChangesAsync();

            var otomobil = new Category { Name = "Otomobil", Slug = "otomobil", ParentCategoryId = vasita.Id, DisplayOrder = 1, IsLeaf = false };
            var suv = new Category { Name = "Arazi, SUV & Pickup", Slug = "arazi-suv-pickup", ParentCategoryId = vasita.Id, DisplayOrder = 2, IsLeaf = false };
            var ticari = new Category { Name = "Ticari Araçlar (Kamyonet/Van)", Slug = "ticari-araclar", ParentCategoryId = vasita.Id, DisplayOrder = 3, IsLeaf = false };
            await context.Categories.AddRangeAsync(otomobil, suv, ticari);
            await context.SaveChangesAsync();

            var rawBrands = GetRawBrandCatalog();

            int brandOrder = 1;
            foreach (var kvp in rawBrands)
            {
                string brandName = kvp.Key;
                string[] seriesList = kvp.Value;

                var brandCategory = new Category
                {
                    Name = brandName,
                    Slug = Slugify(brandName),
                    ParentCategoryId = otomobil.Id,
                    DisplayOrder = brandOrder++,
                    IsLeaf = false
                };
                await context.Categories.AddAsync(brandCategory);
                await context.SaveChangesAsync();

                int seriesOrder = 1;
                foreach (var seriesName in seriesList)
                {
                    bool isSuvSeries = IsSuvName(seriesName);
                    bool isCommercialSeries = IsCommercialName(seriesName);

                    var seriesCategory = new Category
                    {
                        Name = seriesName,
                        Slug = Slugify($"{brandName}-{seriesName}"),
                        ParentCategoryId = brandCategory.Id,
                        DisplayOrder = seriesOrder++,
                        IsLeaf = false
                    };
                    await context.Categories.AddAsync(seriesCategory);
                    await context.SaveChangesAsync();

                    var subPackages = GeneratePackagesForSeries(brandName, seriesName, seriesCategory.Id, isSuvSeries, isCommercialSeries);
                    await context.Categories.AddRangeAsync(subPackages);
                    await context.SaveChangesAsync();
                }
            }
        }
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

    private static bool IsSuvName(string name)
    {
        var n = name.ToLowerInvariant();
        return n.StartsWith("q") || n.StartsWith("x") || n.StartsWith("gl") || n.Contains("cross") ||
               n.Contains("suv") || n.Contains("duster") || n.Contains("tucson") || n.Contains("sportage") ||
               n.Contains("tiguan") || n.Contains("kuga") || n.Contains("puma") || n.Contains("qashqai") ||
               n.Contains("rav4") || n.Contains("defender") || n.Contains("range rover") || n.Contains("cherokee") ||
               n.Contains("wrangler") || n.Contains("t10x") || n.Contains("model y") || n.Contains("formentor") ||
               n.Contains("omoda") || n.Contains("tiggo") || n.Contains("atto") || n.Contains("sealion") ||
               n.Contains("cayenne") || n.Contains("macan") || n.Contains("urus") || n.Contains("bentayga");
    }

    private static bool IsCommercialName(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("caddy") || n.Contains("transporter") || n.Contains("caravelle") || n.Contains("multivan") ||
               n.Contains("kangoo") || n.Contains("trafic") || n.Contains("master") || n.Contains("doblo") ||
               n.Contains("fiorino") || n.Contains("ducato") || n.Contains("transit") || n.Contains("courier") ||
               n.Contains("custom") || n.Contains("ranger") || n.Contains("hilux") || n.Contains("l200") ||
               n.Contains("navara") || n.Contains("berlingo") || n.Contains("partner") || n.Contains("rifter") ||
               n.Contains("combo") || n.Contains("proace");
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
                Name = $"{series} Standart Menzil (RWD)",
                Slug = Slugify($"{brand}-{series}-standart-rwd"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                DefaultFuelType = "Elektrik",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = "Arkadan İtiş",
                DefaultEngineCapacityCc = 0,
                DefaultEnginePowerHp = 204
            });
            list.Add(new Category
            {
                Name = $"{series} Long Range Dual Motor (AWD)",
                Slug = Slugify($"{brand}-{series}-long-range-awd"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                DefaultFuelType = "Elektrik",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = "4x4",
                DefaultEngineCapacityCc = 0,
                DefaultEnginePowerHp = 350
            });
        }
        else if (b == "ferrari" || b == "lamborghini" || b == "aston martin" || b == "bentley" || b == "porsche" || b == "maserati" || b == "mclaren")
        {
            list.Add(new Category
            {
                Name = $"{series} V8 Bi-Turbo Performance",
                Slug = Slugify($"{brand}-{series}-v8-biturbo"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                DefaultFuelType = "Benzin",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = isSuv ? "SUV" : "Coupe",
                DefaultTractionType = "4x4",
                DefaultEngineCapacityCc = 3996,
                DefaultEnginePowerHp = 650
            });
        }
        else
        {
            list.Add(new Category
            {
                Name = $"{series} 1.0 / 1.5 Benzinli Otomatik",
                Slug = Slugify($"{brand}-{series}-benzin-otomatik"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                DefaultFuelType = "Benzin",
                DefaultTransmission = "Otomatik",
                DefaultBodyType = body,
                DefaultTractionType = traction,
                DefaultEngineCapacityCc = 1498,
                DefaultEnginePowerHp = 150
            });

            list.Add(new Category
            {
                Name = $"{series} 1.5 / 2.0 Dizel Manuel",
                Slug = Slugify($"{brand}-{series}-dizel-manuel"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                DefaultFuelType = "Dizel",
                DefaultTransmission = "Manuel",
                DefaultBodyType = body,
                DefaultTractionType = traction,
                DefaultEngineCapacityCc = 1598,
                DefaultEnginePowerHp = 115
            });

            list.Add(new Category
            {
                Name = $"{series} 1.6 / 1.8 Hibrit & ECO Otomatik",
                Slug = Slugify($"{brand}-{series}-hibrit-otomatik"),
                ParentCategoryId = seriesId,
                IsLeaf = true,
                DefaultFuelType = b == "dacia" || b == "fiat" || b == "honda" ? "LPG & Benzin" : "Hibrit",
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
            ["AUDI"] = new[] { "A1", "A3", "A4", "A5", "A6", "A7", "A8", "Q2", "Q3", "Q4 e-tron", "Q5", "Q6 e-tron", "Q7", "Q8", "e-tron", "e-tron GT", "RS3", "RS4", "RS5", "RS6", "RS7", "RS Q3", "RS Q8", "S3", "S4", "S5", "S6", "S7", "S8", "TT", "R8" },
            ["BMW"] = new[] { "1 Serisi", "2 Serisi", "3 Serisi", "4 Serisi", "5 Serisi", "6 Serisi", "7 Serisi", "8 Serisi", "X1", "X2", "X3", "X4", "X5", "X6", "X7", "XM", "Z4", "iX1", "iX2", "iX3", "i4", "i5", "i7", "iX", "i3", "M2", "M3", "M4", "M5", "M8" },
            ["MERCEDES-BENZ"] = new[] { "A Serisi", "B Serisi", "C Serisi", "E Serisi", "S Serisi", "CLA", "CLE", "CLS", "GLA", "GLB", "GLC", "GLE", "GLS", "G Serisi", "EQA", "EQB", "EQC", "EQE", "EQE SUV", "EQS", "EQS SUV", "AMG GT", "SL" },
            ["RENAULT"] = new[] { "Clio", "Clio E-Tech", "Megane", "Megane E-Tech", "Megane Sedan", "Symbol", "Fluence", "Taliant", "Captur", "Arkana", "Austral", "Kadjar", "Koleos", "Duster", "Espace", "Scenic", "Rafale", "5 E-Tech", "Kangoo", "Kangoo Multix", "Kangoo Van", "Trafic", "Master" },
            ["VOLKSWAGEN"] = new[] { "Polo", "Golf", "Golf Variant", "Passat", "Jetta", "Arteon", "T-Roc", "T-Cross", "Taigo", "Tiguan", "Touareg", "Touran", "Caddy", "Transporter", "Caravelle", "Multivan", "ID.3", "ID.4", "ID.5", "ID.7", "ID.7 Tourer", "ID.Buzz" },
            ["TOYOTA"] = new[] { "Yaris", "Yaris Hybrid", "Yaris Cross", "Corolla", "Corolla Hybrid", "Corolla Hatchback", "Corolla Touring Sports", "Camry", "C-HR", "C-HR Hybrid", "RAV4", "RAV4 Hybrid", "Highlander", "Land Cruiser", "Land Cruiser Prado", "Aygo", "Aygo X", "Prius", "bZ4X", "Proace", "Proace City", "Hilux" },
            ["HONDA"] = new[] { "Civic", "City", "Jazz", "Jazz Crosstar", "HR-V", "ZR-V", "CR-V", "e:Ny1", "Accord", "Prelude" },
            ["FIAT"] = new[] { "Egea Sedan", "Egea Hatchback", "Egea Cross", "Egea Cross Wagon", "500", "500e", "500X", "500L", "Panda", "Grande Panda", "Tipo", "Punto", "Doblo", "Fiorino", "Ducato" },
            ["FORD"] = new[] { "Fiesta", "Focus", "Mondeo", "Puma", "Kuga", "EcoSport", "Explorer", "Capri", "Mustang", "Mustang Mach-E", "Bronco", "Ranger", "Ranger Raptor", "Transit", "Tourneo Courier", "Tourneo Connect", "Tourneo Custom", "Transit Custom" },
            ["HYUNDAI"] = new[] { "i10", "i20", "i20 N", "i30", "Bayon", "Kona", "Kona Electric", "Tucson", "Santa Fe", "Ioniq", "Ioniq 5", "Ioniq 6", "Ioniq 9", "Nexo", "Staria" },
            ["PEUGEOT"] = new[] { "108", "208", "e-208", "2008", "e-2008", "308", "308 SW", "408", "508", "3008", "e-3008", "5008", "e-5008", "Partner", "Rifter", "Expert", "Boxer" },
            ["CITROEN"] = new[] { "C1", "C3", "C3 Aircross", "C4", "C4 X", "C5", "C5 Aircross", "C5 X", "C-Elysee", "Berlingo", "Jumpy", "Jumper", "Ami", "e-C3", "e-C4", "e-C4 X" },
            ["OPEL"] = new[] { "Corsa", "Astra", "Astra Sports Tourer", "Mokka", "Crossland", "Frontera", "Grandland", "Insignia", "Combo", "Vivaro", "Movano" },
            ["SKODA"] = new[] { "Fabia", "Scala", "Rapid", "Octavia", "Superb", "Kamiq", "Karoq", "Kodiaq", "Enyaq", "Enyaq Coupe", "Elroq", "Kushaq" },
            ["KIA"] = new[] { "Picanto", "Rio", "Ceed", "Ceed SW", "Proceed", "Stonic", "XCeed", "Niro", "Niro EV", "Sportage", "Sorento", "EV3", "EV4", "EV5", "EV6", "EV9", "Carnival" },
            ["NISSAN"] = new[] { "Micra", "Juke", "Qashqai", "X-Trail", "Ariya", "Leaf", "Navara", "Townstar" },
            ["VOLVO"] = new[] { "EX30", "EX40", "EC40", "EX90", "XC40", "XC60", "XC90", "S60", "S90", "V60", "V90", "C40" },
            ["TESLA"] = new[] { "Model 3", "Model Y", "Model S", "Model X", "Cybertruck" },
            ["TOGG"] = new[] { "T10X", "T10F" },
            ["BYD"] = new[] { "Atto 2", "Atto 3", "Dolphin", "Dolphin Surf", "Seal", "Seal U", "Seal U DM-i", "Sealion 7", "Han", "Tang" },
            ["CHERY"] = new[] { "Tiggo 4", "Tiggo 7", "Tiggo 8", "Omoda 5", "Omoda 5 EV" },
            ["MG"] = new[] { "MG3", "MG4", "MG5", "MG ZS", "MG ZS EV", "MG HS", "MG EHS", "MG7", "Cyberster" },
            ["DACIA"] = new[] { "Sandero", "Sandero Stepway", "Logan", "Duster", "Jogger", "Spring", "Bigster" },
            ["JEEP"] = new[] { "Renegade", "Compass", "Avenger", "Cherokee", "Grand Cherokee", "Wrangler", "Gladiator" },
            ["CUPRA"] = new[] { "Leon", "Formentor", "Ateca", "Born", "Tavascan", "Terramar" },
            ["SEAT"] = new[] { "Ibiza", "Leon", "Arona", "Ateca", "Tarraco" },
            ["SUZUKI"] = new[] { "Swift", "Swift Sport", "Ignis", "Baleno", "Vitara", "S-Cross", "Jimny", "Across" },
            ["MITSUBISHI"] = new[] { "Colt", "ASX", "Eclipse Cross", "Outlander", "L200", "Space Star" },
            ["SUBARU"] = new[] { "Impreza", "XV", "Crosstrek", "Forester", "Outback", "Solterra", "WRX", "BRZ" },
            ["MAZDA"] = new[] { "Mazda 2", "Mazda 3", "Mazda 6", "CX-3", "CX-30", "CX-5", "CX-60", "CX-80", "MX-5", "MX-30" },
            ["LEXUS"] = new[] { "LBX", "UX", "UX 300e", "NX", "RX", "RZ", "ES", "LS", "LC", "LM" },
            ["ALFA ROMEO"] = new[] { "Giulietta", "Giulia", "Stelvio", "Tonale", "Junior", "4C" },
            ["PORSCHE"] = new[] { "911", "718 Cayman", "718 Boxster", "Taycan", "Panamera", "Macan", "Cayenne" },
            ["JAGUAR"] = new[] { "XE", "XF", "XJ", "F-Pace", "E-Pace", "I-Pace", "F-Type" },
            ["LAND ROVER"] = new[] { "Defender", "Discovery", "Discovery Sport", "Range Rover", "Range Rover Sport", "Range Rover Velar", "Range Rover Evoque" },
            ["MASERATI"] = new[] { "Ghibli", "Quattroporte", "Levante", "Grecale", "GranTurismo", "GranCabrio", "MC20" },
            ["MINI"] = new[] { "Cooper", "Cooper 3 Door", "Cooper 5 Door", "Countryman", "Clubman", "Paceman", "Aceman" },
            ["FERRARI"] = new[] { "Roma", "Roma Spider", "296 GTB", "296 GTS", "SF90 Stradale", "SF90 Spider", "12Cilindri", "Purosangue", "812 Superfast", "F8 Tributo" },
            ["LAMBORGHINI"] = new[] { "Huracan", "Revuelto", "Urus", "Aventador", "Gallardo" },
            ["ASTON MARTIN"] = new[] { "Vantage", "DB12", "DBS", "DBX", "Vanquish" },
            ["BENTLEY"] = new[] { "Continental GT", "Continental GTC", "Flying Spur", "Bentayga" }
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