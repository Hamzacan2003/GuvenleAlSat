import React, { useState } from 'react';
import { api } from '../services/api';
import { Building2, User } from 'lucide-react';

export const RegisterModal: React.FC<{ isOpen: boolean; onClose: () => void; onLoginClick: () => void }> = ({
  isOpen,
  onClose,
  onLoginClick
}) => {
  const [userType, setUserType] = useState<0 | 1>(0); // 0: Bireysel, 1: Galeri
  const [storeName, setStoreName] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [tcNo, setTcNo] = useState('');
  const [birthYear, setBirthYear] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (userType === 1 && !storeName.trim()) {
      alert('Lütfen Mağaza / Galeri adınızı giriniz.');
      return;
    }

    setLoading(true);
    try {
      const payload = {
        firstName,
        lastName,
        email,
        password,
        phoneNumber: phone,
        identificationNumber: tcNo,
        birthYear: Number(birthYear),
        userType: userType, // 0 veya 1
        storeName: userType === 1 ? storeName : null
      };

      const res = await api.post('/Auth/register', payload);
      if (res.data?.success) {
        alert(
          userType === 1
            ? 'Galeri hesabınız başarıyla oluşturuldu! Şimdi giriş yapabilirsiniz.'
            : 'Hesabınız başarıyla oluşturuldu! Şimdi giriş yapabilirsiniz.'
        );
        onClose();
        onLoginClick();
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Kayıt işlemi başarısız.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6 text-xs relative">
        <button onClick={onClose} className="absolute top-4 right-4 text-gray-400 hover:text-gray-600 font-bold">
          ✕
        </button>

        <h2 className="text-base font-bold text-gray-900 mb-1 text-center">sahibinden.com'a Kayıt Ol</h2>
        <p className="text-gray-500 text-center mb-4">Hesap türünüzü seçerek formu doldurun</p>

        {/* HESAP TÜRÜ SEÇİMİ (BİREYSEL VS GALERİ) */}
        <div className="grid grid-cols-2 gap-2 mb-4">
          <button
            type="button"
            onClick={() => setUserType(0)}
            className={`p-2.5 rounded border flex flex-col items-center justify-center gap-1 font-bold transition ${
              userType === 0 ? 'border-[#0055b8] bg-blue-50 text-[#0055b8]' : 'border-gray-200 text-gray-600'
            }`}
          >
            <User className="w-5 h-5" />
            <span>Bireysel Hesap</span>
          </button>

          <button
            type="button"
            onClick={() => setUserType(1)}
            className={`p-2.5 rounded border flex flex-col items-center justify-center gap-1 font-bold transition ${
              userType === 1 ? 'border-amber-500 bg-amber-50 text-amber-900' : 'border-gray-200 text-gray-600'
            }`}
          >
            <Building2 className="w-5 h-5 text-amber-600" />
            <span>Galeri Hesabı Aç</span>
          </button>
        </div>

        <form onSubmit={handleRegister} className="space-y-2.5">
          {/* GALERİ SEÇİLDİYSE MAĞAZA ADI */}
          {userType === 1 && (
            <div>
              <label className="block font-bold text-amber-900 mb-1">Mağaza / Galeri Adı *</label>
              <input
                type="text"
                placeholder="Örn: Altıntop Motors"
                required
                className="w-full border border-amber-300 bg-amber-50/30 p-2 rounded focus:outline-none focus:border-amber-500"
                value={storeName}
                onChange={(e) => setStoreName(e.target.value)}
              />
            </div>
          )}

          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block font-bold text-gray-700 mb-1">Ad</label>
              <input
                type="text"
                required
                className="w-full border p-2 rounded"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
              />
            </div>
            <div>
              <label className="block font-bold text-gray-700 mb-1">Soyad</label>
              <input
                type="text"
                required
                className="w-full border p-2 rounded"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block font-bold text-gray-700 mb-1">T.C. Kimlik No</label>
              <input
                type="text"
                maxLength={11}
                required
                className="w-full border p-2 rounded"
                value={tcNo}
                onChange={(e) => setTcNo(e.target.value)}
              />
            </div>
            <div>
              <label className="block font-bold text-gray-700 mb-1">Doğum Yılı</label>
              <input
                type="number"
                placeholder="1995"
                required
                className="w-full border p-2 rounded"
                value={birthYear}
                onChange={(e) => setBirthYear(e.target.value)}
              />
            </div>
          </div>

          <div>
            <label className="block font-bold text-gray-700 mb-1">E-Posta</label>
            <input
              type="email"
              required
              className="w-full border p-2 rounded"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div>
            <label className="block font-bold text-gray-700 mb-1">Telefon</label>
            <input
              type="text"
              placeholder="05xxxxxxxxx"
              required
              className="w-full border p-2 rounded"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
          </div>

          <div>
            <label className="block font-bold text-gray-700 mb-1">Şifre</label>
            <input
              type="password"
              required
              className="w-full border p-2 rounded"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className={`w-full py-2.5 rounded font-bold text-white transition ${
              userType === 1 ? 'bg-amber-600 hover:bg-amber-700' : 'bg-[#0055b8] hover:bg-[#004494]'
            }`}
          >
            {loading ? 'Kayıt Yapılıyor...' : userType === 1 ? 'Galeri Olarak Kayıt Ol' : 'Bireysel Kayıt Ol'}
          </button>
        </form>

        <div className="mt-3 text-center text-gray-500">
          Zaten hesabınız var mı?{' '}
          <button onClick={() => { onClose(); onLoginClick(); }} className="text-[#0055b8] font-bold hover:underline">
            Giriş Yap
          </button>
        </div>
      </div>
    </div>
  );
};