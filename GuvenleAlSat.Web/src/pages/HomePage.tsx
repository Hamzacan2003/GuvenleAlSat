import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Link, useSearchParams } from 'react-router-dom';
import { Filter, X, ChevronDown, ChevronUp } from 'lucide-react';
import { getFullImageUrl } from '../services/api';

const getCleanImageUrl = (url?: string | null): string => {
  if (!url || typeof url !== 'string' || url.trim() === '') {
    return 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=400&q=80';
  }
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }
  const cleanPath = url.startsWith('/') ? url : `/${url}`;
  return `http://localhost:5121${cleanPath}`;
};

export const HomePage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [listings, setListings] = useState<any[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(false);

  // Mobil Filtre Çekmecesi State'i
  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState<boolean>(false);

  // Kategori Yönetimi
  const [categories, setCategories] = useState<any[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  const [selectedCategoryName, setSelectedCategoryName] = useState<string>('Vasıta');

  // Ortak Filtreler
  const [cities, setCities] = useState<any[]>([]);
  const [selectedCity, setSelectedCity] = useState(searchParams.get('city') || '');
  const [minPrice, setMinPrice] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [sortBy, setSortBy] = useState('date_desc');

  // Vasıta Özel Filtreleri
  const [minYear, setMinYear] = useState('');
  const [maxYear, setMaxYear] = useState('');
  const [selectedFuels, setSelectedFuels] = useState<string[]>([]);
  const [selectedTransmissions, setSelectedTransmissions] = useState<string[]>([]);
  const [onlyUndamaged, setOnlyUndamaged] = useState(false);

  // Emlak Özel Filtreleri
  const [selectedRooms, setSelectedRooms] = useState<string[]>([]);
  const [minM2, setMinM2] = useState('');
  const [maxM2, setMaxM2] = useState('');
  const [maxBuildingAge, setMaxBuildingAge] = useState('');

  const isRealEstate = selectedCategoryName.toLowerCase().includes('emlak');

  useEffect(() => {
    api.get('/locations/cities')
      .then((res) => {
        if (res?.data?.success && Array.isArray(res.data.data)) {
          setCities(res.data.data);
        }
      })
      .catch(() => {});

    api.get('/categories/roots')
      .then((res) => {
        if (res?.data?.success && Array.isArray(res.data.data)) {
          setCategories(res.data.data);
        }
      })
      .catch(() => {});
  }, []);

  const fetchListings = async () => {
    setLoading(true);
    try {
      const payload: any = {
        categoryId: selectedCategoryId,
        city: selectedCity || null,
        minPrice: minPrice ? Number(minPrice) : null,
        maxPrice: maxPrice ? Number(maxPrice) : null,
        searchQuery: searchParams.get('q') || null,
        page: 1,
        pageSize: 30,
        sortBy: sortBy,
      };

      if (!isRealEstate) {
        payload.minYear = minYear ? Number(minYear) : null;
        payload.maxYear = maxYear ? Number(maxYear) : null;
        payload.fuelTypes = selectedFuels.length ? selectedFuels : null;
        payload.transmissions = selectedTransmissions.length ? selectedTransmissions : null;
        payload.excludeHeavyDamage = onlyUndamaged;
      }

      const res = await api.post('/Listings/search', payload);
      if (res?.data?.success && Array.isArray(res.data.data?.items)) {
        setListings(res.data.data.items);
        setTotalCount(res.data.data.totalCount ?? res.data.data.items.length);
      } else {
        setListings([]);
        setTotalCount(0);
      }
    } catch (err) {
      console.error('İlanlar çekilirken hata oluştu:', err);
      setListings([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchListings();
  }, [
    selectedCity,
    selectedCategoryId,
    selectedFuels,
    selectedTransmissions,
    selectedRooms,
    onlyUndamaged,
    searchParams,
    sortBy,
  ]);

  const handleCategorySelect = (catName: string) => {
    setSelectedCategoryName(catName);
    const found = categories.find((c) => c.name.toLowerCase().includes(catName.toLowerCase()));
    if (found) {
      setSelectedCategoryId(found.id);
    } else {
      setSelectedCategoryId(null);
    }
  };

  const handleCategoryClick = async (cat: any) => {
    setSelectedCategoryId(cat.id);
    setSelectedCategoryName(cat.name);
    if (!cat.isLeaf) {
      try {
        const subRes = await api.get(`/categories/${cat.id}/subcategories`);
        if (subRes?.data?.success && Array.isArray(subRes.data.data) && subRes.data.data.length > 0) {
          setCategories(subRes.data.data);
        }
      } catch {}
    }
  };

  const resetFilters = () => {
    setSelectedCategoryId(null);
    setSelectedCategoryName('Vasıta');
    setSelectedCity('');
    setMinPrice('');
    setMaxPrice('');
    setMinYear('');
    setMaxYear('');
    setSelectedFuels([]);
    setSelectedTransmissions([]);
    setOnlyUndamaged(false);
    setSelectedRooms([]);
    setMinM2('');
    setMaxM2('');
    setMaxBuildingAge('');
    api.get('/categories/roots').then((res) => {
      if (res?.data?.success && Array.isArray(res.data.data)) {
        setCategories(res.data.data);
      }
    });
  };
// Kullanım:
<img 
  src={getFullImageUrl(listing.mainImageUrl || listing.imageUrl)} 
  alt={listing.title}
  className="w-full h-full object-cover"
  onError={(e: any) => {
    e.target.src = 'https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800&auto=format&fit=crop&q=60';
  }}
/>
  const toggleArray = (val: string, list: string[], setList: (v: string[]) => void) => {
    if (list.includes(val)) setList(list.filter((x) => x !== val));
    else setList([...list, val]);
  };

  return (
    <div className="w-full max-w-[1200px] mx-auto px-2 sm:px-4 py-3">
      {/* ÜST BİLGİ & MOBİL FİLTRE BUTONU */}
      <div className="flex flex-row justify-between items-center mb-3">
        <div className="text-[11px] text-[#0055b8] flex items-center gap-1">
          <Link to="/" onClick={resetFilters} className="hover:underline">
            Anasayfa
          </Link>
          <span>&gt;</span>
          <span className="text-gray-600 font-bold">{isRealEstate ? 'Emlak Vitrini' : 'Vasıta Vitrini'}</span>
        </div>

        {/* Yalnızca Mobilde ve Tablette Görünen Filtre Açma Butonu */}
        <button
          onClick={() => setIsMobileFilterOpen(!isMobileFilterOpen)}
          className="md:hidden flex items-center gap-1.5 bg-[#ffe100] text-gray-900 px-3 py-1.5 rounded font-bold text-xs shadow-sm border border-amber-300"
        >
          <Filter className="w-3.5 h-3.5" />
          <span>Filtreler</span>
          {isMobileFilterOpen ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
        </button>
      </div>

      <div className="flex flex-col md:grid md:grid-cols-12 gap-4">
        {/* SOL MENÜ - FİLTRE PANELİ (Masaüstünde Sabit, Mobilde Açılır-Kapanır) */}
        <aside
          className={`w-full md:col-span-4 lg:col-span-3 space-y-3 ${
            isMobileFilterOpen ? 'block' : 'hidden md:block'
          }`}
        >
          <div className="bg-white border border-[#d9d9d9] rounded p-3 text-[11px] space-y-3 shadow-sm">
            {/* Kategori Seçimi */}
            <div>
              <div className="font-bold text-[#0055b8] pb-1 border-b mb-2 flex justify-between items-center">
                <span>Kategoriler</span>
                <button onClick={resetFilters} className="text-red-500 text-[10px] hover:underline">
                  Sıfırla
                </button>
              </div>

              {categories.length > 0 ? (
                <div className="space-y-1 max-h-48 overflow-y-auto pr-1">
                  {categories.map((c) => (
                    <div
                      key={c.id}
                      onClick={() => handleCategoryClick(c)}
                      className={`p-1.5 rounded cursor-pointer flex justify-between items-center transition ${
                        selectedCategoryId === c.id || selectedCategoryName === c.name
                          ? 'bg-[#ffe100] text-black font-bold'
                          : 'text-[#0055b8] hover:bg-gray-100'
                      }`}
                    >
                      <span className="truncate">{c.name}</span>
                      {!c.isLeaf && <span className="text-gray-400">&gt;</span>}
                    </div>
                  ))}
                </div>
              ) : (
                <div className="space-y-1">
                  <button
                    onClick={() => handleCategorySelect('Vasıta')}
                    className={`w-full text-left px-2 py-1.5 rounded flex justify-between ${
                      !isRealEstate ? 'bg-[#ffe100] font-bold text-black' : 'text-[#0055b8]'
                    }`}
                  >
                    <span>Vasıta</span> &gt;
                  </button>
                  <button
                    onClick={() => handleCategorySelect('Emlak')}
                    className={`w-full text-left px-2 py-1.5 rounded flex justify-between ${
                      isRealEstate ? 'bg-[#ffe100] font-bold text-black' : 'text-[#0055b8]'
                    }`}
                  >
                    <span>Emlak</span> &gt;
                  </button>
                </div>
              )}
            </div>

            {/* Şehir Filtresi */}
            <div className="pt-2 border-t">
              <label className="block font-bold text-gray-800 mb-1">Adres</label>
              <select
                className="w-full border border-gray-300 p-1.5 rounded text-[11px] bg-white focus:outline-none"
                value={selectedCity}
                onChange={(e) => setSelectedCity(e.target.value)}
              >
                <option value="">Tüm İller</option>
                {cities.map((city) => (
                  <option key={city.id} value={city.name}>
                    {city.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Fiyat Filtresi */}
            <div className="pt-2 border-t">
              <label className="block font-bold text-gray-800 mb-1">Fiyat (TL)</label>
              <div className="grid grid-cols-2 gap-2">
                <input
                  type="number"
                  placeholder="min TL"
                  className="border border-gray-300 p-1.5 rounded text-center text-xs"
                  value={minPrice}
                  onChange={(e) => setMinPrice(e.target.value)}
                />
                <input
                  type="number"
                  placeholder="max TL"
                  className="border border-gray-300 p-1.5 rounded text-center text-xs"
                  value={maxPrice}
                  onChange={(e) => setMaxPrice(e.target.value)}
                />
              </div>
            </div>

            {/* Dinamik Alanlar (Vasıta vs Emlak) */}
            {!isRealEstate ? (
              <>
                <div className="pt-2 border-t">
                  <label className="block font-bold text-gray-800 mb-1">Model Yılı</label>
                  <div className="grid grid-cols-2 gap-2">
                    <input
                      type="number"
                      placeholder="min"
                      className="border border-gray-300 p-1.5 rounded text-center text-xs"
                      value={minYear}
                      onChange={(e) => setMinYear(e.target.value)}
                    />
                    <input
                      type="number"
                      placeholder="max"
                      className="border border-gray-300 p-1.5 rounded text-center text-xs"
                      value={maxYear}
                      onChange={(e) => setMaxYear(e.target.value)}
                    />
                  </div>
                </div>

                <div className="pt-2 border-t">
                  <label className="block font-bold text-gray-800 mb-1">Yakıt Türü</label>
                  <div className="space-y-1">
                    {['Benzin', 'Dizel', 'LPG & Benzin', 'Hibrit', 'Elektrik'].map((f) => (
                      <label key={f} className="flex items-center gap-1.5 text-gray-700 cursor-pointer py-0.5">
                        <input
                          type="checkbox"
                          checked={selectedFuels.includes(f)}
                          onChange={() => toggleArray(f, selectedFuels, setSelectedFuels)}
                        />
                        <span>{f}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div className="pt-2 border-t">
                  <label className="block font-bold text-gray-800 mb-1">Vites</label>
                  <div className="space-y-1">
                    {['Manuel', 'Otomatik'].map((t) => (
                      <label key={t} className="flex items-center gap-1.5 text-gray-700 cursor-pointer py-0.5">
                        <input
                          type="checkbox"
                          checked={selectedTransmissions.includes(t)}
                          onChange={() => toggleArray(t, selectedTransmissions, setSelectedTransmissions)}
                        />
                        <span>{t}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div className="pt-2 border-t">
                  <label className="flex items-center gap-1.5 font-bold text-emerald-800 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={onlyUndamaged}
                      onChange={(e) => setOnlyUndamaged(e.target.checked)}
                    />
                    <span>Hatasız / Boyasız</span>
                  </label>
                </div>
              </>
            ) : (
              <>
                <div className="pt-2 border-t">
                  <label className="block font-bold text-gray-800 mb-1">Oda Sayısı</label>
                  <div className="space-y-1">
                    {['1+1', '2+1', '3+1', '4+1'].map((r) => (
                      <label key={r} className="flex items-center gap-1.5 text-gray-700 cursor-pointer py-0.5">
                        <input
                          type="checkbox"
                          checked={selectedRooms.includes(r)}
                          onChange={() => toggleArray(r, selectedRooms, setSelectedRooms)}
                        />
                        <span>{r}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div className="pt-2 border-t">
                  <label className="block font-bold text-gray-800 mb-1">Metrekare (m²)</label>
                  <div className="grid grid-cols-2 gap-2">
                    <input
                      type="number"
                      placeholder="min m²"
                      className="border border-gray-300 p-1.5 rounded text-center text-xs"
                      value={minM2}
                      onChange={(e) => setMinM2(e.target.value)}
                    />
                    <input
                      type="number"
                      placeholder="max m²"
                      className="border border-gray-300 p-1.5 rounded text-center text-xs"
                      value={maxM2}
                      onChange={(e) => setMaxM2(e.target.value)}
                    />
                  </div>
                </div>

                <div className="pt-2 border-t">
                  <label className="block font-bold text-gray-800 mb-1">Maksimum Bina Yaşı</label>
                  <input
                    type="number"
                    placeholder="Örn: 10"
                    className="w-full border border-gray-300 p-1.5 rounded text-center text-xs"
                    value={maxBuildingAge}
                    onChange={(e) => setMaxBuildingAge(e.target.value)}
                  />
                </div>
              </>
            )}

            <button
              onClick={() => {
                fetchListings();
                setIsMobileFilterOpen(false); // Mobilde tıklandığında paneli kapatıp ilanları göstersin
              }}
              className="w-full bg-[#438ed8] hover:bg-[#357ebd] text-white font-bold py-2 rounded text-sm transition mt-3 shadow-sm"
            >
              Filtrele
            </button>
          </div>
        </aside>

        {/* SAĞ İLAN TABLOSU */}
        <main className="w-full md:col-span-8 lg:col-span-9 space-y-3">
          {/* Sonuç Sayısı ve Sıralama Çubuğu */}
          <div className="bg-white border border-[#d9d9d9] rounded p-2.5 text-xs flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2 shadow-sm">
            <div>
              Toplam <strong className="text-red-600 font-bold">{totalCount}</strong> ilan bulundu.
            </div>
            <div className="flex items-center gap-2 text-[11px] w-full sm:w-auto justify-between sm:justify-end">
              <span className="text-gray-500 font-semibold">Sıralama:</span>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                className="border border-gray-300 rounded p-1.5 bg-white font-bold text-gray-800 focus:outline-none cursor-pointer text-xs"
              >
                <option value="date_desc">Tarihe Göre (En Yeni)</option>
                <option value="price_asc">Fiyata Göre (Önce En Düşük)</option>
                <option value="price_desc">Fiyata Göre (Önce En Yüksek)</option>
                {!isRealEstate && (
                  <>
                    <option value="km_asc">Kilometreye Göre (Önce En Düşük)</option>
                    <option value="km_desc">Kilometreye Göre (Önce En Yüksek)</option>
                    <option value="year_desc">Model Yılına Göre (En Yeni)</option>
                  </>
                )}
              </select>
            </div>
          </div>

          <div className="bg-white border border-[#d9d9d9] rounded overflow-hidden shadow-sm">
            {/* Masaüstü Başlık Satırı (Yalnızca md ve üzeri ekranlarda görünür) */}
            <div className="hidden md:grid grid-cols-12 bg-[#f0f0f0] border-b border-[#d9d9d9] text-[11px] font-bold text-gray-700 py-2 px-3">
              <div className="col-span-8 pl-28">İlan Başlığı</div>
              <div className="col-span-2 text-right">Fiyat ▲</div>
              <div className="col-span-1 text-center">İlan Tarihi</div>
              <div className="col-span-1 text-right">İl / İlçe</div>
            </div>

            {loading ? (
              <div className="text-center py-12 text-gray-500 font-semibold text-xs">
                İlanlar yükleniyor...
              </div>
            ) : listings.length === 0 ? (
              <div className="text-center py-16 text-gray-500 text-xs space-y-2">
                <div className="font-bold text-gray-700 text-sm">Aramanıza uygun aktif ilan bulunamadı.</div>
                <div>İlk ilanı siz vererek satışa çıkarabilirsiniz.</div>
                <Link
                  to="/ilan-ver"
                  className="inline-block mt-2 bg-[#438ed8] hover:bg-[#357ebd] text-white font-bold px-4 py-2 rounded text-xs transition shadow-sm"
                >
                  Ücretsiz İlan Ver
                </Link>
              </div>
            ) : (
              <div className="divide-y divide-[#e9e9e9]">
                {listings.map((item) => (
                  <Link
                    key={item.id}
                    to={`/ilan/${item.listingNo}`}
                    className="flex flex-col sm:grid sm:grid-cols-12 items-start sm:items-center p-2.5 sm:py-2 sm:px-3 text-xs hover:bg-[#f3f7fc] transition bg-white gap-2 sm:gap-0"
                  >
                    {/* Resim ve Başlık Alanı */}
                    <div className="sm:col-span-8 flex items-center gap-3 w-full">
                      <div className="w-[95px] h-[72px] sm:w-[100px] sm:h-[75px] bg-gray-100 rounded overflow-hidden shrink-0 border border-gray-200">
                        <img
                          src={getCleanImageUrl(item.mainImageUrl || item.images?.[0]?.imageUrl)}
                          alt={item.title}
                          className="w-full h-full object-cover"
                          onError={(e) => {
                            (e.target as HTMLImageElement).src =
                              'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=400&q=80';
                          }}
                        />
                      </div>
                      <div className="min-w-0 flex-1">
                        <span className="text-[10px] sm:text-[11px] text-gray-400 block font-mono">#{item.listingNo}</span>
                        <span className="font-bold text-[#8a147e] hover:underline text-xs sm:text-[13px] line-clamp-2 sm:line-clamp-1">
                          {item.title}
                        </span>
                        <div className="text-gray-500 text-[10px] sm:text-[11px] mt-0.5 flex flex-wrap gap-1.5 sm:gap-2">
                          {item.year && <span>{item.year}</span>}
                          {item.kilometer !== null && item.kilometer !== undefined && (
                            <span>• {item.kilometer.toLocaleString()} km</span>
                          )}
                          {item.fuelType && <span>• {item.fuelType}</span>}
                        </div>
                      </div>
                    </div>

                    {/* Fiyat Alanı */}
                    <div className="sm:col-span-2 text-left sm:text-right font-black text-red-700 text-xs sm:text-[13px] w-full sm:w-auto">
                      {item.price ? `${item.price.toLocaleString('tr-TR')} TL` : 'Fiyat Belirtilmemiş'}
                    </div>

                    {/* Tarih Alanı */}
                    <div className="sm:col-span-1 text-left sm:text-center text-[10px] sm:text-[11px] text-gray-600 leading-tight">
                      {item.publishedAt || item.createdAt
                        ? new Date(item.publishedAt || item.createdAt).toLocaleDateString('tr-TR', {
                            day: 'numeric',
                            month: 'short',
                          })
                        : 'Bugün'}
                    </div>

                    {/* İl / İlçe Alanı */}
                    <div className="sm:col-span-1 text-left sm:text-right text-[10px] sm:text-[11px] text-gray-600 leading-tight">
                      {item.city} <span className="text-gray-400 block sm:inline">/ {item.district}</span>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
};