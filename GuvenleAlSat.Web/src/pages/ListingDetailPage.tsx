import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api } from '../services/api';
import { ShieldCheck, Phone, MessageSquare, Send, X, ExternalLink } from 'lucide-react';
import { getFullImageUrl } from '../services/api';
const getCleanImageUrl = (url?: string | null): string => {
  if (!url || typeof url !== 'string' || url.trim() === '') {
    return 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=800&q=80';
  }
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }
  const cleanPath = url.startsWith('/') ? url : `/${url}`;
  return `http://localhost:5121${cleanPath}`;
};

export const ListingDetailPage: React.FC = () => {
  const { listingNo } = useParams<{ listingNo: string }>();
  const [listing, setListing] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [phone, setPhone] = useState<string | null>(null);
  const [phoneLoading, setPhoneLoading] = useState(false);

  // Mesajlaşma State'leri
  const [isChatOpen, setIsChatOpen] = useState(false);
  const [messages, setMessages] = useState<any[]>([]);
  const [messageText, setMessageText] = useState('');
  const [sendingMsg, setSendingMsg] = useState(false);

  // Oturum ve Kullanıcı Çözümleme
  const token = localStorage.getItem('accessToken') || localStorage.getItem('token');
  let currentUserId: string | null = null;
  if (token) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      currentUserId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload['sub'] || payload['id'] || null;
    } catch {
      currentUserId = null;
    }
  }

  useEffect(() => {
    setLoading(true);
    api.get(`/Listings/${listingNo}`)
      .then((res) => {
        if (res.data?.success) {
          setListing(res.data.data);
        }
      })
      .catch((err) => console.error('İlan detayı alınamadı:', err))
      .finally(() => setLoading(false));
  }, [listingNo]);

  // Sohbet geçmişini getir
  useEffect(() => {
    if (isChatOpen && listing && currentUserId && listing.userId !== currentUserId) {
      api.get(`/messages/conversation?otherUserId=${listing.userId}&listingId=${listing.id}`)
        .then((res) => {
          if (res.data?.success && Array.isArray(res.data.data)) {
            setMessages(res.data.data);
          }
        })
        .catch(() => {});
    }
  }, [isChatOpen, listing, currentUserId]);

  const handleGetPhone = async () => {
    if (!listingNo) return;
    setPhoneLoading(true);
    try {
      const res = await api.get(`/Listings/${listingNo}/phone`);
      if (res.data?.success) {
        setPhone(res.data.data);
      }
    } catch {
      alert('Telefon numarası alınamadı.');
    } finally {
      setPhoneLoading(false);
    }
  };

  // Mesaj Gönderme
  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageText.trim() || !listing) return;

    if (!token) {
      alert('Satıcıya mesaj göndermek için önce giriş yapmalısınız.');
      return;
    }

    setSendingMsg(true);
    try {
      const payload = {
        receiverId: listing.userId,
        listingId: listing.id,
        content: messageText.trim()
      };

      await api.post('/messages', payload);

      setMessages((prev) => [
        ...prev,
        {
          id: Date.now().toString(),
          senderId: currentUserId,
          content: messageText.trim(),
          sentAt: new Date().toISOString()
        }
      ]);
      setMessageText('');
    } catch {
      alert('Mesaj gönderilemedi, lütfen tekrar deneyin.');
    } finally {
      setSendingMsg(false);
    }
  };

  const partNamesTr: Record<string, string> = {
    frontBumper: 'Ön Tampon',
    rearBumper: 'Arka Tampon',
    hood: 'Kaput',
    roof: 'Tavan',
    trunkLid: 'Bagaj Kapağı',
    frontLeftFender: 'Sol Ön Çamurluk',
    frontRightFender: 'Sağ Ön Çamurluk',
    rearLeftFender: 'Sol Arka Çamurluk',
    rearRightFender: 'Sağ Arka Çamurluk',
    frontLeftDoor: 'Sol Ön Kapı',
    frontRightDoor: 'Sağ Ön Kapı',
    rearLeftDoor: 'Sol Arka Kapı',
    rearRightDoor: 'Sağ Arka Kapı'
  };

  if (loading) {
    return <div className="text-center py-20 text-xs text-gray-500 font-bold">İlan detayları yükleniyor...</div>;
  }

  if (!listing) {
    return (
      <div className="text-center py-20 text-xs text-gray-500">
        <p className="text-sm font-bold text-gray-800 mb-2">İlan bulunamadı veya yayından kaldırılmış.</p>
        <Link to="/" className="text-[#0055b8] hover:underline font-semibold">&lt; Vitrine Geri Dön</Link>
      </div>
    );
  }

  const isOwner = currentUserId && listing.userId && currentUserId.toLowerCase() === listing.userId.toLowerCase();
  const rawImages = listing.images?.length > 0 
    ? listing.images 
    : [{ imageUrl: 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=800&q=80' }];

  return (
    <div className="max-w-[1200px] mx-auto px-3 py-4 text-xs">
      {/* Başlık ve İlan No */}
      <div className="border-b pb-3 mb-4 flex justify-between items-end">
        <div>
          <h1 className="text-lg font-bold text-gray-900">{listing.title}</h1>
          <div className="text-gray-500 text-[11px] flex gap-2 mt-1">
            <span>İlan No: <strong className="text-red-700 font-mono">{listing.listingNo}</strong></span>
            <span>•</span>
            <span>Konum: <strong>{listing.city} / {listing.district} / {listing.neighborhood}</strong></span>
          </div>
        </div>
        <div className="text-right">
          <div className="text-2xl font-black text-[#0055b8]">
            {listing.price?.toLocaleString('tr-TR')} {listing.currency || 'TL'}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-12 gap-6">
        {/* SOL: Fotoğraf Galerisi */}
        <div className="col-span-7">
          <div className="h-[400px] bg-black rounded overflow-hidden flex items-center justify-center border">
            <img 
              src={getCleanImageUrl(rawImages[activeImageIndex]?.imageUrl)} 
              alt={listing.title} 
              className="max-h-full max-w-full object-contain"
              onError={(e) => {
                (e.target as HTMLImageElement).src =
                  'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=800&q=80';
              }}
            />
          </div>

          {rawImages.length > 1 && (
            <div className="flex gap-2 mt-3 overflow-x-auto pb-2">
              {rawImages.map((img: any, idx: number) => (
                <button
                  key={idx}
                  onClick={() => setActiveImageIndex(idx)}
                  className={`w-16 h-12 rounded overflow-hidden border-2 shrink-0 ${activeImageIndex === idx ? 'border-[#0055b8]' : 'border-gray-200'}`}
                >
                  <img
                    src={getCleanImageUrl(img.imageUrl)}
                    alt=""
                    className="w-full h-full object-cover"
                    onError={(e) => {
                      (e.target as HTMLImageElement).src =
                        'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=200&q=80';
                    }}
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* SAĞ: Satıcı Bilgileri & Aksiyonlar */}
        <div className="col-span-5 space-y-4">
          <div className="bg-white border rounded p-4 shadow-2xs space-y-3">
            <div className="flex items-center gap-2">
              <div className="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center font-bold text-[#0055b8]">
                {listing.user?.firstName?.[0] || 'U'}
              </div>
              <div>
                <div className="font-bold text-gray-900 text-sm">
                  {listing.user?.firstName} {listing.user?.lastName}
                </div>
                <div className="text-emerald-700 text-[11px] font-semibold flex items-center gap-1">
                  <ShieldCheck className="w-3.5 h-3.5" />
                  NVİ Doğrulanmış Satıcı
                </div>
              </div>
            </div>
// Kullanım:
<img 
  src={getFullImageUrl(listing.mainImageUrl || listing.imageUrl)} 
  alt={listing.title}
  className="w-full h-full object-cover"
  onError={(e: any) => {
    e.target.src = 'https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800&auto=format&fit=crop&q=60';
  }}
/>
            <div className="pt-2 border-t space-y-2">
              {/* Telefon Butonu */}
              {phone ? (
                <div className="text-center font-mono font-bold text-base text-gray-900 bg-gray-50 py-2 border rounded">
                  {phone}
                </div>
              ) : (
                <button
                  onClick={handleGetPhone}
                  disabled={phoneLoading}
                  className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-2.5 rounded text-xs flex items-center justify-center gap-2 shadow-xs transition"
                >
                  <Phone className="w-4 h-4" />
                  {phoneLoading ? 'Alınıyor...' : 'Telefon Numarasını Göster'}
                </button>
              )}

              {/* Mesajlaşma Butonu */}
              {isOwner ? (
                <div className="w-full py-2 bg-gray-100 text-gray-500 text-center font-bold rounded text-[11px] border">
                  Bu ilan size aittir
                </div>
              ) : (
                <button
                  onClick={() => setIsChatOpen(!isChatOpen)}
                  className="w-full bg-[#0055b8] hover:bg-[#004494] text-white font-bold py-2.5 rounded text-xs flex items-center justify-center gap-2 shadow-xs transition"
                >
                  <MessageSquare className="w-4 h-4" />
                  {isChatOpen ? 'Mesaj Panelini Kapat' : 'Satıcıya Mesaj Gönder'}
                </button>
              )}
            </div>

            {/* Canlı Mesajlaşma Kutusu */}
            {isChatOpen && !isOwner && (
              <div className="border rounded p-3 bg-gray-50 space-y-2 mt-3">
                <div className="font-bold text-gray-800 text-[11px] flex justify-between items-center border-b pb-1">
                  <span>Satıcı ile Canlı Sohbet</span>
                  <div className="flex items-center gap-2">
                    <Link
                      to="/bana-ozel/mesajlarim"
                      className="text-[#0055b8] hover:underline flex items-center gap-0.5 text-[10px]"
                    >
                      <span>Gelen Kutusu</span>
                      <ExternalLink className="w-3 h-3" />
                    </Link>
                    <button onClick={() => setIsChatOpen(false)}>
                      <X className="w-3.5 h-3.5 text-gray-400" />
                    </button>
                  </div>
                </div>

                <div className="h-36 overflow-y-auto bg-white p-2 border rounded space-y-1.5 text-[11px]">
                  {messages.length === 0 ? (
                    <div className="text-gray-400 text-center py-6">Henüz bir mesajınız yok. İlk mesajı siz yazın!</div>
                  ) : (
                    messages.map((m) => (
                      <div
                        key={m.id}
                        className={`p-1.5 rounded max-w-[85%] ${
                          m.senderId === currentUserId ? 'ml-auto bg-blue-100 text-blue-900' : 'bg-gray-100 text-gray-800'
                        }`}
                      >
                        {m.content}
                      </div>
                    ))
                  )}
                </div>

                <form onSubmit={handleSendMessage} className="flex gap-1.5">
                  <input
                    type="text"
                    placeholder="Mesajınızı buraya yazın..."
                    className="flex-1 border p-1.5 rounded text-xs bg-white focus:outline-none"
                    value={messageText}
                    onChange={(e) => setMessageText(e.target.value)}
                  />
                  <button
                    type="submit"
                    disabled={sendingMsg || !messageText.trim()}
                    className="bg-[#0055b8] hover:bg-[#004494] text-white px-3 rounded flex items-center justify-center disabled:opacity-50"
                  >
                    <Send className="w-3.5 h-3.5" />
                  </button>
                </form>
              </div>
            )}
          </div>

          {/* Araç Teknik Detay Tablosu */}
          {listing.vehicleDetail && (
            <div className="bg-white border rounded p-4 shadow-2xs">
              <h3 className="font-bold text-xs text-gray-900 border-b pb-2 mb-2">Araç Bilgileri</h3>
              <div className="grid grid-cols-2 gap-y-2 text-[11px]">
                <span className="text-gray-500">Model Yılı</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.year}</span>
                <span className="text-gray-500">Kilometre</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.kilometer?.toLocaleString()} km</span>
                <span className="text-gray-500">Yakıt Türü</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.fuelType}</span>
                <span className="text-gray-500">Vites Tipi</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.transmission}</span>
                <span className="text-gray-500">Kasa Tipi</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.bodyType}</span>
                <span className="text-gray-500">Motor Gücü</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.enginePowerHp} HP</span>
                <span className="text-gray-500">Motor Hacmi</span>
                <span className="font-semibold text-right">{listing.vehicleDetail.engineCapacityCc} cc</span>
                <span className="text-gray-500">Ağır Hasar Kayıtlı</span>
                <span className={`font-bold text-right ${listing.vehicleDetail.heavyDamageRegistered ? 'text-red-600' : 'text-emerald-700'}`}>
                  {listing.vehicleDetail.heavyDamageRegistered ? 'Evet (Pert)' : 'Hayır'}
                </span>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ALT ALAN: Ekspertiz Tablosu */}
      {listing.damageReport && (
        <div className="bg-white border rounded p-4 mt-6 shadow-2xs">
          <h3 className="font-bold text-sm text-gray-900 border-b pb-2 mb-3 flex items-center gap-1.5">
            <ShieldCheck className="w-4 h-4 text-emerald-600" />
            Boya & Değişen Durumu (Ekspertiz)
          </h3>
          <div className="grid grid-cols-3 gap-2.5">
            {Object.entries(listing.damageReport).map(([part, status]: any) => {
              if (['id', 'listingid', 'createdat', 'updatedat', 'isdeleted'].includes(part.toLowerCase())) {
                return null;
              }

              const displayName = partNamesTr[part];
              if (!displayName) return null;

              const isOrig = status === 0 || status === 'Original';
              const isPaint = status === 1 || status === 'Painted';

              return (
                <div
                  key={part}
                  className={`p-2.5 rounded border flex justify-between items-center ${
                    isOrig
                      ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                      : isPaint
                      ? 'bg-amber-50 border-amber-200 text-amber-800'
                      : 'bg-red-50 border-red-200 text-red-800'
                  }`}
                >
                  <span className="font-medium text-xs">{displayName}</span>
                  <strong className="text-[11px] font-bold">
                    {isOrig ? 'Orijinal' : isPaint ? 'Boyalı' : 'Değişen'}
                  </strong>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Açıklama */}
      {listing.description && (
        <div className="bg-white border rounded p-4 mt-4 shadow-2xs">
          <h3 className="font-bold text-xs text-gray-900 border-b pb-2 mb-2">Açıklama</h3>
          <div className="text-gray-700 whitespace-pre-line leading-relaxed text-xs">
            {listing.description}
          </div>
        </div>
      )}
    </div>
  );
};