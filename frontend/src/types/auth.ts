export type UserRole = 'Cliente' | 'Trabajador' | 'Administrador';

export interface Usuario {
  id: number;
  nombre: string;
  email: string;
  rol: UserRole;
  fechaCreacion: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegistroRequest {
  nombre: string;
  email: string;
  password: string;
  rol?: UserRole;
}

export interface LoginResponse {
  token: string;
  usuario: Usuario;
}
