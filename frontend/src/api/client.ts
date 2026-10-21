import axios, { AxiosError } from 'axios';

// URL base configurada desde variables de entorno con fallback
const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7269';

export const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor de Peticiones: inyecta Bearer token JWT si existe en storage
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('canchas_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor de Respuestas: normalización de respuestas y manejo centralizado de 401, 403, 429
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<{ error?: string; message?: string }>) => {
    const status = error.response?.status;

    if (status === 401) {
      // Sesión expirada o token inválido
      localStorage.removeItem('canchas_token');
      localStorage.removeItem('canchas_user');
      // Solo redirige si no estamos ya en login
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login?expired=1';
      }
    } else if (status === 429) {
      console.warn('Límite de peticiones alcanzado (Rate Limit excedido).');
    }

    return Promise.reject(error);
  }
);

export default apiClient;
