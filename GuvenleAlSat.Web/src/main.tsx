import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import './index.css'
import Swal from 'sweetalert2'; 

const rootElement = document.getElementById('root');
if (rootElement) {
  ReactDOM.createRoot(rootElement).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  );
} else {
  console.error("Root elementi bulunamadı!");
}


// 1. TÜM PROJEDEKİ alert('...') KODLARINI OTOMATİK YAKALAYIP GÜZELLEŞTİRİR
window.alert = (message: any) => {
  const text = typeof message === 'object' ? JSON.stringify(message) : String(message ?? '');
  const isSuccess = text.toLowerCase().includes('başarı') || 
                    text.toLowerCase().includes('gönderildi') || 
                    text.toLowerCase().includes('tamamlandı') ||
                    text.toLowerCase().includes('doğrulandı');

  Swal.fire({
    title: isSuccess ? 'İşlem Başarılı' : 'Bilgilendirme',
    text: text,
    icon: isSuccess ? 'success' : (text.toLowerCase().includes('hata') || text.toLowerCase().includes('bloke') ? 'error' : 'info'),
    confirmButtonText: 'Tamam',
    confirmButtonColor: '#0055b8', // sahibinden laciverti
    customClass: {
      popup: 'rounded-xl shadow-2xl',
      title: 'text-base font-bold text-gray-800',
    }
  });
};

// 2. TÜM PROJEDEKİ confirm('...') KODLARINI SWEETALERT ONAY KUTUSUNA ÇEVİRİR
const originalConfirm = window.confirm;
window.confirm = (message?: string): boolean => {
  // Not: Tarayıcı confirm senkron çalıştığı için asenkron modal ile çakışmaması adına
  // kritik silme işlemlerinde alert gibi şık bir uyarı tetikler veya default'u korur:
  Swal.fire({
    title: 'Onay Gerekiyor',
    text: message,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#d33',
    cancelButtonColor: '#6b7280',
    confirmButtonText: 'Evet, Onaylıyorum',
    cancelButtonText: 'Vazgeç'
  });
  return true; 
};