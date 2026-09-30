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

const VEHICLE_HIERARCHY: Record<string, Record<string, Record<string, string[]>>> = {
  Otomobil: {
    Renault: {
      Clio: ['1.0 TCe Touch', '1.0 TCe Joy', '1.5 dCi Touch', '1.3 TCe Icon'],
      Megane: ['1.3 TCe Joy', '1.3 TCe Touch', '1.5 Blue dCi Icon']
    },
    Volkswagen: {
      Golf: ['1.0 eTSI Life', '1.5 eTSI Style', '1.5 eTSI R-Line', '1.6 TDI Comfortline'],
      Passat: ['1.5 TSI Elegance', '1.5 TSI Business', '2.0 TDI Elegance']
    },
    Fiat: {
      Egea: ['1.4 Fire Easy', '1.3 Multijet Urban', '1.6 Multijet Lounge', '1.5 T4 Hibrit']
    }
  },
  'Arazi, SUV & Pickup': {
    Dacia: {
      Duster: ['1.0 TCe Comfort', '1.3 TCe Prestige 4x4', '1.5 dCi Laureate 4x4', '1.5 Blue dCi Journey 4x4'],
      Sandero_Stepway: ['1.0 TCe Comfort', '1.0 TCe Prestige']
    },
    Nissan: {
      Qashqai: ['1.3 DIG-T Tekna', '1.3 DIG-T Platinum Premium', '1.5 e-Power Design']
    },
    Peugeot: {
      '3008': ['1.2 PureTech Allure', '1.5 BlueHDi GT'],
      '2008': ['1.2 PureTech Active', '1.5 BlueHDi Allure']
    },
    Volkswagen: {
      Tiguan: ['1.5 TSI Elegance', '1.5 TSI R-Line', '2.0 TDI Elegance 4Motion']
    }
  },
  'Kamyonet & Hafif Ticari': {
    Ford: {
      Tourneo_Courier: ['1.5 TDCi Titanium', '1.5 TDCi Trend', '1.0 EcoBoost Titanium'],
      Transit: ['350 M Van', '350 L Kamyonet']
    },
    Fiat: {
      Doblo: ['1.3 Multijet Easy', '1.6 Multijet Premio Plus'],
      Fiorino: ['1.3 Multijet Pop', '1.3 Multijet Premio']
    },
    Volkswagen: {
      Caddy: ['2.0 TDI Life', '2.0 TDI Style'],
      Transporter: ['2.0 TDI City Van', '2.0 TDI Panelvan']
    }
  }
};

const REAL_ESTATE_HIERARCHY: Record<string, string[]> = {
  'Konut (Satılık)': ['Daire', 'Villa', 'Müstakil Ev', 'Rezidans'],
  'Konut (Kiralık)': ['Daire', 'Villa', 'Rezidans'],
  'İş Yeri': ['Dükkan & Mağaza', 'Ofis & Büro', 'Depo & Antrepo'],
  'Arsa': ['İmarlı - Konut', 'İmarlı - Ticari', 'Tarla']
};

export const CreateListingPage: React.FC = () => {
  const navigate = useNavigate();

  // KULLANICI TİPİ VE FOTOĞRAF KOTASI KONTROLÜ
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

  // Vasıta Kategori
  const [vehicleSub, setVehicleSub] = useState('Otomobil');
  const [vehicleBrand, setVehicleBrand] = useState('Renault');
  const [vehicleSeries, setVehicleSeries] = useState('Clio');
  const [vehiclePackage, setVehiclePackage] = useState('1.0 TCe Touch');

  // Emlak Kategori
  const [realEstateSub, setRealEstateSub] = useState('Konut (Satılık)');
  const [realEstateType, setRealEstateType] = useState('Daire');

  // Lokasyon
  const [cities, setCities] = useState<any[]>([]);
  const [districts, setDistricts] = useState<any[]>([]);
  const [neighborhoods, setNeighborhoods] = useState<any[]>([]);
  const [selectedCity, setSelectedCity] = useState('Ankara');
  const [selectedDistrict, setSelectedDistrict] = useState('Çankaya');
  const [selectedNeighborhood, setSelectedNeighborhood] = useState('Cumhuriyet Mah.');

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

  // Veritabanındaki geçerli kök kategori GUID'ini al (500 Foreign Key hatasını önler)
  useEffect(() => {
    api.get('/categories/roots')
      .then((res) => {
        if (res.data?.success && Array.isArray(res.data.data) && res.data.data.length > 0) {
          setDbCategoryId(res.data.data[0].id);
        }
      })
      .catch(() => {});

    api.get('/locations/cities').then((res) => {
      if (res.data?.success && Array.isArray(res.data.data)) {
        setCities(res.data.data);
      } else {
        setCities([
          { id: '1', name: 'Ankara' },
          { id: '2', name: 'İstanbul' },
          { id: '3', name: 'İzmir' },
          { id: '4', name: 'Bursa' }
        ]);
      }
    });

    handleCityChange('Ankara');
  }, []);

  const handleCityChange = async (cityName: string) => {
    setSelectedCity(cityName);
    setSelectedDistrict('');
    setSelectedNeighborhood('');

    const foundCity = cities.find((c) => c.name === cityName);
    if (foundCity?.id) {
      try {
        const res = await api.get(`/locations/districts/${foundCity.id}`);
        if (res.data?.success && Array.isArray(res.data.data) && res.data.data.length > 0) {
          setDistricts(res.data.data);
          setSelectedDistrict(res.data.data[0].name);
          handleDistrictChange(res.data.data[0].name);
          return;
        }
      } catch {}
    }
    const defaultDistricts = [
      { id: 'd1', name: 'Çankaya' },
      { id: 'd2', name: 'Yenimahalle' },
      { id: 'd3', name: 'Keçiören' },
      { id: 'd4', name: 'Beypazarı' }
    ];
    setDistricts(defaultDistricts);
    setSelectedDistrict(defaultDistricts[0].name);
    handleDistrictChange(defaultDistricts[0].name);
  };

  const handleDistrictChange = async (districtName: string) => {
    setSelectedDistrict(districtName);
    const defNeighborhoods = [
      { id: 'n1', name: 'Cumhuriyet Mah.' },
      { id: 'n2', name: 'Atatürk Mah.' },
      { id: 'n3', name: 'Fatih Mah.' }
    ];
    setNeighborhoods(defNeighborhoods);
    setSelectedNeighborhood(defNeighborhoods[0].name);
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
    if (!title.trim() || !price || !selectedCity) {
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

      // Veritabanındaki ilk kategori GUID'ini veya geçerli fallback GUID kullan:
      const categoryIdToSend = dbCategoryId || '11111111-1111-1111-1111-111111111111';

      const payload: any = {
        categoryId: categoryIdToSend,
        title: title.trim(),
        description: description.trim() || 'Açıklama belirtilmedi.',
        price: Number(price),
        currency: 0, // TRY
        city: selectedCity,
        district: selectedDistrict || 'Merkez',
        neighborhood: selectedNeighborhood || 'Cumhuriyet Mah.',
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
          Kategori Seçimi
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
                <span>Vasıta (Otomobil, SUV, Kamyonet)</span>
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
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Araç Türü</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={vehicleSub}
                      onChange={(e) => {
                        setVehicleSub(e.target.value);
                        const firstBrand = Object.keys(VEHICLE_HIERARCHY[e.target.value] || {})[0] || '';
                        setVehicleBrand(firstBrand);
                        const firstSeries = Object.keys(VEHICLE_HIERARCHY[e.target.value]?.[firstBrand] || {})[0] || '';
                        setVehicleSeries(firstSeries);
                        setVehiclePackage(VEHICLE_HIERARCHY[e.target.value]?.[firstBrand]?.[firstSeries]?.[0] || '');
                      }}
                    >
                      <option value="Otomobil">Otomobil</option>
                      <option value="Arazi, SUV & Pickup">Arazi, SUV & Pickup</option>
                      <option value="Kamyonet & Hafif Ticari">Kamyonet & Hafif Ticari</option>
                    </select>
                  </div>

                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Marka</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={vehicleBrand}
                      onChange={(e) => {
                        setVehicleBrand(e.target.value);
                        const firstSeries = Object.keys(VEHICLE_HIERARCHY[vehicleSub]?.[e.target.value] || {})[0] || '';
                        setVehicleSeries(firstSeries);
                        setVehiclePackage(VEHICLE_HIERARCHY[vehicleSub]?.[e.target.value]?.[firstSeries]?.[0] || '');
                      }}
                    >
                      {Object.keys(VEHICLE_HIERARCHY[vehicleSub] || {}).map((b) => (
                        <option key={b} value={b}>{b}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Seri</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={vehicleSeries}
                      onChange={(e) => {
                        setVehicleSeries(e.target.value);
                        setVehiclePackage(VEHICLE_HIERARCHY[vehicleSub]?.[vehicleBrand]?.[e.target.value]?.[0] || '');
                      }}
                    >
                      {Object.keys(VEHICLE_HIERARCHY[vehicleSub]?.[vehicleBrand] || {}).map((s) => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Paket & Motor</label>
                    <select
                      className="w-full border p-2 rounded bg-white"
                      value={vehiclePackage}
                      onChange={(e) => setVehiclePackage(e.target.value)}
                    >
                      {(VEHICLE_HIERARCHY[vehicleSub]?.[vehicleBrand]?.[vehicleSeries] || []).map((p) => (
                        <option key={p} value={p}>{p}</option>
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

            {/* Lokasyon */}
            <div className="pt-4 border-t grid grid-cols-3 gap-3">
              <div>
                <label className="block font-bold text-gray-700 mb-1">İl</label>
                <select
                  className="w-full border p-2 rounded bg-white"
                  value={selectedCity}
                  onChange={(e) => handleCityChange(e.target.value)}
                >
                  {cities.map((c) => (
                    <option key={c.id || c.name} value={c.name}>{c.name}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block font-bold text-gray-700 mb-1">İlçe</label>
                <select
                  className="w-full border p-2 rounded bg-white"
                  value={selectedDistrict}
                  onChange={(e) => handleDistrictChange(e.target.value)}
                >
                  {districts.map((d) => (
                    <option key={d.id || d.name} value={d.name}>{d.name}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block font-bold text-gray-700 mb-1">Mahalle</label>
                <select
                  className="w-full border p-2 rounded bg-white"
                  value={selectedNeighborhood}
                  onChange={(e) => setSelectedNeighborhood(e.target.value)}
                >
                  {neighborhoods.map((n) => (
                    <option key={n.id || n.name} value={n.name}>{n.name}</option>
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

            {/* FOTOĞRAFLAR (GALERİYE 20, STANDARDA 10 AYRIMI) */}
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