import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { ShieldCheck, Lock, KeyRound } from 'lucide-react';

export const ProfilePage: React.FC = () => {
  const [profile, setProfile] = useState<any>(null);
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [storeName, setStoreName] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  // Şifre Değiştirme State
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [newPasswordConfirm, setNewPasswordConfirm] = useState('');
  const [changingPassword, setChangingPassword] = useState(false);

  useEffect(() => {
    setLoading(true);
    api.get('/users/profile')
      .then((res) => {
        if (res.data?.success && res.data.data) {
          const u = res.data.data;
          setProfile(u);
          setEmail(u.email || '');
          setPhoneNumber(u.phoneNumber || '');
          setStoreName(u.storeName || '');
        }
      })
      .catch(() => {
        const localUser = JSON.parse(localStorage.getItem('user') || '{}');
        if (localUser && localUser.email) {
          setProfile(localUser);
          setEmail(localUser.email || '');
          setPhoneNumber(localUser.phoneNumber || '');
          setStoreName(localUser.storeName || '');
        }
      })
      .finally(() => setLoading(false));
  }, []);

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const res = await api.put('/users/profile', { email, phoneNumber, storeName });
      if (res.data?.success) {
        alert('İletişim bilgileriniz başarıyla güncellendi!');
        const savedUser = JSON.parse(localStorage.getItem('user') || '{}');
        localStorage.setItem('user', JSON.stringify({ ...savedUser, email, phoneNumber, storeName }));
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Güncelleme başarısız.');
    } finally {
      setSaving(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentPassword || !newPassword) {
      alert('Lütfen mevcut şifrenizi ve yeni şifrenizi giriniz.');
      return;
    }
    if (newPassword !== newPasswordConfirm) {
      alert('Yeni şifreler birbiriyle uyuşmuyor.');
      return;
    }
    if (newPassword.length < 6) {
      alert('Yeni şifre en az 6 karakter olmalıdır.');
      return;
    }

    setChangingPassword(true);
    try {
      const res = await api.post('/users/change-password', {
        currentPassword,
        newPassword
      });
      if (res.data?.success) {
        alert('Şifreniz başarıyla değiştirildi!');
        setCurrentPassword('');
        setNewPassword('');
        setNewPasswordConfirm('');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Mevcut şifreniz hatalı.');
    } finally {
      setChangingPassword(false);
    }
  };

  if (loading) return <div className="text-center py-20 text-xs font-bold text-gray-500">Profil bilgileri yükleniyor...</div>;
  if (!profile) return <div className="text-center py-20 text-xs text-red-600 font-bold">Profil alınamadı. Lütfen giriş yapınız.</div>;

  const isCorporate = profile.userType === 'Corporate' || profile.userType === 1;

  return (
    <div className="max-w-[700px] mx-auto px-4 py-8 text-xs space-y-6">
      {/* 1. HESAP VE İLETİŞİM BİLGİLERİ */}
      <div className="bg-white border rounded p-6 shadow-2xs space-y-4">
        <div className="flex items-center justify-between border-b pb-3">
          <h1 className="text-base font-bold text-gray-900">Hesap Bilgilerim</h1>
          <span className="flex items-center gap-1 text-emerald-700 font-bold bg-emerald-50 px-2 py-1 rounded text-[11px] border border-emerald-200">
            <ShieldCheck className="w-4 h-4 text-emerald-600" /> NVİ Onaylı Resmî Hesap
          </span>
        </div>

        <form onSubmit={handleSaveProfile} className="space-y-3">
          <div className="grid grid-cols-2 gap-3 bg-gray-50 p-3 rounded border">
            <div>
              <div className="flex items-center gap-1 font-bold text-gray-600 mb-1">
                <span>Ad (Resmî Kayıt)</span>
                <Lock className="w-3 h-3 text-gray-400" />
              </div>
              <input type="text" disabled className="w-full border p-2 rounded bg-gray-100 text-gray-700 cursor-not-allowed font-semibold" value={profile.firstName || ''} />
            </div>
            <div>
              <div className="flex items-center gap-1 font-bold text-gray-600 mb-1">
                <span>Soyad (Resmî Kayıt)</span>
                <Lock className="w-3 h-3 text-gray-400" />
              </div>
              <input type="text" disabled className="w-full border p-2 rounded bg-gray-100 text-gray-700 cursor-not-allowed font-semibold" value={profile.lastName || ''} />
            </div>
          </div>

          <div>
            <label className="block font-bold text-gray-700 mb-1">E-Posta Adresi</label>
            <input type="email" className="w-full border p-2 rounded bg-white" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>

          <div>
            <label className="block font-bold text-gray-700 mb-1">Telefon Numarası</label>
            <input type="text" className="w-full border p-2 rounded bg-white" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} required />
          </div>

          {isCorporate && (
            <div>
              <label className="block font-bold text-gray-700 mb-1">Mağaza / Galeri Adı</label>
              <input type="text" className="w-full border p-2 rounded bg-white font-bold text-amber-900" value={storeName} onChange={(e) => setStoreName(e.target.value)} />
            </div>
          )}

          <div className="pt-2 flex justify-end">
            <button type="submit" disabled={saving} className="bg-[#0055b8] hover:bg-[#004494] text-white font-bold px-6 py-2 rounded">
              {saving ? 'Kaydediliyor...' : 'Bilgileri Güncelle'}
            </button>
          </div>
        </form>
      </div>

      {/* 2. GÜVENLİK VE ŞİFRE DEĞİŞTİRME */}
      <div className="bg-white border rounded p-6 shadow-2xs space-y-4">
        <div className="border-b pb-3 flex items-center gap-2">
          <KeyRound className="w-4 h-4 text-gray-700" />
          <h2 className="text-sm font-bold text-gray-900">Şifre Değiştir</h2>
        </div>

        <form onSubmit={handleChangePassword} className="space-y-3">
          <div>
            <label className="block font-bold text-gray-700 mb-1">Mevcut Şifreniz *</label>
            <input
              type="password"
              placeholder="••••••••"
              className="w-full border p-2 rounded bg-white"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block font-bold text-gray-700 mb-1">Yeni Şifre *</label>
              <input
                type="password"
                placeholder="En az 6 karakter"
                className="w-full border p-2 rounded bg-white"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                required
              />
            </div>
            <div>
              <label className="block font-bold text-gray-700 mb-1">Yeni Şifre (Tekrar) *</label>
              <input
                type="password"
                placeholder="Tekrar giriniz"
                className="w-full border p-2 rounded bg-white"
                value={newPasswordConfirm}
                onChange={(e) => setNewPasswordConfirm(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="pt-2 flex justify-end">
            <button
              type="submit"
              disabled={changingPassword}
              className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold px-6 py-2 rounded shadow transition"
            >
              {changingPassword ? 'Güncelleniyor...' : 'Şifreyi Değiştir'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};