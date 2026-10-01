import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { Upload, X, ShieldCheck, Home, Car } from 'lucide-react';

const PART_NAMES_TR: Record<string, string> = {
  hood: 'Kaput',
  roof: 'Tavan',
  trunkLid: 'Bagaj Kapağı',
  frontBumper: 'Ön Tampon',
  rearBumper: 'Arka Tampon',
  frontLeftFender: 'Sol Ön Çamurluk',
  frontRightFender: 'Sağ Ön Çamurluk',
  rearLeftFender: 'Sol Arka Çamurluk',
  rearRightFender: 'Sağ Arka Çamurluk',
  frontLeftDoor: 'Sol Ön Kapı',
  frontRightDoor: 'Sağ Ön Kapı',
  rearLeftDoor: 'Sol Arka Kapı',
  rearRightDoor: 'Sağ Arka Kapı',
};

const REAL_ESTATE_HIERARCHY: Record<string, string[]> = {
  'Konut (Satılık)': ['Daire', 'Villa', 'Müstakil Ev', 'Rezidans'],
  'Konut (Kiralık)': ['Daire', 'Villa', 'Rezidans'],
  'İş Yeri': ['Dükkan & Mağaza', 'Ofis & Büro', 'Depo & Antrepo'],
  'Arsa': ['İmarlı - Konut', 'İmarlı - Ticari', 'Tarla']
};

interface DropdownItem {
  id: string;
  name: string;
}

export const IlanVer: React.FC = () => {
  const navigate = useNavigate();

  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const isCorporate =
    user?.userType === 'Corporate' ||
    user?.userType === 1 ||
    user?.userType === '1' ||
    !!user?.storeName;

  const maxPhotos = isCorporate ? 20 : 10;

  const [step, setStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [dbCategoryId, setDbCategoryId] = useState<string>('');

  // Tür: 'vehicle' | 'realestate'
  const [mainType, setMainType] = useState<'vehicle' | 'realestate'>('vehicle');

  // Vasıta Kategori State'leri (Dinamik API)
  const [vehicleTypes, setVehicleTypes] = useState<DropdownItem[]>([]);
  const [brands, setBrands] = useState<DropdownItem[]>([]);
  const [seriesList, setSeriesList] = useState<DropdownItem[]>([]);
  const [packages, setPackages] = useState<DropdownItem[]>([]);

  const [selectedVehicleTypeId, setSelectedVehicleTypeId] = useState<string>('');
  const [selectedBrandId, setSelectedBrandId] = useState<string>('');
  const [selectedSeriesId, setSelectedSeriesId] = useState<string>('');
  const [selectedPackageId, setSelectedPackageId] = useState<string>('');

  // Emlak Kategori
  const [realEstateSub, setRealEstateSub] = useState('Konut (Satılık)');
  const [realEstateType, setRealEstateType] = useState('Daire');

  // Lokasyon State'leri (Dinamik API)
  const [cities, setCities] = useState<any[]>([]);
  const [districts, setDistricts] = useState<any[]>([]);
  const [neighborhoods, setNeighborhoods] = useState<any[]>([]);

  const [selectedCityId, setSelectedCityId] = useState<string>('');
  const [selectedCityName, setSelectedCityName] = useState<string>('');
  const [selectedDistrictId, setSelectedDistrictId] = useState<string>('');
  const [selectedDistrictName, setSelectedDistrictName] = useState<string>('');
  const [selectedNeighborhoodName, setSelectedNeighborhoodName] = useState<string>('');

  // Vasıta Özellikleri
  const [year, setYear] = useState('2020');
  const [kilometer, setKilometer] = useState('');
  const [fuelType, setFuelType] = useState('Benzin');
  const [transmission, setTransmission] = useState('Otomatik');
  const [bodyType, setBodyType] = useState('Sedan');
  const [color, setColor] = useState('Beyaz');
  const [enginePower, setEnginePower] = useState('150');
  const [engineCapacity, setEngineCapacity] = useState('1600');
  const [heavyDamage, setHeavyDamage] = useState(false);
  const [damageReport, setDamageReport] = useState<Record<string, number>>({
    hood: 0, roof: 0, trunkLid: 0, frontBumper: 0, rearBumper: 0,
    frontLeftFender: 0, frontRightFender: 0, rearLeftFender: 0, rearRightFender: 0,
    frontLeftDoor: 0, frontRightDoor: 0, rearLeftDoor: 0, rearRightDoor: 0
  });

  // Emlak Özellikleri
  const [grossM2, setGrossM2] = useState('');
  const [netM2, setNetM2] = useState('');
  const [roomCount, setRoomCount] = useState('3+1');
  const [buildingAge, setBuildingAge] = useState('5');
  const [floorLocation, setFloorLocation] = useState('3');
  const [totalFloors, setTotalFloors] = useState('5');
  const [heatingType, setHeatingType] = useState('Kombi (Doğalgaz)');
  const [bathroomCount, setBathroomCount] = useState('1');
  const [hasBalcony, setHasBalcony] = useState(true);
  const [isFurnished, setIsFurnished] = useState(false);
  const [inSite, setInSite] = useState(false);

  // Genel İlan Bilgileri
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [images, setImages] = useState<string[]>([]);
  const [uploadingImage, setUploadingImage] = useState(false);

  // 1. Sayfa Açıldığında 81 İli ve Kategori Ağacını Çek
  useEffect(() => {
    // 81 İli Çek
    api.get('/locations/cities').then((res) => {
      const cityData = res.data?.data || res.data || [];
      if (Array.isArray(cityData) && cityData.length > 0) {
        setCities(cityData);
        const defCity = cityData.find((c: any) => c.name === 'Ankara') || cityData[0];
        setSelectedCityId(defCity.id);
        setSelectedCityName(defCity.name);
        loadDistricts(defCity.id);
      }
    });

    // Kök kategorileri (Vasıta) alıp altındaki Otomobil/SUV/Ticari türlerini getir
    api.get('/categories/roots').then((res) => {
      const rootList = res.data?.data || res.data || [];
      const vasita = rootList.find((c: any) => c.slug === 'vasita') || rootList[0];
      if (vasita) {
        api.get(`/categories/${vasita.id}/subcategories`).then((subRes) => {
          const vTypes = subRes.data?.data || subRes.data || [];
          setVehicleTypes(vTypes);
          if (vTypes.length > 0) {
            setSelectedVehicleTypeId(vTypes[0].id);
            loadBrands(vTypes[0].id);
          }
        });
      }
    });
  }, []);

  // Araç Türü Değiştiğinde (Otomobil -> 40+ Marka)
  const loadBrands = async (vehicleTypeId: string) => {
    try {
      const res = await api.get(`/categories/${vehicleTypeId}/subcategories`);
      const bList = res.data?.data || res.data || [];
      setBrands(bList);
      if (bList.length > 0) {
        setSelectedBrandId(bList[0].id);
        loadSeries(bList[0].id);
      } else {
        setSeriesList([]);
        setPackages([]);
      }
    } catch {}
  };

  // Marka Değiştiğinde (Örn: Renault, BMW, Mercedes -> Modeller)
  const loadSeries = async (brandId: string) => {
    try {
      const res = await api.get(`/categories/${brandId}/subcategories`);
      const sList = res.data?.data || res.data || [];
      setSeriesList(sList);
      if (sList.length > 0) {
        setSelectedSeriesId(sList[0].id);
        loadPackages(sList[0].id);
      } else {
        setPackages([]);
      }
    } catch {}
  };

  // Model Değiştiğinde -> Paket & Motor Seçenekleri
  const loadPackages = async (seriesId: string) => {
    try {
      const res = await api.get(`/categories/${seriesId}/subcategories`);
      const pList = res.data?.data || res.data || [];
      setPackages(pList);
      if (pList.length > 0) {
        setSelectedPackageId(pList[0].id);
        setDbCategoryId(pList[0].id);
      } else {
        setDbCategoryId(seriesId);
      }
    } catch {}
  };

  // İl Değiştiğinde -> Tüm İlçeler
  const loadDistricts = async (cityId: string) => {
    try {
      const res = await api.get(`/locations/districts/${cityId}`);
      const dList = res.data?.data || res.data || [];
      setDistricts(dList);
      if (dList.length > 0) {
        setSelectedDistrictId(dList[0].id);
        setSelectedDistrictName(dList[0].name);
        loadNeighborhoods(dList[0].id);
      } else {
        setNeighborhoods([]);
      }
    } catch {}
  };

  // İlçe Değiştiğinde -> Mahalleler
  const loadNeighborhoods = async (districtId: string) => {
    try {
      const res = await api.get(`/locations/neighborhoods/${districtId}`);
      const nList = res.data?.data || res.data || [];
      setNeighborhoods(nList);
      if (nList.length > 0) {
        setSelectedNeighborhoodName(nList[0].name);
      }
    } catch {}
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || e.target.files.length === 0) return;
    if (images.length + e.target.files.length > maxPhotos) {
      alert(`Hesap limitiniz gereği en fazla ${maxPhotos} adet fotoğraf yükleyebilirsiniz.`);
      return;
    }

    setUploadingImage(true);
    const formData = new FormData();
    Array.from(e.target.files).forEach((file) => formData.append('files', file));

    try {
      const res = await api.post('/Images/upload-multiple', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      if (res.data?.success && Array.isArray(res.data.data)) {
        setImages((prev) => [...prev, ...res.data.data]);
      }
    } catch {
      alert('Fotoğraflar yüklenemedi.');
    } finally {
      setUploadingImage(false);
      e.target.value = '';
    }
  };

  const handleSubmit = async () => {
    if (!title.trim() || !price || !selectedCityName) {
      alert('Lütfen başlık, fiyat ve şehir alanlarını doldurunuz.');
      return;
    }

    setLoading(true);
    try {
      const cleanImages = (images || []).map((img, idx) => ({
        imageUrl: img,
        isMain: idx === 0,
        displayOrder: idx + 1
      }));

      const categoryIdToSend = dbCategoryId || selectedPackageId || selectedSeriesId || '11111111-1111-1111-1111-111111111111';

      const payload: any = {
        categoryId: categoryIdToSend,
        title: title.trim(),
        description: description.trim() || 'Açıklama belirtilmedi.',
        price: Number(price),
        currency: 0,
        city: selectedCityName,
        district: selectedDistrictName || 'Merkez',
        neighborhood: selectedNeighborhoodName || 'Merkez Mah.',
        images: cleanImages
      };

      if (mainType === 'vehicle') {
        payload.vehicleDetail = {
          year: Number(year),
          kilometer: Number(kilometer) || 0,
          fuelType,
          transmission,
          bodyType,
          color,
          enginePowerHp: Number(enginePower) || 150,
          engineCapacityCc: Number(engineCapacity) || 1600,
          heavyDamageRegistered: heavyDamage
        };
        payload.damageReport = damageReport;
      } else {
        payload.realEstateDetail = {
          grossSquareMeters: Number(grossM2) || 100,
          netSquareMeters: Number(netM2) || 85,
          roomCount,
          buildingAge: Number(buildingAge) || 0,
          floorLocation: Number(floorLocation) || 1,
          totalFloors: Number(totalFloors) || 5,
          heatingType,
          bathroomCount: Number(bathroomCount) || 1,
          hasBalcony,
          isFurnished,
          inSite
        };
      }

      const res = await api.post('/Listings', payload);
      if (res.data?.success) {
        alert('İlanınız başarıyla yayına alındı!');
        navigate('/bana-ozel/ilanlarim');
      } else {
        alert(res.data?.message || 'İlan kaydedilemedi.');
      }
    } catch (err: any) {
      console.error('İlan hata detayı:', err.response?.data);
      alert('İlan oluşturma hatası: ' + (err.response?.data?.message || err.message));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-[1000px] mx-auto px-4 py-8 text-xs">
      <div className="flex justify-between items-center bg-white p-4 border rounded mb-6">
        <div className={`flex items-center gap-2 font-bold ${step === 1 ? 'text-[#0055b8]' : 'text-gray-400'}`}>
          <span className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center">1</span>
          Kategori & Konum Seçimi
        </div>
        <div className={`flex items-center gap-2 font-bold ${step === 2 ? 'text-[#0055b8]' : 'text-gray-400'}`}>
          <span className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center">2</span>
          {mainType === 'vehicle' ? 'Araç Özellikleri & Ekspertiz' : 'Emlak Özellikleri'}
        </div>
        <div className={`flex items-center gap-2 font-bold ${step === 3 ? 'text-[#0055b8]' : 'text-gray-400'}`}>
          <span className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center">3</span>
          Fiyat & Fotoğraflar
        </div>
      </div>

      <div className="bg-white border rounded p-6 shadow-2xs">
        {step === 1 && (
          <div className="space-y-5">
            <h2 className="font-bold text-sm text-gray-900 border-b pb-2">1. Adım: Ne İlanı Vermek İstiyorsunuz?</h2>

            <div className="grid grid-cols-2 gap-4">
              <button
                type="button"
                onClick={() => setMainType('vehicle')}
                className={`p-4 rounded border flex items-center justify-center gap-3 font-bold transition text-sm ${
                  mainType === 'vehicle' ? 'border-[#0055b8] bg-blue-50 text-[#0055b8]' : 'border-gray-200 text-gray-700'
                }`}
              >
                <Car className="w-6 h-6" />
                <span>Vasıta (Otomobil, SUV, Ticari)</span>
              </button>

              <button
                type="button"
                onClick={() => setMainType('realestate')}
                className={`p-4 rounded border flex items-center justify-center gap-3 font-bold transition text-sm ${
                  mainType === 'realestate' ? 'border-[#0055b8] bg-blue-50 text-[#0055b8]' : 'border-gray-200 text-gray-700'
                }`}
              >
                <Home className="w-6 h-6" />
                <span>Emlak (Konut, İş Yeri, Arsa)</span>
              </button>
            </div>

            {mainType === 'vehicle' ? (
              <div className="pt-3 border-t space-y-3">
                <div className="grid grid-cols-4 gap-3">
                  {/* Araç Türü */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Araç Türü</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={selectedVehicleTypeId}
                      onChange={(e) => {
                        setSelectedVehicleTypeId(e.target.value);
                        loadBrands(e.target.value);
                      }}
                    >
                      {vehicleTypes.map((t) => (
                        <option key={t.id} value={t.id}>{t.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Marka (Veritabanındaki 40+ Marka) */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Marka</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={selectedBrandId}
                      onChange={(e) => {
                        setSelectedBrandId(e.target.value);
                        loadSeries(e.target.value);
                      }}
                    >
                      {brands.map((b) => (
                        <option key={b.id} value={b.id}>{b.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Seri */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Seri</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={selectedSeriesId}
                      onChange={(e) => {
                        setSelectedSeriesId(e.target.value);
                        loadPackages(e.target.value);
                      }}
                    >
                      {seriesList.map((s) => (
                        <option key={s.id} value={s.id}>{s.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Paket & Motor */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Paket & Motor</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={selectedPackageId}
                      onChange={(e) => {
                        setSelectedPackageId(e.target.value);
                        setDbCategoryId(e.target.value);
                      }}
                    >
                      {packages.map((p) => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            ) : (
              <div className="pt-3 border-t grid grid-cols-2 gap-3">
                <div>
                  <label className="block font-bold text-gray-700 mb-1">Emlak Türü</label>
                  <select
                    className="w-full border p-2 rounded bg-white"
                    value={realEstateSub}
                    onChange={(e) => {
                      setRealEstateSub(e.target.value);
                      setRealEstateType(REAL_ESTATE_HIERARCHY[e.target.value]?.[0] || '');
                    }}
                  >
                    {Object.keys(REAL_ESTATE_HIERARCHY).map((re) => (
                      <option key={re} value={re}>{re}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block font-bold text-gray-700 mb-1">Alt Kategori</label>
                  <select
                    className="w-full border p-2 rounded bg-white"
                    value={realEstateType}
                    onChange={(e) => setRealEstateType(e.target.value)}
                  >
                    {(REAL_ESTATE_HIERARCHY[realEstateSub] || []).map((t) => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                </div>
              </div>
            )}

            {/* Lokasyon: İl - İlçe - Mahalle */}
            <div className="pt-4 border-t grid grid-cols-3 gap-3">
              <div>
                <label className="block font-bold text-gray-700 mb-1">İl (81 İl)</label>
                <select
                  className="w-full border p-2 rounded bg-white"
                  value={selectedCityId}
                  onChange={(e) => {
                    const cId = e.target.value;
                    setSelectedCityId(cId);
                    const cObj = cities.find((c) => c.id === cId);
                    setSelectedCityName(cObj?.name || '');
                    loadDistricts(cId);
                  }}
                >
                  {cities.map((c) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block font-bold text-gray-700 mb-1">İlçe</label>
                <select
                  className="w-full border p-2 rounded bg-white"
                  value={selectedDistrictId}
                  onChange={(e) => {
                    const dId = e.target.value;
                    setSelectedDistrictId(dId);
                    const dObj = districts.find((d) => d.id === dId);
                    setSelectedDistrictName(dObj?.name || '');
                    loadNeighborhoods(dId);
                  }}
                >
                  {districts.map((d) => (
                    <option key={d.id} value={d.id}>{d.name}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block font-bold text-gray-700 mb-1">Mahalle</label>
                <select
                  className="w-full border p-2 rounded bg-white"
                  value={selectedNeighborhoodName}
                  onChange={(e) => setSelectedNeighborhoodName(e.target.value)}
                >
                  {neighborhoods.map((n) => (
                    <option key={n.id} value={n.name}>{n.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex justify-end pt-4 border-t">
              <button
                type="button"
                onClick={() => setStep(2)}
                className="bg-[#0055b8] hover:bg-[#004494] text-white font-bold px-6 py-2 rounded"
              >
                İleri: Özellikler &gt;
              </button>
            </div>
          </div>
        )}

        {step === 2 && (
          <div className="space-y-4">
            {mainType === 'vehicle' ? (
              <>
                <h2 className="font-bold text-sm text-gray-900 border-b pb-2">2. Adım: Araç Bilgileri & Ekspertiz</h2>
                <div className="grid grid-cols-3 gap-3">
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Model Yılı</label>
                    <input type="number" className="w-full border p-2 rounded" value={year} onChange={(e) => setYear(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Kilometre</label>
                    <input type="number" placeholder="Örn: 95000" className="w-full border p-2 rounded" value={kilometer} onChange={(e) => setKilometer(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Yakıt Türü</label>
                    <select className="w-full border p-2 rounded bg-white" value={fuelType} onChange={(e) => setFuelType(e.target.value)}>
                      <option value="Benzin">Benzin</option>
                      <option value="Dizel">Dizel</option>
                      <option value="LPG & Benzin">LPG & Benzin</option>
                      <option value="Hibrit">Hibrit</option>
                      <option value="Elektrik">Elektrik</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Vites</label>
                    <select className="w-full border p-2 rounded bg-white" value={transmission} onChange={(e) => setTransmission(e.target.value)}>
                      <option value="Otomatik">Otomatik</option>
                      <option value="Manuel">Manuel</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Renk</label>
                    <input type="text" className="w-full border p-2 rounded" value={color} onChange={(e) => setColor(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Motor Gücü (HP)</label>
                    <input type="number" className="w-full border p-2 rounded" value={enginePower} onChange={(e) => setEnginePower(e.target.value)} />
                  </div>
                </div>

                <div className="pt-2">
                  <label className="flex items-center gap-2 cursor-pointer font-bold text-red-600">
                    <input type="checkbox" checked={heavyDamage} onChange={(e) => setHeavyDamage(e.target.checked)} />
                    Ağır Hasar Kayıtlı (Pert)
                  </label>
                </div>

                <div className="pt-3 border-t">
                  <label className="block font-bold text-gray-800 mb-2 flex items-center gap-1">
                    <ShieldCheck className="w-4 h-4 text-emerald-600" /> Boya & Değişen Durumu (Ekspertiz)
                  </label>
                  <div className="grid grid-cols-3 gap-2">
                    {Object.entries(PART_NAMES_TR).map(([key, label]) => (
                      <div key={key} className="flex justify-between items-center border p-2 rounded bg-gray-50">
                        <span className="font-semibold text-gray-700">{label}</span>
                        <select
                          className="border rounded text-[10px] p-1 bg-white font-bold"
                          value={damageReport[key] ?? 0}
                          onChange={(e) => setDamageReport({ ...damageReport, [key]: Number(e.target.value) })}
                        >
                          <option value={0}>Orijinal</option>
                          <option value={1}>Boyalı</option>
                          <option value={2}>Değişen</option>
                        </select>
                      </div>
                    ))}
                  </div>
                </div>
              </>
            ) : (
              <>
                <h2 className="font-bold text-sm text-gray-900 border-b pb-2">2. Adım: Emlak & Konut Özellikleri</h2>
                <div className="grid grid-cols-3 gap-3">
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Oda Sayısı</label>
                    <select className="w-full border p-2 rounded bg-white" value={roomCount} onChange={(e) => setRoomCount(e.target.value)}>
                      <option value="1+0">1+0 (Stüdyo)</option>
                      <option value="1+1">1+1</option>
                      <option value="2+1">2+1</option>
                      <option value="3+1">3+1</option>
                      <option value="4+1">4+1</option>
                      <option value="5+1">5+1 ve üzeri</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Brüt m²</label>
                    <input type="number" placeholder="Örn: 125" className="w-full border p-2 rounded" value={grossM2} onChange={(e) => setGrossM2(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Net m²</label>
                    <input type="number" placeholder="Örn: 110" className="w-full border p-2 rounded" value={netM2} onChange={(e) => setNetM2(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Bina Yaşı</label>
                    <input type="number" placeholder="0: Sıfır Bina" className="w-full border p-2 rounded" value={buildingAge} onChange={(e) => setBuildingAge(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Bulunduğu Kat</label>
                    <input type="number" placeholder="Örn: 3" className="w-full border p-2 rounded" value={floorLocation} onChange={(e) => setFloorLocation(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Kat Sayısı</label>
                    <input type="number" placeholder="Örn: 8" className="w-full border p-2 rounded" value={totalFloors} onChange={(e) => setTotalFloors(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Isıtma Tipi</label>
                    <select className="w-full border p-2 rounded bg-white" value={heatingType} onChange={(e) => setHeatingType(e.target.value)}>
                      <option value="Kombi (Doğalgaz)">Kombi (Doğalgaz)</option>
                      <option value="Merkezi">Merkezi</option>
                      <option value="Yerden Isıtma">Yerden Isıtma</option>
                      <option value="Klima">Klima</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Banyo Sayısı</label>
                    <select className="w-full border p-2 rounded bg-white" value={bathroomCount} onChange={(e) => setBathroomCount(e.target.value)}>
                      <option value="1">1</option>
                      <option value="2">2</option>
                      <option value="3">3+</option>
                    </select>
                  </div>
                </div>

                <div className="pt-3 flex gap-6">
                  <label className="flex items-center gap-2 cursor-pointer font-bold text-gray-700">
                    <input type="checkbox" checked={hasBalcony} onChange={(e) => setHasBalcony(e.target.checked)} />
                    Balkon Var
                  </label>
                  <label className="flex items-center gap-2 cursor-pointer font-bold text-gray-700">
                    <input type="checkbox" checked={isFurnished} onChange={(e) => setIsFurnished(e.target.checked)} />
                    Eşyalı
                  </label>
                  <label className="flex items-center gap-2 cursor-pointer font-bold text-gray-700">
                    <input type="checkbox" checked={inSite} onChange={(e) => setInSite(e.target.checked)} />
                    Site İçerisinde
                  </label>
                </div>
              </>
            )}

            <div className="flex justify-between pt-4 border-t">
              <button type="button" onClick={() => setStep(1)} className="bg-gray-100 px-6 py-2 rounded font-bold">
                &lt; Geri
              </button>
              <button type="button" onClick={() => setStep(3)} className="bg-[#0055b8] text-white px-6 py-2 rounded font-bold">
                İleri: Fiyat & Fotoğraf &gt;
              </button>
            </div>
          </div>
        )}

        {step === 3 && (
          <div className="space-y-4">
            <h2 className="font-bold text-sm text-gray-900 border-b pb-2">3. Adım: İlan Başlığı, Fiyat ve Fotoğraflar</h2>

            <div>
              <label className="block font-bold text-gray-700 mb-1">İlan Başlığı</label>
              <input
                type="text"
                placeholder={mainType === 'vehicle' ? 'Örn: Sahibinden Bakımlı Duster 4x4' : 'Örn: Metroya Yakın Masrafsız 3+1 Daire'}
                className="w-full border p-2 rounded"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>

            <div>
              <label className="block font-bold text-gray-700 mb-1">Fiyat (TL)</label>
              <input
                type="number"
                placeholder="Örn: 950000"
                className="w-full border p-2 rounded font-bold text-red-600"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
              />
            </div>

            <div>
              <label className="block font-bold text-gray-700 mb-1">Açıklama</label>
              <textarea
                rows={3}
                placeholder="Detaylı bilgi yazınız..."
                className="w-full border p-2 rounded"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <label className="font-bold text-gray-700">Fotoğraflar ({images.length} / {maxPhotos})</label>
                <span className={`text-[10px] font-bold px-2 py-0.5 rounded border ${
                  isCorporate ? 'bg-amber-50 text-amber-900 border-amber-300' : 'bg-gray-100 text-gray-600 border-gray-200'
                }`}>
                  {isCorporate ? '🏢 Kurumsal Galeri (Maks 20 Adet)' : '👤 Standart Hesap (Maks 10 Adet)'}
                </span>
              </div>

              <div className="flex flex-wrap gap-2 items-center">
                {images.map((img, idx) => (
                  <div key={idx} className="relative w-20 h-16 border rounded overflow-hidden">
                    <img src={img} alt="" className="w-full h-full object-cover" />
                    <button
                      type="button"
                      onClick={() => setImages(images.filter((_, i) => i !== idx))}
                      className="absolute top-1 right-1 bg-red-600 text-white rounded-full p-0.5"
                    >
                      <X className="w-2.5 h-2.5" />
                    </button>
                  </div>
                ))}

                {images.length < maxPhotos && (
                  <label className="w-20 h-16 border-2 border-dashed rounded flex flex-col items-center justify-center cursor-pointer hover:border-blue-600 text-gray-400">
                    <Upload className="w-4 h-4" />
                    <span className="text-[9px] font-bold">{uploadingImage ? '...' : '+ Fotoğraf'}</span>
                    <input type="file" multiple accept="image/*" className="hidden" onChange={handleFileUpload} />
                  </label>
                )}
              </div>
            </div>

            <div className="flex justify-between pt-4 border-t">
              <button type="button" onClick={() => setStep(2)} className="bg-gray-100 px-6 py-2 rounded font-bold">
                &lt; Geri
              </button>
              <button
                type="button"
                disabled={loading}
                onClick={handleSubmit}
                className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold px-8 py-2 rounded"
              >
                {loading ? 'Yayınlanıyor...' : 'İlanı Yayına Al'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};