export const getCleanImageUrl = (url?: string | null): string => {
  if (!url || typeof url !== 'string' || url.trim() === '') {
    return 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=400&q=80';
  }

  // Eğer doğrudan tam bir web adresi ise
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }

  // Eğer backend localhost uploads klasöründen geliyorsa
  const baseUrl = 'http://localhost:5121';
  const cleanPath = url.startsWith('/') ? url : `/${url}`;
  return `${baseUrl}${cleanPath}`;
};