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

const ENGINE_CAPACITY_OPTIONS = [
  { label: '1.0 altı', value: '999' },
  { label: '1.0', value: '1000' },
  { label: '1.2', value: '1200' },
  { label: '1.3', value: '1332' },
  { label: '1.4', value: '1398' },
  { label: '1.5', value: '1498' },
  { label: '1.6', value: '1598' },
  { label: '1.8', value: '1798' },
  { label: '2.0', value: '1998' },
  { label: '2.5', value: '2498' },
  { label: '3.0 ve üzeri', value: '2998' },
  { label: 'Elektrikli (0 cc)', value: '0' }
];

// SUV ve Ticari Kataloğu (Veritabanında alt kategori henüz yoksa anında devreye girer)
const SUV_CATALOG: Record<string, string[]> = {
  'DACIA': ['Duster', 'Sandero Stepway', 'Jogger', 'Bigster'],
  'NISSAN': ['Qashqai', 'X-Trail', 'Juke'],
  'CHERY': ['Tiggo 7 Pro', 'Tiggo 8 Pro', 'Omoda 5'],
  'PEUGEOT': ['2008', '3008', '5008'],
  'VOLKSWAGEN': ['Tiguan', 'T-Roc', 'Taigo', 'Touareg'],
  'HYUNDAI': ['Tucson', 'Bayon', 'Kona', 'Santa Fe'],
  'TOYOTA': ['C-HR', 'RAV4', 'Corolla Cross', 'Yaris Cross', 'Land Cruiser'],
  'JEEP': ['Renegade', 'Compass', 'Wrangler', 'Grand Cherokee'],
  'KIA': ['Sportage', 'Stonic', 'XCeed', 'Sorento'],
  'BMW': ['X1', 'X2', 'X3', 'X4', 'X5', 'X6', 'X7'],
  'MERCEDES-BENZ': ['GLA', 'GLB', 'GLC', 'GLE', 'GLS', 'G Serisi'],
  'AUDI': ['Q2', 'Q3', 'Q5', 'Q7', 'Q8']
};

const COMMERCIAL_CATALOG: Record<string, string[]> = {
  'FORD': ['Tourneo Courier', 'Tourneo Connect', 'Tourneo Custom', 'Transit', 'Transit Custom', 'Ranger'],
  'FIAT': ['Doblo Combi', 'Doblo Cargo', 'Fiorino Combi', 'Fiorino Cargo', 'Ducato'],
  'RENAULT': ['Kangoo Multix', 'Kangoo Express', 'Trafic', 'Master'],
  'VOLKSWAGEN': ['Caddy', 'Transporter', 'Caravelle', 'Crafter', 'Amarok'],
  'PEUGEOT': ['Rifter', 'Partner', 'Expert', 'Boxer'],
  'CITROEN': ['Berlingo', 'Jumpy', 'Jumper'],
  'OPEL': ['Combo Life', 'Combo Cargo', 'Vivaro', 'Movano'],
  'TOYOTA': ['Hilux', 'Proace City']
};

export const CreateListingPage: React.FC = () => {
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

  const [mainType, setMainType] = useState<'vehicle' | 'realestate'>('vehicle');

  // --- KATEGORİ VE ARAÇ LİSTELERİ ---
  const [vehicleTypes, setVehicleTypes] = useState<any[]>([]);
  const [brands, setBrands] = useState<any[]>([]);
  const [models, setModels] = useState<any[]>([]);
  const [packages, setPackages] = useState<any[]>([]);

  const [selectedVehicleTypeId, setSelectedVehicleTypeId] = useState<string>('');
  const [selectedBrandId, setSelectedBrandId] = useState<string>('');
  const [selectedModelId, setSelectedModelId] = useState<string>('');
  const [selectedPackageId, setSelectedPackageId] = useState<string>('');

  // Emlak
  const [realEstateSub, setRealEstateSub] = useState('Konut (Satılık)');
  const [realEstateType, setRealEstateType] = useState('Daire');

  // Lokasyon
  const [cities, setCities] = useState<any[]>([]);
  const [districts, setDistricts] = useState<any[]>([]);
  const [neighborhoods, setNeighborhoods] = useState<any[]>([]);

  const [selectedCityId, setSelectedCityId] = useState<string>('');
  const [selectedCityName, setSelectedCityName] = useState<string>('');
  const [selectedDistrictId, setSelectedDistrictId] = useState<string>('');
  const [selectedDistrictName, setSelectedDistrictName] = useState<string>('');
  const [selectedNeighborhoodName, setSelectedNeighborhoodName] = useState<string>('');

  // Araç Özellikleri
  const [year, setYear] = useState('2022');
  const [kilometer, setKilometer] = useState('');
  const [fuelType, setFuelType] = useState('Benzin');
  const [transmission, setTransmission] = useState('Otomatik');
  const [bodyType, setBodyType] = useState('Sedan');
  const [color, setColor] = useState('Beyaz');
  const [engineCapacity, setEngineCapacity] = useState('1498');
  const [enginePower, setEnginePower] = useState('150');
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

  // Genel
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [images, setImages] = useState<string[]>([]);
  const [uploadingImage, setUploadingImage] = useState(false);

  // 1. Sayfa Açıldığında 81 İli ve Kök Kategoriden (Vasıta) Araç Türlerini Çek
  useEffect(() => {
    // 81 İli Çek
    api.get('/locations/cities').then((res) => {
      const cityData = Array.isArray(res.data) ? res.data : (res.data?.data || []);
      if (Array.isArray(cityData) && cityData.length > 0) {
        setCities(cityData);
        const ankara = cityData.find((c: any) => c.name.toLowerCase() === 'ankara') || cityData[0];
        setSelectedCityId(ankara.id);
        setSelectedCityName(ankara.name);
        fetchDistricts(ankara.id);
      }
    });

    // Kök Kategorileri Çek (Vasıta, Emlak)
    api.get('/Categories/roots').then(async (res) => {
      const rootList = Array.isArray(res.data) ? res.data : (res.data?.data || []);
      const vasita = rootList.find(
        (c: any) => c.slug?.toLowerCase().includes('vasita') || c.name?.toLowerCase().includes('vasıta')
      ) || rootList[0];

      if (vasita) {
        const subRes = await api.get(`/Categories/${vasita.id}/subcategories`);
        const vTypes = Array.isArray(subRes.data) ? subRes.data : (subRes.data?.data || []);
        setVehicleTypes(vTypes);

        if (vTypes.length > 0) {
          const firstType = vTypes[0];
          setSelectedVehicleTypeId(firstType.id);
          loadBrands(firstType.id, vTypes);
        }
      }
    }).catch((err) => {
      console.error('Kategori roots hatası:', err);
    });
  }, []);

  // Araç Türü Değişince Markaları Çek (Otomobil, SUV, Ticari)
  const loadBrands = async (vehicleTypeId: string, currentTypeList = vehicleTypes) => {
    setBrands([]);
    setModels([]);
    setPackages([]);

    const currentType = currentTypeList.find((t) => t.id === vehicleTypeId);
    const typeName = (currentType?.name || '').toLowerCase();

    try {
      const res = await api.get(`/Categories/${vehicleTypeId}/subcategories`);
      let bList = Array.isArray(res.data) ? res.data : (res.data?.data || []);

      // Eğer backend SUV veya Ticari altını boş dönmüşse kataloğu devreye al:
      if (bList.length === 0) {
        if (typeName.includes('suv') || typeName.includes('arazi')) {
          bList = Object.keys(SUV_CATALOG).map((b) => ({ id: `suv-${b}`, name: b, isCatalog: true }));
        } else if (typeName.includes('ticari') || typeName.includes('kamyonet')) {
          bList = Object.keys(COMMERCIAL_CATALOG).map((b) => ({ id: `com-${b}`, name: b, isCatalog: true }));
        }
      }

      setBrands(bList);
      if (bList.length > 0) {
        setSelectedBrandId(bList[0].id);
        loadModels(bList[0].id, bList[0]);
      }
    } catch (e) {
      console.error('Marka yükleme hatası:', e);
    }
  };

  // Marka Değişince Modelleri Çek
  const loadModels = async (brandId: string, brandObj?: any) => {
    setModels([]);
    setPackages([]);

    const bName = brandObj?.name || brands.find((b) => b.id === brandId)?.name || '';

    // SUV Kataloğu
    if (brandId.startsWith('suv-') && SUV_CATALOG[bName]) {
      const mList = SUV_CATALOG[bName].map((m) => ({ id: `model-${m}`, name: m, brandName: bName }));
      setModels(mList);
      if (mList.length > 0) {
        setSelectedModelId(mList[0].id);
        generateMockPackages(bName, mList[0].name, true, false);
      }
      return;
    }

    // Ticari Kataloğu
    if (brandId.startsWith('com-') && COMMERCIAL_CATALOG[bName]) {
      const mList = COMMERCIAL_CATALOG[bName].map((m) => ({ id: `model-${m}`, name: m, brandName: bName }));
      setModels(mList);
      if (mList.length > 0) {
        setSelectedModelId(mList[0].id);
        generateMockPackages(bName, mList[0].name, false, true);
      }
      return;
    }

    // Normal Veritabanı Modelleri (Otomobil)
    try {
      const res = await api.get(`/Categories/${brandId}/subcategories`);
      const mList = Array.isArray(res.data) ? res.data : (res.data?.data || []);
      setModels(mList);
      if (mList.length > 0) {
        setSelectedModelId(mList[0].id);
        loadPackages(mList[0].id);
      } else {
        setDbCategoryId(brandId);
      }
    } catch (e) {
      console.error('Model yükleme hatası:', e);
    }
  };

  // Model Değişince Paketleri Çek (Veritabanı)
  const loadPackages = async (modelId: string) => {
    setPackages([]);
    try {
      const res = await api.get(`/Categories/${modelId}/subcategories`);
      const pList = Array.isArray(res.data) ? res.data : (res.data?.data || []);
      setPackages(pList);
      if (pList.length > 0) {
        const firstP = pList[0];
        setSelectedPackageId(firstP.id);
        setDbCategoryId(firstP.id);
        syncVehicleDefaults(firstP);
      } else {
        setSelectedPackageId('');
        setDbCategoryId(modelId);
      }
    } catch (e) {
      console.error('Paket yükleme hatası:', e);
    }
  };

  // SUV & Ticari Paketlerini Hızlı Doldurma
  const generateMockPackages = (brand: string, model: string, isSuv: boolean, isCom: boolean) => {
    const list = [
      { id: 'p1', name: `${model} 1.0 / 1.5 Benzinli Otomatik`, defaultFuelType: 'Benzin', defaultTransmission: 'Otomatik', defaultBodyType: isSuv ? 'SUV' : 'Kamyonet/Van', defaultEngineCapacityCc: 1498, defaultEnginePowerHp: 150 },
      { id: 'p2', name: `${model} 1.5 / 2.0 Dizel Manuel`, defaultFuelType: 'Dizel', defaultTransmission: 'Manuel', defaultBodyType: isSuv ? 'SUV' : 'Kamyonet/Van', defaultEngineCapacityCc: 1598, defaultEnginePowerHp: 115 },
      { id: 'p3', name: `${model} 1.6 Hibrit / ECO Otomatik`, defaultFuelType: 'Hibrit', defaultTransmission: 'Otomatik', defaultBodyType: isSuv ? 'SUV' : 'Kamyonet/Van', defaultEngineCapacityCc: 1598, defaultEnginePowerHp: 140 }
    ];
    setPackages(list);
    setSelectedPackageId(list[0].id);
    setDbCategoryId(selectedVehicleTypeId);
    syncVehicleDefaults(list[0]);
  };

  const syncVehicleDefaults = (pkg: any) => {
    if (pkg.defaultFuelType) setFuelType(pkg.defaultFuelType);
    if (pkg.defaultTransmission) setTransmission(pkg.defaultTransmission);
    if (pkg.defaultBodyType) setBodyType(pkg.defaultBodyType);
    if (pkg.defaultEngineCapacityCc) setEngineCapacity(String(pkg.defaultEngineCapacityCc));
    if (pkg.defaultEnginePowerHp) setEnginePower(String(pkg.defaultEnginePowerHp));
  };

  // İlçeleri Çek
  const fetchDistricts = async (cityId: string) => {
    setDistricts([]);
    setNeighborhoods([]);
    try {
      const res = await api.get(`/locations/districts/${cityId}`);
      const list = Array.isArray(res.data) ? res.data : (res.data?.data || []);
      setDistricts(list);
      if (list.length > 0) {
        setSelectedDistrictId(list[0].id);
        setSelectedDistrictName(list[0].name);
        fetchNeighborhoods(list[0].id);
      }
    } catch (e) {
      console.error('İlçe yükleme hatası:', e);
    }
  };

  // Mahalleleri Çek
  const fetchNeighborhoods = async (districtId: string) => {
    setNeighborhoods([]);
    try {
      const res = await api.get(`/locations/neighborhoods/${districtId}`);
      const list = Array.isArray(res.data) ? res.data : (res.data?.data || []);
      setNeighborhoods(list);
      if (list.length > 0) {
        setSelectedNeighborhoodName(list[0].name);
      }
    } catch (e) {
      console.error('Mahalle yükleme hatası:', e);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || e.target.files.length === 0) return;
    if (images.length + e.target.files.length > maxPhotos) {
      alert(`En fazla ${maxPhotos} adet fotoğraf yükleyebilirsiniz.`);
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

      // Eğer mock paketse geçerli kategori ID olarak araç türünün ID'sini gönder
      let categoryIdToSend = dbCategoryId || selectedPackageId || selectedModelId || selectedBrandId;
      if (!categoryIdToSend || categoryIdToSend.startsWith('suv-') || categoryIdToSend.startsWith('com-') || categoryIdToSend.startsWith('model-') || categoryIdToSend.startsWith('p')) {
        categoryIdToSend = selectedVehicleTypeId;
      }

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
          enginePowerHp: Number(enginePower) || 120,
          engineCapacityCc: Number(engineCapacity) || 1500,
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
                  {/* Araç Türü */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Araç Türü</label>
                    <select
                      className="w-full border p-2 rounded bg-white font-semibold"
                      value={selectedVehicleTypeId}
                      onChange={(e) => {
                        const newId = e.target.value;
                        setSelectedVehicleTypeId(newId);
                        loadBrands(newId);
                      }}
                    >
                      {vehicleTypes.map((t) => (
                        <option key={t.id} value={t.id}>{t.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Marka */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Marka</label>
                    <select
                      className="w-full border p-2 rounded bg-white font-semibold"
                      value={selectedBrandId}
                      onChange={(e) => {
                        const bId = e.target.value;
                        setSelectedBrandId(bId);
                        loadModels(bId);
                      }}
                    >
                      {brands.map((b) => (
                        <option key={b.id} value={b.id}>{b.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Model / Seri */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Model / Seri</label>
                    <select
                      className="w-full border p-2 rounded bg-white font-semibold"
                      value={selectedModelId}
                      onChange={(e) => {
                        const mId = e.target.value;
                        setSelectedModelId(mId);
                        const selM = models.find((m) => m.id === mId);
                        if (selM?.brandName) {
                          const currentType = vehicleTypes.find((t) => t.id === selectedVehicleTypeId);
                          const isSuv = (currentType?.name || '').toLowerCase().includes('suv');
                          generateMockPackages(selM.brandName, selM.name, isSuv, !isSuv);
                        } else {
                          loadPackages(mId);
                        }
                      }}
                    >
                      {models.map((m) => (
                        <option key={m.id} value={m.id}>{m.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Donanım Paketi */}
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Paket & Donanım</label>
                    <select
                      className="w-full border p-2 rounded bg-white font-semibold"
                      value={selectedPackageId}
                      onChange={(e) => {
                        setSelectedPackageId(e.target.value);
                        setDbCategoryId(e.target.value);
                        const sel = packages.find((p) => p.id === e.target.value);
                        if (sel) syncVehicleDefaults(sel);
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
                    className="w-full border p-2 rounded bg-white font-semibold"
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
                    className="w-full border p-2 rounded bg-white font-semibold"
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
                <label className="block font-bold text-gray-700 mb-1">İl (81 İl)</label>
                <select
                  className="w-full border p-2 rounded bg-white font-semibold"
                  value={selectedCityId}
                  onChange={(e) => {
                    const cId = e.target.value;
                    setSelectedCityId(cId);
                    const cObj = cities.find((c) => c.id === cId);
                    setSelectedCityName(cObj?.name || '');
                    fetchDistricts(cId);
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
                  className="w-full border p-2 rounded bg-white font-semibold"
                  value={selectedDistrictId}
                  onChange={(e) => {
                    const dId = e.target.value;
                    setSelectedDistrictId(dId);
                    const dObj = districts.find((d) => d.id === dId);
                    setSelectedDistrictName(dObj?.name || '');
                    fetchNeighborhoods(dId);
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
                  className="w-full border p-2 rounded bg-white font-semibold"
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
                <h2 className="font-bold text-sm text-gray-900 border-b pb-2">2. Adım: Motor, Yıl, Vites ve Ekspertiz Seçimi</h2>
                <div className="grid grid-cols-3 gap-3">
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Model Yılı</label>
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={year} onChange={(e) => setYear(e.target.value)}>
                      {Array.from({ length: 40 }, (_, i) => 2026 - i).map((y) => (
                        <option key={y} value={y}>{y}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Kilometre</label>
                    <input type="number" placeholder="Örn: 95000" className="w-full border p-2 rounded" value={kilometer} onChange={(e) => setKilometer(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Yakıt Türü</label>
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={fuelType} onChange={(e) => setFuelType(e.target.value)}>
                      <option value="Benzin">Benzin</option>
                      <option value="Dizel">Dizel</option>
                      <option value="LPG & Benzin">LPG & Benzin</option>
                      <option value="Hibrit">Hibrit</option>
                      <option value="Elektrik">Elektrik</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Vites</label>
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={transmission} onChange={(e) => setTransmission(e.target.value)}>
                      <option value="Otomatik">Otomatik</option>
                      <option value="Manuel">Manuel</option>
                      <option value="Yarı Otomatik">Yarı Otomatik</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Motor Hacmi</label>
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={engineCapacity} onChange={(e) => setEngineCapacity(e.target.value)}>
                      {ENGINE_CAPACITY_OPTIONS.map((ec) => (
                        <option key={ec.value} value={ec.value}>{ec.label}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Motor Gücü (HP)</label>
                    <input type="number" placeholder="Örn: 150" className="w-full border p-2 rounded" value={enginePower} onChange={(e) => setEnginePower(e.target.value)} />
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Kasa Tipi</label>
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={bodyType} onChange={(e) => setBodyType(e.target.value)}>
                      <option value="Sedan">Sedan</option>
                      <option value="Hatchback">Hatchback</option>
                      <option value="Station Wagon">Station Wagon</option>
                      <option value="SUV">SUV</option>
                      <option value="Coupe">Coupe</option>
                      <option value="Cabrio">Cabrio</option>
                      <option value="Kamyonet/Van">Kamyonet/Van</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Renk</label>
                    <input type="text" className="w-full border p-2 rounded" value={color} onChange={(e) => setColor(e.target.value)} />
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
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={roomCount} onChange={(e) => setRoomCount(e.target.value)}>
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
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={heatingType} onChange={(e) => setHeatingType(e.target.value)}>
                      <option value="Kombi (Doğalgaz)">Kombi (Doğalgaz)</option>
                      <option value="Merkezi">Merkezi</option>
                      <option value="Yerden Isıtma">Yerden Isıtma</option>
                      <option value="Klima">Klima</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Banyo Sayısı</label>
                    <select className="w-full border p-2 rounded bg-white font-semibold" value={bathroomCount} onChange={(e) => setBathroomCount(e.target.value)}>
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

export const IlanVer = CreateListingPage;