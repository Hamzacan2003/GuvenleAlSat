import axios from 'axios';

// Canlı ortamda Vercel ortam değişkenini alır, yoksa canlı Render API adresine gider:
export const API_BASE_URL =
  import.meta.env.VITE_API_URL || 'https://guvenlealsat-api.onrender.com/api';

// Görsellerin Render üzerinden veya göreceli yoldan güvenle yüklenmesini sağlayan yardımcı
export const getFullImageUrl = (url?: string): string => {
  if (!url) return 'https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800&auto=format&fit=crop&q=60';
  
  // Eğer zaten tam bir https/http URL'si ise
  if (url.startsWith('http://') || url.startsWith('https://')) {
    // Localhost veya dahili IP kalmışsa canlı Render adresine çevir
    if (url.includes('localhost') || url.includes('127.0.0.1')) {
      const parts = url.split('/uploads/');
      if (parts.length > 1) {
        return `https://guvenlealsat-api.onrender.com/uploads/${parts[1]}`;
      }
    }
    return url;
  }

  // Göreceli yol (/uploads/resim.jpg) ise Render kök adresini ekle
  const cleanPath = url.startsWith('/') ? url : `/${url}`;
  return `https://guvenlealsat-api.onrender.com${cleanPath}`;
};

export const api = axios.create({
  baseURL: API_BASE_URL,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken') || localStorage.getItem('token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Yanıt interceptor'ı
api.interceptors.response.use(
  (response) => response,
  async (error: any) => {
    const originalRequest = error.config;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/Auth/login') &&
      !originalRequest.url?.includes('/Auth/register') &&
      !originalRequest.url?.includes('/Auth/refresh-token')
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers = originalRequest.headers || {};
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('refreshToken');

      if (!refreshToken) {
        isRefreshing = false;
        return Promise.reject(error);
      }

      try {
        // Dinamik API_BASE_URL kullanarak refresh-token isteği at
        const response = await axios.post(`${API_BASE_URL}/Auth/refresh-token`, {
          refreshToken: refreshToken,
        });

        const data = response.data?.data || response.data;
        const newAccessToken = data?.token || data?.accessToken;
        const newRefreshToken = data?.refreshToken;

        if (newAccessToken) {
          localStorage.setItem('token', newAccessToken);
          localStorage.setItem('accessToken', newAccessToken);
          if (newRefreshToken) {
            localStorage.setItem('refreshToken', newRefreshToken);
          }

          processQueue(null, newAccessToken);

          originalRequest.headers = originalRequest.headers || {};
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          return api(originalRequest);
        } else {
          throw new Error('Yeni access token alınamadı.');
        }
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        localStorage.removeItem('token');
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.location.reload();
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);