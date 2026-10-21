import apiClient from './client';
import type { LoginRequest, LoginResponse, RegistroRequest, Usuario } from '../types/auth';

export const authService = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/api/v1/usuarios/login', credentials);
    return response.data;
  },

  registro: async (data: RegistroRequest): Promise<Usuario> => {
    const response = await apiClient.post<Usuario>('/api/v1/usuarios/registro', data);
    return response.data;
  },

  obtenerPerfil: async (id: number): Promise<Usuario> => {
    const response = await apiClient.get<Usuario>(`/api/v1/usuarios/${id}`);
    return response.data;
  },
};
