import apiClient from './client';
import type {
  Cancha,
  CanchaCreateRequest,
  CanchaUpdateRequest,
  Sede,
  SedeRequest,
  TipoCancha,
  TipoCanchaRequest,
  Reserva,
  ReservaCreateRequest,
  ReservaUpdateRequest,
} from '../types/cancha';

export const canchasService = {
  listar: async (params?: { sedeId?: number; tipoCanchaId?: number; estado?: string }): Promise<Cancha[]> => {
    const response = await apiClient.get<Cancha[]>('/api/v1/canchas', { params });
    return response.data;
  },

  obtenerPorId: async (id: number): Promise<Cancha> => {
    const response = await apiClient.get<Cancha>(`/api/v1/canchas/${id}`);
    return response.data;
  },

  crear: async (cancha: CanchaCreateRequest): Promise<Cancha> => {
    const response = await apiClient.post<Cancha>('/api/v1/canchas', cancha);
    return response.data;
  },

  actualizar: async (id: number, cancha: CanchaUpdateRequest): Promise<Cancha> => {
    const response = await apiClient.put<Cancha>(`/api/v1/canchas/${id}`, cancha);
    return response.data;
  },

  eliminar: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/canchas/${id}`);
  },

  // Sedes CRUD (SedeApi.cs)
  listarSedes: async (): Promise<Sede[]> => {
    const response = await apiClient.get<Sede[]>('/api/v1/sedes');
    return response.data;
  },

  crearSede: async (sede: SedeRequest): Promise<Sede> => {
    const response = await apiClient.post<Sede>('/api/v1/sedes', sede);
    return response.data;
  },

  actualizarSede: async (id: number, sede: SedeRequest): Promise<Sede> => {
    const response = await apiClient.put<Sede>(`/api/v1/sedes/${id}`, sede);
    return response.data;
  },

  eliminarSede: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/sedes/${id}`);
  },

  // Tipos de Cancha CRUD (TipoCanchaApi.cs)
  listarTiposCancha: async (): Promise<TipoCancha[]> => {
    const response = await apiClient.get<TipoCancha[]>('/api/v1/tipos-cancha');
    return response.data;
  },

  crearTipoCancha: async (tipo: TipoCanchaRequest): Promise<TipoCancha> => {
    const response = await apiClient.post<TipoCancha>('/api/v1/tipos-cancha', tipo);
    return response.data;
  },

  actualizarTipoCancha: async (id: number, tipo: TipoCanchaRequest): Promise<TipoCancha> => {
    const response = await apiClient.put<TipoCancha>(`/api/v1/tipos-cancha/${id}`, tipo);
    return response.data;
  },

  eliminarTipoCancha: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/tipos-cancha/${id}`);
  },

  // Reservas (ReservaApi.cs)
  listarReservas: async (params?: { canchaId?: number; estado?: string }): Promise<Reserva[]> => {
    const response = await apiClient.get<Reserva[]>('/api/v1/reservas', { params });
    return response.data;
  },

  crearReserva: async (reserva: ReservaCreateRequest): Promise<Reserva> => {
    const response = await apiClient.post<Reserva>('/api/v1/reservas', reserva);
    return response.data;
  },

  actualizarReserva: async (id: number, reserva: ReservaUpdateRequest): Promise<Reserva> => {
    const response = await apiClient.put<Reserva>(`/api/v1/reservas/${id}`, reserva);
    return response.data;
  },
};


