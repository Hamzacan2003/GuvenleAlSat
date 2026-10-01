import axios from 'axios';

// Canlı ortamda Vercel ortam değişkenini alır, yoksa canlı Render API adresine gider:
export const API_BASE_URL =
  import.meta.env.VITE_API_URL || 'https://guvenlealsat-api.onrender.com/api';

// Görsellerin Cloudinary veya Render üzerinden güvenle yüklenmesini sağlayan yardımcı
export const getFullImageUrl = (url?: string): string => {
  if (!url) return 'https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800&auto=format&fit=crop&q=60';
  
  // Zaten Cloudinary veya harici tam link ise dokunma
  if (url.startsWith('http://') || url.startsWith('https://')) {
    if (url.includes('localhost') || url.includes('127.0.0.1')) {
      const parts = url.split('/uploads/');
      if (parts.length > 1) {
        return `https://guvenlealsat-api.onrender.com/uploads/${parts[1]}`;
      }
    }
    return url;
  }

  // Göreceli yol (/uploads/resim.jpg) ise Render domainini ekle
  const cleanPath = url.startsWith('/') ? url : `/${url}`;
  return `https://guvenlealsat-api.onrender.com${cleanPath}`;
};

export const api = axios.create({
  baseURL: API_BASE_URL,
});

// İstek interceptor'ı: Bearer token ekler
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken') || localStorage.getItem('token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Yanıt interceptor'ı: 401 durumunda sonsuz reload'a sokmadan token temizler
api.interceptors.response.use(
  (response) => response,
  async (error: any) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      const isAuthUrl =
        originalRequest.url?.includes('/Auth/login') ||
        originalRequest.url?.includes('/Auth/register') ||
        originalRequest.url?.includes('/Auth/refresh-token');

      if (isAuthUrl) {
        return Promise.reject(error);
      }

      originalRequest._retry = true;
      const refreshToken = localStorage.getItem('refreshToken');

      if (refreshToken) {
        try {
          const res = await axios.post(`${API_BASE_URL}/Auth/refresh-token`, {
            refreshToken,
          });

          const data = res.data?.data || res.data;
          const newAccessToken = data?.token || data?.accessToken;
          const newRefreshToken = data?.refreshToken;

          if (newAccessToken) {
            localStorage.setItem('token', newAccessToken);
            localStorage.setItem('accessToken', newAccessToken);
            if (newRefreshToken) {
              localStorage.setItem('refreshToken', newRefreshToken);
            }

            originalRequest.headers = originalRequest.headers || {};
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
            return api(originalRequest);
          }
        } catch {
          // Token yenilenemezse temizle
          localStorage.removeItem('token');
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          localStorage.removeItem('user');
        }
      }
    }

    return Promise.reject(error);
  }
);