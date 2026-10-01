import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Link } from 'react-router-dom';
import { Eye, PlusCircle, Zap, Trash2, Edit } from 'lucide-react';
import { getFullImageUrl } from '../services/api';
export const MyListingsPage: React.FC = () => {
  const [listings, setListings] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchListings = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Listings/my-listings');
      const data = res?.data;
      if (data?.success && Array.isArray(data.data)) {
        setListings(data.data);
      } else if (Array.isArray(data)) {
        setListings(data);
      } else {
        setListings([]);
      }
    } catch (err) {
      console.error('Kendi ilanlarım çekilemedi:', err);
      setListings([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchListings();
  }, []);

  const handleBoost = async (id: string, listingNo: number) => {
    if (!confirm(`#${listingNo} numaralı ilanınızı Premium Doping ile vitrinde öne çıkarmak istiyor musunuz?`)) return;
    try {
      const res = await api.post(`/Listings/${id}/boost-premium`);
      if (res.data?.success) {
        alert(res.data.message);
        fetchListings();
      }
    } catch {
      alert('Doping işlemi uygulanamadı.');
    }
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
  const handleDelete = async (id: string, listingNo: number) => {
    if (!confirm(`#${listingNo} numaralı ilanı satıldı olarak işaretleyip kalıcı olarak silmek istiyor musunuz?`)) return;
    try {
      const res = await api.delete(`/Listings/${id}`);
      if (res.data?.success) {
        alert('İlanınız başarıyla yayından kaldırıldı ve silindi.');
        setListings((prev) => prev.filter((item) => item.id !== id));
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'İlan silinemedi.');
    }
  };

  return (
    <div className="max-w-[1200px] mx-auto px-3 py-6 text-xs">
      <div className="flex justify-between items-center border-b pb-3 mb-4 bg-white p-4 rounded border">
        <div>
          <h1 className="text-base font-bold text-gray-900">Bana Özel &gt; İlanlarım</h1>
          <p className="text-gray-500 text-[11px] mt-0.5">Sadece sizin sisteme yüklediğiniz aktif ilanlar listelenir.</p>
        </div>
        <Link
          to="/ilan-ver"
          className="bg-[#0055b8] hover:bg-[#004494] text-white font-bold px-4 py-2 rounded text-xs flex items-center gap-1.5 transition"
        >
          <PlusCircle className="w-4 h-4" />
          Ücretsiz İlan Ver
        </Link>
      </div>

      {loading ? (
        <div className="text-center py-20 text-gray-500 font-bold bg-white rounded border">
          İlanlarınız yükleniyor...
        </div>
      ) : listings.length === 0 ? (
        <div className="text-center py-20 bg-white border rounded shadow-2xs space-y-3">
          <div className="w-16 h-16 bg-blue-50 text-[#0055b8] rounded-full flex items-center justify-center mx-auto text-2xl font-bold">
            0
          </div>
          <p className="font-bold text-gray-800 text-sm">Henüz yayında bir ilanınız bulunmuyor.</p>
          <p className="text-gray-500 text-xs">Aracınızı hemen satışa çıkarmak için ücretsiz ilan verebilirsiniz.</p>
          <Link
            to="/ilan-ver"
            className="inline-block mt-2 bg-[#ffe100] text-black hover:bg-[#ebd000] font-bold px-6 py-2 rounded text-xs transition"
          >
            Hemen İlan Ver &gt;
          </Link>
        </div>
      ) : (
        <div className="bg-white border rounded overflow-hidden shadow-2xs">
          <table className="w-full text-left border-collapse">
            <thead className="bg-[#f0f0f0] border-b text-[11px] text-gray-700 font-bold">
              <tr>
                <th className="p-3 w-28">Görsel</th>
                <th className="p-3">İlan No</th>
                <th className="p-3">Başlık</th>
                <th className="p-3">Fiyat</th>
                <th className="p-3">Konum</th>
                <th className="p-3">Yayın Tarihi</th>
                <th className="p-3 text-right">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y text-gray-700">
              {listings.map((item) => {
                const imgUrl = item.mainImageUrl || item.images?.[0]?.imageUrl || 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=200&q=80';
                const dateVal = item.publishedAt || item.createdAt;

                return (
                  <tr key={item.id || item.listingNo} className="hover:bg-[#f3f7fc] transition">
                    <td className="p-3">
                      <img
                        src={imgUrl}
                        alt=""
                        className="w-20 h-14 object-cover rounded border"
                      />
                    </td>
                    <td className="p-3 font-mono font-bold text-gray-900">#{item.listingNo}</td>
                    <td className="p-3 font-semibold text-[#0055b8] max-w-xs truncate">{item.title}</td>
                    <td className="p-3 font-black text-red-600 text-sm">
                      {item.price?.toLocaleString('tr-TR')} {item.currency === 0 || item.currency === 'TRY' ? 'TL' : item.currency}
                    </td>
                    <td className="p-3 text-gray-600">
                      {item.city} / {item.district}
                    </td>
                    <td className="p-3 text-gray-500 text-[11px]">
                      {dateVal ? new Date(dateVal).toLocaleDateString('tr-TR') : '-'}
                    </td>
                    <td className="p-3 text-right space-x-1.5 whitespace-nowrap">
                      <Link
                        to={`/ilan/${item.listingNo}`}
                        className="inline-flex items-center gap-1 bg-gray-100 hover:bg-gray-200 text-gray-800 font-bold px-2.5 py-1.5 rounded text-[11px] border"
                      >
                        <Eye className="w-3 h-3 text-blue-600" />
                        Görüntüle
                      </Link>

                      <Link
                        to={`/ilan-duzenle/${item.listingNo}`}
                        className="inline-flex items-center gap-1 bg-blue-600 hover:bg-blue-700 text-white font-bold px-2.5 py-1.5 rounded text-[11px] shadow-xs"
                      >
                        <Edit className="w-3 h-3" />
                        Düzenle
                      </Link>

                      <button
                        onClick={() => handleBoost(item.id, item.listingNo)}
                        className="inline-flex items-center gap-1 bg-[#ffe100] hover:bg-[#ebd000] text-black font-bold px-2.5 py-1.5 rounded text-[11px] border border-yellow-400 shadow-xs transition"
                        title="Vitrinde en üste taşır"
                      >
                        <Zap className="w-3 h-3 text-amber-700 fill-amber-700" />
                        Doping Yap
                      </button>

                      <button
                        onClick={() => handleDelete(item.id, item.listingNo)}
                        className="inline-flex items-center gap-1 bg-red-50 hover:bg-red-100 text-red-700 border border-red-200 font-bold px-2.5 py-1.5 rounded text-[11px] shadow-xs transition"
                        title="İlanı kalıcı olarak sil"
                      >
                        <Trash2 className="w-3 h-3" />
                        Sil
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};