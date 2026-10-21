export type EstadoCancha = 'Disponible' | 'Mantenimiento' | 'Fuera de servicio';

export interface Cancha {
  id: number;
  numero: number;
  sedeId: number;
  sedeNombre: string;
  tipoCanchaId: number;
  tipoCanchaNombre: string;
  precioPorHora: number;
  estado: EstadoCancha;
}

export interface CanchaCreateRequest {
  numero: number;
  sedeId: number;
  tipoCanchaId: number;
  estado: EstadoCancha;
}

export interface CanchaUpdateRequest {
  numero: number;
  sedeId: number;
  tipoCanchaId: number;
  estado: EstadoCancha;
}

export interface Sede {
  id: number;
  nombre: string;
  direccion: string;
  telefono?: string | null;
}

export interface SedeRequest {
  nombre: string;
  direccion: string;
  telefono?: string | null;
}

export interface TipoCancha {
  id: number;
  nombre: string;
  descripcion?: string | null;
  precioPorHora: number;
}

export interface TipoCanchaRequest {
  nombre: string;
  descripcion?: string | null;
  precioPorHora: number;
}

export type EstadoReserva = 'Pendiente' | 'Confirmada' | 'Cancelada' | 'Completada';

export interface Reserva {
  id: number;
  usuarioId: number;
  usuarioNombre: string;
  usuarioEmail: string;
  canchaId: number;
  canchaNumero: number;
  trabajadorId?: number | null;
  trabajadorNombre?: string | null;
  fechaInicio: string;
  fechaFin: string;
  estado: EstadoReserva;
}

export interface ReservaCreateRequest {
  canchaId: number;
  fechaInicio: string;
  fechaFin: string;
  usuarioId?: number | null;
  trabajadorId?: number | null;
}

export interface ReservaUpdateRequest {
  fechaInicio?: string | null;
  fechaFin?: string | null;
  estado?: string | null;
  trabajadorId?: number | null;
}


