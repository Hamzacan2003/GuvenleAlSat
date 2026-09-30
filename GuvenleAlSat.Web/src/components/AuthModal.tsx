import React, { useState } from 'react';
import { api } from '../services/api';
import { ShieldCheck, Building2, User, X, MailCheck, KeyRound } from 'lucide-react';

interface AuthModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (user: any) => void;
}

export const AuthModal: React.FC<AuthModalProps> = ({ isOpen, onClose, onSuccess }) => {
  const [activeTab, setActiveTab] = useState<'login' | 'register' | 'forgot'>('login');
  const [authStep, setAuthStep] = useState<'form' | 'verify'>('form');

  // Giriş State
  const [loginEmail, setLoginEmail] = useState('');
  const [loginPassword, setLoginPassword] = useState('');

  // Şifremi Unuttum (OTP) State
  const [forgotStep, setForgotStep] = useState<'send_email' | 'enter_code'>('send_email');
  const [forgotEmail, setForgotEmail] = useState('');
  const [forgotCode, setForgotCode] = useState('');
  const [forgotNewPass, setForgotNewPass] = useState('');
  const [forgotNewPassConfirm, setForgotNewPassConfirm] = useState('');
  const [sendingCode, setSendingCode] = useState(false);
  const [resetting, setResetting] = useState(false);

  // Kayıt State
  const [userType, setUserType] = useState<0 | 1>(0); // 0: Bireysel, 1: Galeri
  const [storeName, setStoreName] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [tcNo, setTcNo] = useState('');
  const [birthYear, setBirthYear] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');

  // Kod Doğrulama State (Yeni Kayıt İçin)
  const [verificationCode, setVerificationCode] = useState('');
  const [verifying, setVerifying] = useState(false);
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  // Giriş Yap
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await api.post('/Auth/login', {
        email: loginEmail.trim(),
        password: loginPassword,
      });

      const data = res.data?.data || res.data;
      const token = data?.token || data?.accessToken;
      const refreshToken = data?.refreshToken;

      if (token) {
        localStorage.setItem('accessToken', token);
        localStorage.setItem('token', token);
        if (refreshToken) localStorage.setItem('refreshToken', refreshToken);

        let userData = data?.user;
        if (!userData) {
          userData = {
            email: loginEmail.trim(),
            firstName: data?.firstName || data?.name?.split(' ')[0] || 'Kullanıcı',
            lastName: data?.lastName || data?.name?.split(' ').slice(1).join(' ') || '',
            userType: data?.userType ?? (loginEmail.includes('galeri') ? 'Corporate' : 'Individual'),
            storeName: data?.storeName || null,
          };
        }

        try {
          const profileRes = await api.get('/users/profile', {
            headers: { Authorization: `Bearer ${token}` },
          });
          if (profileRes.data?.success && profileRes.data?.data) {
            userData = profileRes.data.data;
          }
        } catch {}

        localStorage.setItem('user', JSON.stringify(userData));
        onSuccess(userData);
        onClose();
        window.location.reload();
      } else {
        alert(res.data?.message || 'Giriş başarısız: Token alınamadı.');
      }
    } catch (err: any) {
      const errorMsg =
        err.response?.data?.message ||
        err.response?.data?.error ||
        err.message ||
        'E-posta veya şifre hatalı.';

      alert(errorMsg);

      // 3 Kez yanlış girilip bloke olduğunda otomatik şifre sıfırlamaya aktar
      if (
        errorMsg.toLowerCase().includes('bloke') ||
        errorMsg.toLowerCase().includes('kilitlenmiştir')
      ) {
        setForgotEmail(loginEmail.trim());
        setActiveTab('forgot');
        setForgotStep('send_email');
      }
    } finally {
      setLoading(false);
    }
  };

  // 1. Adım: Şifre Sıfırlama Kodunu İste
  const handleSendResetCode = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!forgotEmail.trim()) {
      alert('Lütfen e-posta adresinizi giriniz.');
      return;
    }

    setSendingCode(true);
    try {
      const res = await api.post('/Auth/forgot-password-code', { email: forgotEmail.trim() });
      if (res.data?.success) {
        alert('Doğrulama kodu e-postanıza gönderildi! Lütfen gelen kutunuzu kontrol ediniz.');
        setForgotStep('enter_code');
      } else {
        alert(res.data?.message || 'Kod gönderilemedi.');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'İşlem başarısız.');
    } finally {
      setSendingCode(false);
    }
  };

  // 2. Adım: Kodu ve Yeni Şifreyi Onayla
  const handleCompleteResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (forgotCode.trim().length !== 6) {
      alert('Lütfen 6 haneli doğrulama kodunu giriniz.');
      return;
    }
    if (forgotNewPass !== forgotNewPassConfirm) {
      alert('Yeni şifreler eşleşmiyor.');
      return;
    }
    if (forgotNewPass.length < 6) {
      alert('Yeni şifre en az 6 karakter olmalıdır.');
      return;
    }

    setResetting(true);
    try {
      const res = await api.post('/Auth/reset-password-with-otp', {
        email: forgotEmail.trim(),
        code: forgotCode.trim(),
        newPassword: forgotNewPass,
      });

      if (res.data?.success) {
        alert('Tebrikler! Şifreniz yenilendi ve hesabınızın kilidi açıldı. Şimdi yeni şifrenizle giriş yapabilirsiniz.');
        setActiveTab('login');
        setForgotStep('send_email');
        setLoginEmail(forgotEmail.trim());
        setForgotCode('');
        setForgotNewPass('');
        setForgotNewPassConfirm('');
      } else {
        alert(res.data?.message || 'Şifre güncellenemedi.');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Girdiğiniz kod hatalı veya süresi dolmuş.');
    } finally {
      setResetting(false);
    }
  };

  // Yeni Hesap Aç
  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (userType === 1 && !storeName.trim()) {
      alert('Lütfen Mağaza / Galeri adınızı giriniz.');
      return;
    }

    setLoading(true);
    try {
      const payload = {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        password,
        phoneNumber: phone.trim(),
        nationalIdNumber: tcNo.trim(),
        birthYear: Number(birthYear),
        userType: userType,
        storeName: userType === 1 ? storeName.trim() : null,
      };

      const res = await api.post('/Auth/register', payload);

      if (res.data?.success) {
        const data = res.data?.data;
        const token = data?.token || data?.accessToken;
        const refreshToken = data?.refreshToken;

        if (token) {
          localStorage.setItem('accessToken', token);
          localStorage.setItem('token', token);
          if (refreshToken) localStorage.setItem('refreshToken', refreshToken);
        }

        alert('Kayıt başarılı! E-posta adresinize 6 haneli doğrulama kodu gönderildi.');
        setAuthStep('verify');
      } else {
        alert(res.data?.message || 'Kayıt işlemi başarısız.');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Kayıt sırasında hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  // Yeni Kayıt E-posta Kodunu Doğrulama
  const handleVerifyCode = async (e: React.FormEvent) => {
    e.preventDefault();
    if (verificationCode.trim().length !== 6) {
      alert('Lütfen 6 haneli kodu eksiksiz giriniz.');
      return;
    }

    setVerifying(true);
    try {
      const res = await api.post('/Auth/verify-email', {
        email: email.trim(),
        code: verificationCode.trim(),
      });

      if (res.data?.success) {
        alert('E-posta adresiniz başarıyla doğrulandı! Oturumunuz açılıyor...');
        const userData = {
          email: email.trim(),
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          userType: userType === 1 ? 'Corporate' : 'Individual',
          storeName: userType === 1 ? storeName.trim() : null,
          isNviVerified: true,
        };
        localStorage.setItem('user', JSON.stringify(userData));
        onSuccess(userData);
        onClose();
        window.location.reload();
      } else {
        alert(res.data?.message || 'Kod doğrulanamadı.');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Kod hatalı veya süresi dolmuş.');
    } finally {
      setVerifying(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md overflow-hidden text-xs relative">
        <button
          onClick={onClose}
          className="absolute top-3 right-3 text-gray-400 hover:text-gray-700 z-10 p-1"
        >
          <X className="w-4 h-4" />
        </button>

        {authStep === 'verify' ? (
          /* YENİ KAYIT İÇİN 6 HANELİ E-POSTA ONAY EKRANI */
          <div className="p-6">
            <form onSubmit={handleVerifyCode} className="space-y-4 text-center">
              <div className="w-14 h-14 bg-blue-50 text-[#0055b8] rounded-full flex items-center justify-center mx-auto text-2xl font-bold shadow-inner">
                <MailCheck className="w-7 h-7 text-[#0055b8]" />
              </div>
              <div>
                <h3 className="font-bold text-base text-gray-900">E-postanızı Doğrulayın</h3>
                <p className="text-gray-500 text-[11px] mt-1.5 leading-relaxed">
                  <strong>{email}</strong> adresinize gönderilen 6 haneli onay kodunu giriniz.
                </p>
              </div>

              <div>
                <input
                  type="text"
                  maxLength={6}
                  autoFocus
                  placeholder="••••••"
                  className="w-48 mx-auto text-center tracking-[8px] text-2xl font-mono font-bold border-2 border-[#0055b8] bg-blue-50/20 p-2.5 rounded-lg focus:outline-none"
                  value={verificationCode}
                  onChange={(e) => setVerificationCode(e.target.value.replace(/\D/g, ''))}
                  required
                />
                <span className="block text-[10px] text-gray-400 mt-1">Kod 15 dakika boyunca geçerlidir.</span>
              </div>

              <button
                type="submit"
                disabled={verifying}
                className="w-full bg-[#0055b8] hover:bg-[#004494] text-white font-bold py-2.5 rounded transition shadow-sm"
              >
                {verifying ? 'Doğrulanıyor...' : 'Kodu Onayla ve Başla'}
              </button>
            </form>
          </div>
        ) : (
          <>
            {/* TABLAR */}
            <div className="grid grid-cols-2 border-b text-center font-bold text-sm">
              <button
                onClick={() => setActiveTab('login')}
                className={`py-3.5 border-b-2 transition ${
                  activeTab === 'login' || activeTab === 'forgot'
                    ? 'border-[#0055b8] text-[#0055b8] bg-white'
                    : 'border-transparent text-gray-500 bg-gray-50'
                }`}
              >
                Giriş Yap
              </button>
              <button
                onClick={() => setActiveTab('register')}
                className={`py-3.5 border-b-2 transition ${
                  activeTab === 'register'
                    ? 'border-[#0055b8] text-[#0055b8] bg-white'
                    : 'border-transparent text-gray-500 bg-gray-50'
                }`}
              >
                Hesap Aç (NVİ Onaylı)
              </button>
            </div>

            <div className="p-6">
              {/* GİRİŞ FORMU */}
              {activeTab === 'login' && (
                <form onSubmit={handleLogin} className="space-y-3">
                  <div>
                    <label className="block font-bold text-gray-700 mb-1">E-posta Adresi</label>
                    <input
                      type="email"
                      required
                      placeholder="ornek@mail.com"
                      className="w-full border p-2 rounded text-xs focus:outline-none focus:border-[#0055b8]"
                      value={loginEmail}
                      onChange={(e) => setLoginEmail(e.target.value)}
                    />
                  </div>

                  <div>
                    <div className="flex justify-between items-center mb-1">
                      <label className="block font-bold text-gray-700">Şifre</label>
                      <button
                        type="button"
                        onClick={() => {
                          setForgotEmail(loginEmail);
                          setActiveTab('forgot');
                          setForgotStep('send_email');
                        }}
                        className="text-[11px] text-[#0055b8] hover:underline"
                      >
                        Şifremi Unuttum?
                      </button>
                    </div>
                    <input
                      type="password"
                      required
                      placeholder="••••••••"
                      className="w-full border p-2 rounded text-xs focus:outline-none focus:border-[#0055b8]"
                      value={loginPassword}
                      onChange={(e) => setLoginPassword(e.target.value)}
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={loading}
                    className="w-full bg-[#0055b8] hover:bg-[#004494] text-white font-bold py-2.5 rounded transition mt-2 shadow-sm"
                  >
                    {loading ? 'Giriş Yapılıyor...' : 'Giriş Yap'}
                  </button>
                </form>
              )}

              {/* ŞİFREMİ UNUTTUM / E-POSTA OTP İLE SIFIRLAMA */}
              {activeTab === 'forgot' && (
                <div className="space-y-4">
                  <div className="flex items-center gap-1.5 text-gray-800 font-bold border-b pb-2">
                    <KeyRound className="w-4 h-4 text-[#0055b8]" />
                    <span>Şifre Sıfırlama & Bloke Kaldırma</span>
                  </div>

                  {forgotStep === 'send_email' ? (
                    <form onSubmit={handleSendResetCode} className="space-y-3">
                      <p className="text-[11px] text-gray-600 leading-relaxed">
                        Kayıtlı e-posta adresinizi giriniz. Size şifrenizi yenilemeniz için <strong>6 haneli bir güvenlik kodu</strong> göndereceğiz.
                      </p>

                      <div>
                        <label className="block font-bold text-gray-700 mb-1">E-posta Adresiniz</label>
                        <input
                          type="email"
                          required
                          placeholder="ornek@mail.com"
                          className="w-full border p-2 rounded text-xs focus:outline-none focus:border-[#0055b8]"
                          value={forgotEmail}
                          onChange={(e) => setForgotEmail(e.target.value)}
                        />
                      </div>

                      <button
                        type="submit"
                        disabled={sendingCode}
                        className="w-full bg-[#0055b8] hover:bg-[#004494] text-white font-bold py-2.5 rounded transition shadow-sm"
                      >
                        {sendingCode ? 'Kod Gönderiliyor...' : 'Doğrulama Kodu Gönder'}
                      </button>

                      <button
                        type="button"
                        onClick={() => setActiveTab('login')}
                        className="w-full text-center text-gray-500 hover:underline text-[11px] pt-1 block"
                      >
                        &lt; Giriş Ekranına Dön
                      </button>
                    </form>
                  ) : (
                    <form onSubmit={handleCompleteResetPassword} className="space-y-3">
                      <div className="bg-blue-50 border border-blue-200 p-2 rounded text-[11px] text-blue-800">
                        <strong>{forgotEmail}</strong> adresinize gönderilen 6 haneli kodu ve yeni şifrenizi giriniz.
                      </div>

                      <div>
                        <label className="block font-bold text-gray-700 mb-1">6 Haneli Doğrulama Kodu</label>
                        <input
                          type="text"
                          maxLength={6}
                          required
                          autoFocus
                          placeholder="••••••"
                          className="w-full border-2 border-[#0055b8] p-2 rounded text-center font-mono font-bold text-base tracking-[6px] focus:outline-none"
                          value={forgotCode}
                          onChange={(e) => setForgotCode(e.target.value.replace(/\D/g, ''))}
                        />
                      </div>

                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <label className="block font-bold text-gray-700 mb-1">Yeni Şifre</label>
                          <input
                            type="password"
                            required
                            placeholder="En az 6 karakter"
                            className="w-full border p-2 rounded text-xs"
                            value={forgotNewPass}
                            onChange={(e) => setForgotNewPass(e.target.value)}
                          />
                        </div>
                        <div>
                          <label className="block font-bold text-gray-700 mb-1">Yeni Şifre (Tekrar)</label>
                          <input
                            type="password"
                            required
                            placeholder="Tekrar girin"
                            className="w-full border p-2 rounded text-xs"
                            value={forgotNewPassConfirm}
                            onChange={(e) => setForgotNewPassConfirm(e.target.value)}
                          />
                        </div>
                      </div>

                      <button
                        type="submit"
                        disabled={resetting}
                        className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-2.5 rounded transition shadow-sm"
                      >
                        {resetting ? 'Güncelleniyor...' : 'Şifreyi Yenile ve Blokeyi Aç'}
                      </button>

                      <div className="flex justify-between text-[11px] pt-1">
                        <button
                          type="button"
                          onClick={() => setForgotStep('send_email')}
                          className="text-gray-500 hover:underline"
                        >
                          &lt; E-postayı Değiştir
                        </button>
                        <button
                          type="button"
                          onClick={handleSendResetCode}
                          className="text-[#0055b8] font-bold hover:underline"
                        >
                          Tekrar Kod İste
                        </button>
                      </div>
                    </form>
                  )}
                </div>
              )}

              {/* KAYIT FORMU */}
              {activeTab === 'register' && (
                <form onSubmit={handleRegister} className="space-y-3">
                  <div className="grid grid-cols-2 gap-2 mb-2">
                    <button
                      type="button"
                      onClick={() => setUserType(0)}
                      className={`p-2 rounded border flex items-center justify-center gap-1.5 font-bold transition text-xs ${
                        userType === 0
                          ? 'border-[#0055b8] bg-blue-50 text-[#0055b8]'
                          : 'border-gray-200 text-gray-600 bg-gray-50'
                      }`}
                    >
                      <User className="w-4 h-4" />
                      <span>Bireysel Hesap</span>
                    </button>

                    <button
                      type="button"
                      onClick={() => setUserType(1)}
                      className={`p-2 rounded border flex items-center justify-center gap-1.5 font-bold transition text-xs ${
                        userType === 1
                          ? 'border-amber-500 bg-amber-50 text-amber-900'
                          : 'border-gray-200 text-gray-600 bg-gray-50'
                      }`}
                    >
                      <Building2 className="w-4 h-4 text-amber-600" />
                      <span>Galeri Hesabı</span>
                    </button>
                  </div>

                  {userType === 1 && (
                    <div className="bg-amber-50/50 p-2.5 rounded border border-amber-200">
                      <label className="block font-bold text-amber-900 mb-1">Mağaza / Galeri Adı *</label>
                      <input
                        type="text"
                        required
                        placeholder="Örn: Altıntop Motors"
                        className="w-full border border-amber-300 p-2 rounded bg-white text-xs focus:outline-none focus:border-amber-500"
                        value={storeName}
                        onChange={(e) => setStoreName(e.target.value)}
                      />
                      <span className="text-[10px] text-amber-700 mt-1 block">
                        * Galeri hesabınızla 20 adet fotoğraf ve süresiz ilan hakkı kazanırsınız.
                      </span>
                    </div>
                  )}

                  <div className="bg-blue-50 border border-blue-200 p-2 rounded flex items-center gap-2 text-[#0055b8]">
                    <ShieldCheck className="w-4 h-4 shrink-0" />
                    <span className="text-[11px] leading-tight">
                      Kimlikteki tüm isimlerinizi (varsa iki ismi de) eksiksiz giriniz.
                    </span>
                  </div>

                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className="block font-bold text-gray-700 mb-1">Adınız (Tüm İsimler)</label>
                      <input
                        type="text"
                        required
                        placeholder="ÖRN: HAMZA CAN"
                        className="w-full border p-2 rounded text-xs"
                        value={firstName}
                        onChange={(e) => setFirstName(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="block font-bold text-gray-700 mb-1">Soyadınız</label>
                      <input
                        type="text"
                        required
                        placeholder="ÖRN: ALTINTOP"
                        className="w-full border p-2 rounded text-xs"
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
                        placeholder="11 Haneli TC"
                        className="w-full border p-2 rounded text-xs"
                        value={tcNo}
                        onChange={(e) => setTcNo(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="block font-bold text-gray-700 mb-1">Doğum Yılı</label>
                      <input
                        type="number"
                        required
                        placeholder="örn: 2003"
                        className="w-full border p-2 rounded text-xs"
                        value={birthYear}
                        onChange={(e) => setBirthYear(e.target.value)}
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Cep Telefonu</label>
                    <input
                      type="text"
                      required
                      placeholder="05xxxxxxxxx"
                      className="w-full border p-2 rounded text-xs"
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block font-bold text-gray-700 mb-1">E-posta</label>
                    <input
                      type="email"
                      required
                      placeholder="ornek@mail.com"
                      className="w-full border p-2 rounded text-xs"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block font-bold text-gray-700 mb-1">Şifre</label>
                    <input
                      type="password"
                      required
                      placeholder="••••••••"
                      className="w-full border p-2 rounded text-xs"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={loading}
                    className={`w-full py-2.5 rounded font-bold text-white transition shadow-sm ${
                      userType === 1 ? 'bg-amber-600 hover:bg-amber-700' : 'bg-[#0055b8] hover:bg-[#004494]'
                    }`}
                  >
                    {loading ? 'Kaydediliyor...' : userType === 1 ? 'Galeri Olarak Kaydol' : 'Kaydı Tamamla'}
                  </button>
                </form>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
};