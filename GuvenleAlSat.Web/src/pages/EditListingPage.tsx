import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { Upload, X, ShieldCheck } from 'lucide-react';

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
  rearRightDoor: 'Sağ Arka Kapı'
};

export const EditListingPage: React.FC = () => {
  const { listingNo } = useParams<{ listingNo: string }>();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [listingId, setListingId] = useState('');

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [city, setCity] = useState('');
  const [district, setDistrict] = useState('');
  const [neighborhood, setNeighborhood] = useState('');

  // Araç Özellikleri
  const [year, setYear] = useState(2020);
  const [kilometer, setKilometer] = useState(0);
  const [fuelType, setFuelType] = useState('Benzin');
  const [transmission, setTransmission] = useState('Otomatik');
  const [bodyType, setBodyType] = useState('Sedan');
  const [color, setColor] = useState('Beyaz');
  const [heavyDamage, setHeavyDamage] = useState(false);

  // Ekspertiz Durumu (0: Orijinal, 1: Boyalı, 2: Değişen)
  const [damageReport, setDamageReport] = useState<Record<string, number>>({
    hood: 0, roof: 0, trunkLid: 0, frontBumper: 0, rearBumper: 0,
    frontLeftFender: 0, frontRightFender: 0, rearLeftFender: 0, rearRightFender: 0,
    frontLeftDoor: 0, frontRightDoor: 0, rearLeftDoor: 0, rearRightDoor: 0
  });

  // Fotoğraflar
  const [images, setImages] = useState<string[]>([]);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    setLoading(true);
    api.get(`/Listings/${listingNo}`)
      .then((res) => {
        if (res.data?.success) {
          const l = res.data.data;
          setListingId(l.id);
          setTitle(l.title || '');
          setDescription(l.description || '');
          setPrice(l.price ? l.price.toString() : '');
          setCity(l.city || '');
          setDistrict(l.district || '');
          setNeighborhood(l.neighborhood || '');

          if (l.vehicleDetail) {
            setYear(l.vehicleDetail.year || 2020);
            setKilometer(l.vehicleDetail.kilometer || 0);
            setFuelType(l.vehicleDetail.fuelType || 'Benzin');
            setTransmission(l.vehicleDetail.transmission || 'Otomatik');
            setBodyType(l.vehicleDetail.bodyType || 'Sedan');
            setColor(l.vehicleDetail.color || 'Beyaz');
            setHeavyDamage(!!l.vehicleDetail.heavyDamageRegistered);
          }

          if (l.damageReport) {
            setDamageReport({
              hood: Number(l.damageReport.hood ?? 0),
              roof: Number(l.damageReport.roof ?? 0),
              trunkLid: Number(l.damageReport.trunkLid ?? 0),
              frontBumper: Number(l.damageReport.frontBumper ?? 0),
              rearBumper: Number(l.damageReport.rearBumper ?? 0),
              frontLeftFender: Number(l.damageReport.frontLeftFender ?? 0),
              frontRightFender: Number(l.damageReport.frontRightFender ?? 0),
              rearLeftFender: Number(l.damageReport.rearLeftFender ?? 0),
              rearRightFender: Number(l.damageReport.rearRightFender ?? 0),
              frontLeftDoor: Number(l.damageReport.frontLeftDoor ?? 0),
              frontRightDoor: Number(l.damageReport.frontRightDoor ?? 0),
              rearLeftDoor: Number(l.damageReport.rearLeftDoor ?? 0),
              rearRightDoor: Number(l.damageReport.rearRightDoor ?? 0)
            });
          }

          if (l.images && Array.isArray(l.images)) {
            setImages(l.images.map((img: any) => img.imageUrl).filter(Boolean));
          }
        }
      })
      .catch((err) => console.error(err))
      .finally(() => setLoading(false));
  }, [listingNo]);

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || e.target.files.length === 0) return;
    setUploading(true);

    const formData = new FormData();
    Array.from(e.target.files).forEach((file) => formData.append('files', file));

    try {
      const res = await api.post('/Images/upload-multiple', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      if (res.data?.success && Array.isArray(res.data.data)) {
        setImages((prev) => [...prev, ...res.data.data.filter(Boolean)]);
      }
    } catch {
      alert('Fotoğraf yüklenemedi.');
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);

    try {
      // Temizlenmiş görsel listesi
      const cleanImages = (images || [])
        .filter((img) => img && typeof img === 'string' && img.trim() !== '')
        .map((url, idx) => ({
          imageUrl: url.trim(),
          isMain: idx === 0,
          displayOrder: idx + 1
        }));

      // Ekspertiz parçalarını kesin olarak integer sayı formatına çevir:
      const cleanDamageReport: Record<string, number> = {};
      Object.keys(PART_NAMES_TR).forEach((key) => {
        cleanDamageReport[key] = Number(damageReport[key] ?? 0);
      });

      const payload = {
        title: title.trim(),
        description: description.trim(),
        price: Number(price),
        currency: 0, // CurrencyType.TRY için sayısal enum değeri (400 hatasını engeller)
        city: city || 'Ankara',
        district: district || 'Merkez',
        neighborhood: neighborhood || 'Merkez Mah.',
        vehicleDetail: {
          year: Number(year),
          kilometer: Number(kilometer),
          fuelType,
          transmission,
          bodyType,
          color,
          heavyDamageRegistered: heavyDamage
        },
        damageReport: cleanDamageReport,
        images: cleanImages
      };

      const res = await api.put(`/Listings/${listingId}`, payload);
      if (res.data?.success) {
        alert('İlan bilgileri ve ekspertiz durumu başarıyla güncellendi!');
        navigate('/bana-ozel/ilanlarim');
      } else {
        alert(res.data?.message || 'Güncelleme yapılamadı.');
      }
    } catch (err: any) {
      console.error('Güncelleme hatası detayı:', err.response?.data);
      const errors = err.response?.data?.errors;
      if (errors && typeof errors === 'object') {
        const errorMessages = Object.entries(errors)
          .map(([field, msgs]) => `${field}: ${(msgs as string[]).join(', ')}`)
          .join('\n');
        alert(`Güncelleme Hatası:\n${errorMessages}`);
      } else {
        alert('Güncelleme hatası: ' + (err.response?.data?.message || err.message));
      }
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="text-center py-20 text-xs text-gray-500 font-bold">İlan bilgileri yükleniyor...</div>;

  return (
    <div className="max-w-[1000px] mx-auto px-4 py-8 text-xs">
      <div className="bg-white border rounded p-6 shadow-2xs space-y-5">
        <div className="flex justify-between items-center border-b pb-3">
          <h1 className="text-base font-bold text-gray-900">İlanı Düzenle (#{listingNo})</h1>
          <span className="text-gray-500 text-[11px]">{city} / {district}</span>
        </div>

        <form onSubmit={handleSave} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block font-bold text-gray-700 mb-1">İlan Başlığı</label>
              <input
                type="text"
                className="w-full border p-2 rounded text-xs"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
              />
            </div>
            <div>
              <label className="block font-bold text-gray-700 mb-1">Fiyat (TL)</label>
              <input
                type="number"
                className="w-full border p-2 rounded text-xs font-bold text-red-700"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                required
              />
            </div>
          </div>

          <div>
            <label className="block font-bold text-gray-700 mb-1">Açıklama</label>
            <textarea
              rows={3}
              className="w-full border p-2 rounded text-xs"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          {/* Araç Özellikleri */}
          <div className="grid grid-cols-3 gap-3 pt-3 border-t">
            <div>
              <label className="block font-bold text-gray-700 mb-1">Yıl</label>
              <input type="number" className="w-full border p-2 rounded text-xs" value={year} onChange={(e) => setYear(Number(e.target.value))} />
            </div>
            <div>
              <label className="block font-bold text-gray-700 mb-1">Kilometre</label>
              <input type="number" className="w-full border p-2 rounded text-xs" value={kilometer} onChange={(e) => setKilometer(Number(e.target.value))} />
            </div>
            <div>
              <label className="block font-bold text-gray-700 mb-1">Renk</label>
              <input type="text" className="w-full border p-2 rounded text-xs" value={color} onChange={(e) => setColor(e.target.value)} />
            </div>
          </div>

          <div>
            <label className="flex items-center gap-2 cursor-pointer font-bold text-red-600">
              <input type="checkbox" checked={heavyDamage} onChange={(e) => setHeavyDamage(e.target.checked)} />
              Ağır Hasar Kayıtlı (Pert)
            </label>
          </div>

          {/* Ekspertiz Parçaları */}
          <div className="pt-3 border-t">
            <label className="block font-bold text-gray-800 mb-2 flex items-center gap-1.5">
              <ShieldCheck className="w-4 h-4 text-emerald-600" />
              Boya & Değişen Durumu (Ekspertiz)
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

          {/* Fotoğraflar */}
          <div className="pt-3 border-t">
            <label className="block font-bold text-gray-700 mb-2">Fotoğraflar</label>
            <div className="flex flex-wrap gap-2 items-center">
              {images.map((img, idx) => (
                <div key={idx} className="relative w-20 h-16 border rounded overflow-hidden">
                  <img src={img} alt="" className="w-full h-full object-cover" />
                  {idx === 0 && <span className="absolute bottom-0 inset-x-0 bg-blue-600 text-white text-[8px] text-center font-bold">Kapak</span>}
                  <button type="button" onClick={() => setImages(images.filter((_, i) => i !== idx))} className="absolute top-1 right-1 bg-red-600 text-white rounded-full p-0.5">
                    <X className="w-2.5 h-2.5" />
                  </button>
                </div>
              ))}
              <label className="w-20 h-16 border-2 border-dashed rounded flex flex-col items-center justify-center cursor-pointer hover:border-blue-600 text-gray-400">
                <Upload className="w-4 h-4" />
                <span className="text-[9px] font-bold">{uploading ? '...' : '+ Ekle'}</span>
                <input type="file" multiple accept="image/*" className="hidden" onChange={handleFileUpload} />
              </label>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <button type="button" onClick={() => navigate('/bana-ozel/ilanlarim')} className="bg-gray-100 hover:bg-gray-200 text-gray-700 font-bold px-4 py-2 rounded">
              İptal
            </button>
            <button type="submit" disabled={saving} className="bg-[#0055b8] hover:bg-[#004494] text-white font-bold px-6 py-2 rounded">
              {saving ? 'Kaydediliyor...' : 'Değişiklikleri Kaydet'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};