import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { App } from '../App';
import { authService } from '../api/authService';
import { canchasService } from '../api/canchasService';
import type { Cancha, Sede, TipoCancha, Reserva } from '../types/cancha';

// Mocks de servicios de API
vi.mock('../api/authService', () => ({
  authService: {
    login: vi.fn(),
  },
}));

vi.mock('../api/canchasService', () => ({
  canchasService: {
    listar: vi.fn(),
    listarSedes: vi.fn(),
    listarTiposCancha: vi.fn(),
    listarReservas: vi.fn(),
    crear: vi.fn(),
    crearReserva: vi.fn(),
  },
}));

const mockSedes: Sede[] = [
  { id: 1, nombre: 'Sede Providencia', direccion: 'Av. Providencia 1234', telefono: '+56 2 2233 4455' },
];

const mockTipos: TipoCancha[] = [
  { id: 1, nombre: 'Fútbol 5 Sintético', descripcion: 'Pasto sintético', precioPorHora: 20000 },
];

const mockCanchas: Cancha[] = [
  {
    id: 1,
    numero: 10,
    sedeId: 1,
    sedeNombre: 'Sede Providencia',
    tipoCanchaId: 1,
    tipoCanchaNombre: 'Fútbol 5 Sintético',
    precioPorHora: 20000,
    estado: 'Disponible',
  },
];

const mockReservas: Reserva[] = [];

describe('Prueba E2E de Flujo Completo en Frontend', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();

    vi.mocked(canchasService.listar).mockResolvedValue(mockCanchas);
    vi.mocked(canchasService.listarSedes).mockResolvedValue(mockSedes);
    vi.mocked(canchasService.listarTiposCancha).mockResolvedValue(mockTipos);
    vi.mocked(canchasService.listarReservas).mockResolvedValue(mockReservas);
  });

  it('Flujo E2E Cliente: Login -> Redirección a Dashboard -> Navegar a Canchas -> Abrir Modal y Confirmar Reserva', async () => {
    // 1. Simular respuesta exitosa de Login como Cliente
    vi.mocked(authService.login).mockResolvedValue({
      token: 'fake-jwt-token-cliente',
      usuario: {
        id: 101,
        nombre: 'Cliente E2E',
        email: 'cliente@canchas.com',
        rol: 'Cliente',
        fechaCreacion: '2026-10-21T00:00:00Z',
      },
    });

    vi.mocked(canchasService.crearReserva).mockResolvedValue({
      id: 99,
      usuarioId: 101,
      usuarioNombre: 'Cliente E2E',
      usuarioEmail: 'cliente@canchas.com',
      canchaId: 1,
      canchaNumero: 10,
      trabajadorId: null,
      trabajadorNombre: null,
      fechaInicio: '2026-10-22T18:00:00.000Z',
      fechaFin: '2026-10-22T19:00:00.000Z',
      estado: 'Pendiente',
    });

    render(<App />);

    // 2. Formulario de Inicio de Sesión
    expect(screen.getByRole('heading', { name: /iniciar sesión/i })).toBeInTheDocument();
    const emailInput = screen.getByLabelText(/correo electrónico/i);
    const passwordInput = screen.getByLabelText(/contraseña/i);
    const enterBtn = screen.getByRole('button', { name: /entrar/i });

    fireEvent.change(emailInput, { target: { value: 'cliente@canchas.com' } });
    fireEvent.change(passwordInput, { target: { value: '123456' } });
    fireEvent.click(enterBtn);

    // 3. Pantalla de Bienvenida (Inicio del Dashboard)
    await waitFor(() => {
      expect(screen.getByText(/¡bienvenido, cliente e2e!/i)).toBeInTheDocument();
    });

    // 4. Navegar al apartado Explorar Canchas
    const explorarBtn = screen.getByRole('button', { name: /explorar canchas/i });
    fireEvent.click(explorarBtn);

    await waitFor(() => {
      expect(screen.getByText('Cancha #10')).toBeInTheDocument();
    });

    // 5. Abrir Modal de Reserva
    const reservarBtn = screen.getByRole('button', { name: /reservar/i });
    fireEvent.click(reservarBtn);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /agendar reserva/i })).toBeInTheDocument();
    });

    // 6. Confirmar la Reserva
    const confirmarReservaBtn = screen.getByRole('button', { name: /confirmar reserva/i });
    fireEvent.click(confirmarReservaBtn);

    await waitFor(() => {
      expect(canchasService.crearReserva).toHaveBeenCalledTimes(1);
      expect(screen.getByText(/¡reserva para la cancha #10 confirmada exitosamente!/i)).toBeInTheDocument();
    });
  });

  it('Flujo E2E Administrador: Login -> Navegar a Canchas -> Abrir Modal y Crear Cancha', async () => {
    // 1. Simular respuesta de Login como Administrador
    vi.mocked(authService.login).mockResolvedValue({
      token: 'fake-jwt-token-admin',
      usuario: {
        id: 1,
        nombre: 'Admin E2E',
        email: 'admin@admin.com',
        rol: 'Administrador',
        fechaCreacion: '2026-10-21T00:00:00Z',
      },
    });

    vi.mocked(canchasService.crear).mockResolvedValue({
      id: 2,
      numero: 25,
      sedeId: 1,
      sedeNombre: 'Sede Providencia',
      tipoCanchaId: 1,
      tipoCanchaNombre: 'Fútbol 5 Sintético',
      precioPorHora: 20000,
      estado: 'Disponible',
    });

    render(<App />);

    const emailInput = screen.getByLabelText(/correo electrónico/i);
    const passwordInput = screen.getByLabelText(/contraseña/i);
    const enterBtn = screen.getByRole('button', { name: /entrar/i });

    fireEvent.change(emailInput, { target: { value: 'admin@admin.com' } });
    fireEvent.change(passwordInput, { target: { value: '123456' } });
    fireEvent.click(enterBtn);

    await waitFor(() => {
      expect(screen.getByText(/¡bienvenido, admin e2e!/i)).toBeInTheDocument();
    });

    // 2. Navegar a Canchas en el Sidebar
    const navCanchasBtn = screen.getByRole('button', { name: /^canchas$/i });
    fireEvent.click(navCanchasBtn);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /nueva cancha/i })).toBeInTheDocument();
    });

    // 3. Abrir Modal de Creación
    const nuevaCanchaBtn = screen.getByRole('button', { name: /nueva cancha/i });
    fireEvent.click(nuevaCanchaBtn);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /nueva cancha/i })).toBeInTheDocument();
    });

    // 4. Llenar formulario y Enviar
    const numeroInput = screen.getByLabelText(/número de cancha/i);
    fireEvent.change(numeroInput, { target: { value: '25' } });

    const guardarBtn = screen.getByRole('button', { name: /crear cancha/i });
    fireEvent.click(guardarBtn);

    await waitFor(() => {
      expect(canchasService.crear).toHaveBeenCalledTimes(1);
      expect(screen.getByText(/cancha #25 creada con éxito/i)).toBeInTheDocument();
    });
  });
});
