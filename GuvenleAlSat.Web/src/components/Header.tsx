import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Search, LogOut, ShieldCheck, ChevronDown, PlusCircle, MessageSquare } from 'lucide-react';
import { AuthModal } from './AuthModal';
import { api } from '../services/api';

export const Header: React.FC = () => {
  const navigate = useNavigate();
  const [currentUser, setCurrentUser] = useState<any>(null);
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const [keyword, setKeyword] = useState('');
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const saved = localStorage.getItem('user');
    if (saved) {
      try {
        setCurrentUser(JSON.parse(saved));
      } catch {
        setCurrentUser(null);
      }
    }
  }, []);

  // Okunmamış mesaj sayısını çek (Sadece oturum açıkken ve token varsa)
  useEffect(() => {
    const token = localStorage.getItem('token') || localStorage.getItem('accessToken');
    if (!currentUser || !token) return;

    const fetchUnread = () => {
      api.get('/messages/unread-count')
        .then((res) => {
          if (res.data?.success) {
            setUnreadCount(res.data.count ?? res.data.data ?? 0);
          }
        })
        .catch(() => {});
    };

    fetchUnread();
    const timer = setInterval(fetchUnread, 15000); // 15 saniyede bir kontrol et
    return () => clearInterval(timer);
  }, [currentUser]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setCurrentUser(null);
    setIsProfileOpen(false);
    navigate('/');
    window.location.reload();
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (keyword.trim()) navigate(`/?q=${encodeURIComponent(keyword)}`);
    else navigate('/');
  };

  const isCorporate = currentUser?.userType === 'Corporate' || currentUser?.userType === 1;

  return (
    <>
      <header className="bg-[#303e51] text-white w-full border-b border-[#232f3e] sticky top-0 z-40">
        <div className="max-w-[1200px] mx-auto h-[64px] px-3 flex items-center justify-between gap-4">
          <Link to="/" className="flex items-center">
            <span className="bg-[#ffe100] text-[#111] font-black text-2xl px-3 py-1 rounded tracking-tight shadow-sm">
              sahibinden<span className="text-[#303e51] font-normal">.com</span>
            </span>
          </Link>

          <form onSubmit={handleSearch} className="flex-1 max-w-[580px] flex items-center">
            <div className="relative w-full">
              <input
                type="text"
                placeholder="Kelime, ilan no veya mağaza adı ile ara"
                className="w-full h-[36px] bg-[#222c3a] text-white placeholder-gray-400 text-xs px-3 pr-10 rounded border border-[#44556b] focus:outline-none focus:bg-white focus:text-black"
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
              />
              <button type="submit" className="absolute right-0 top-0 h-[36px] w-[36px] flex items-center justify-center text-gray-300 hover:text-white">
                <Search className="w-4 h-4" />
              </button>
            </div>
          </form>

          <div className="flex items-center gap-3 text-xs font-semibold">
            {currentUser ? (
              <div className="flex items-center gap-2">
                {/* 1. MESAJ / BİLDİRİM BUTONU */}
                <Link
                  to="/bana-ozel/mesajlarim"
                  className="relative p-2 bg-[#222c3a] hover:bg-[#2c394b] border border-[#44556b] hover:border-[#ffe100] rounded text-white flex items-center justify-center transition"
                  title="Mesajlarım & Bildirimler"
                >
                  <MessageSquare className="w-4 h-4 text-gray-200" />
                  {unreadCount > 0 && (
                    <span className="absolute -top-1.5 -right-1.5 bg-red-600 text-white text-[10px] font-black w-4 h-4 rounded-full flex items-center justify-center animate-bounce shadow">
                      {unreadCount}
                    </span>
                  )}
                </Link>

                {/* 2. PROFİL MENÜSÜ */}
                <div className="relative" ref={dropdownRef}>
                  <button
                    onClick={() => setIsProfileOpen(!isProfileOpen)}
                    className="flex items-center gap-2 bg-[#222c3a] border border-[#44556b] px-3 py-1.5 rounded hover:border-[#ffe100] transition"
                  >
                    <div className="w-6 h-6 rounded-full bg-[#ffe100] text-[#111] flex items-center justify-center font-bold text-xs">
                      {currentUser.firstName ? currentUser.firstName[0].toUpperCase() : 'U'}
                    </div>
                    <span className="text-white font-bold max-w-[120px] truncate">
                      {currentUser.firstName} {currentUser.lastName}
                    </span>
                    <ChevronDown className="w-3.5 h-3.5 text-gray-400" />
                  </button>

                  {isProfileOpen && (
                    <div className="absolute right-0 mt-2 w-72 bg-white rounded shadow-2xl border border-gray-200 text-gray-800 z-50 overflow-hidden">
                      <div className="p-3 bg-gray-50 border-b border-gray-200">
                        <div className="font-bold text-sm text-gray-900">
                          {currentUser.firstName} {currentUser.lastName}
                        </div>
                        <div className="text-[11px] text-gray-500 truncate">{currentUser.email}</div>

                        {/* ROZETLER */}
                        <div className="mt-2 flex flex-wrap gap-1">
                          <span className="inline-flex items-center gap-1 bg-emerald-50 border border-emerald-200 text-emerald-700 px-2 py-0.5 rounded text-[10px] font-bold">
                            <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
                            NVİ Doğrulanmış
                          </span>
                          {isCorporate && (
                            <span className="inline-flex items-center gap-1 bg-amber-50 border border-amber-300 text-amber-800 px-2 py-0.5 rounded text-[10px] font-bold">
                              🏢 {currentUser.storeName || 'Galeri Hesabı'}
                            </span>
                          )}
                        </div>
                      </div>

                      <div className="p-2 space-y-1 text-xs">
                        <Link
                          to="/profilim"
                          onClick={() => setIsProfileOpen(false)}
                          className="block px-3 py-2 rounded hover:bg-blue-50 hover:text-[#0055b8] font-semibold text-gray-700"
                        >
                          Profilim / Hesap Bilgileri
                        </Link>
                        <Link
                          to="/bana-ozel/ilanlarim"
                          onClick={() => setIsProfileOpen(false)}
                          className="block px-3 py-2 rounded hover:bg-blue-50 hover:text-[#0055b8]"
                        >
                          Bana Özel / İlanlarım
                        </Link>
                        <Link
                          to="/bana-ozel/mesajlarim"
                          onClick={() => setIsProfileOpen(false)}
                          className="block px-3 py-2 rounded hover:bg-blue-50 hover:text-[#0055b8] flex justify-between items-center"
                        >
                          <span>Gelen Kutusu / Mesajlarım</span>
                          {unreadCount > 0 && (
                            <span className="bg-red-600 text-white font-bold px-1.5 py-0.2 rounded-full text-[10px]">
                              {unreadCount}
                            </span>
                          )}
                        </Link>
                        <Link
                          to="/ilan-ver"
                          onClick={() => setIsProfileOpen(false)}
                          className="block px-3 py-2 rounded hover:bg-blue-50 hover:text-[#0055b8]"
                        >
                          Yeni İlan Girişi
                        </Link>
                      </div>

                      <div className="p-2 border-t bg-gray-50">
                        <button
                          onClick={handleLogout}
                          className="w-full text-left px-3 py-1.5 text-red-600 font-bold hover:bg-red-50 rounded flex items-center gap-2"
                        >
                          <LogOut className="w-4 h-4" />
                          <span>Güvenli Çıkış</span>
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            ) : (
              <>
                <button onClick={() => setIsAuthModalOpen(true)} className="hover:underline text-gray-200">
                  Giriş Yap
                </button>
                <span className="text-gray-500">|</span>
                <button onClick={() => setIsAuthModalOpen(true)} className="hover:underline text-gray-200">
                  Hesap Aç
                </button>
              </>
            )}

            <Link
              to="/ilan-ver"
              className="bg-[#438ed8] hover:bg-[#357ebd] text-white font-bold px-4 py-2 rounded text-xs transition shadow-sm flex items-center gap-1.5"
            >
              <PlusCircle className="w-4 h-4" />
              <span>Ücretsiz* İlan Ver</span>
            </Link>
          </div>
        </div>
      </header>

      <AuthModal
        isOpen={isAuthModalOpen}
        onClose={() => setIsAuthModalOpen(false)}
        onSuccess={(u) => {
          setCurrentUser(u);
          localStorage.setItem('user', JSON.stringify(u));
        }}
      />
    </>
  );
};