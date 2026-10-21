import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CanchasSection } from '../components/CanchasSection';
import type { Cancha, Sede, TipoCancha } from '../types/cancha';

const mockSedes: Sede[] = [
  { id: 1, nombre: 'Sede Central', direccion: 'Av. Providencia 1234', telefono: '+56 2 2233 4455' },
];

const mockTipos: TipoCancha[] = [
  { id: 1, nombre: 'Fútbol 5', descripcion: 'Sintético', precioPorHora: 20000 },
];

const mockCanchas: Cancha[] = [
  {
    id: 1,
    numero: 1,
    sedeId: 1,
    sedeNombre: 'Sede Central',
    tipoCanchaId: 1,
    tipoCanchaNombre: 'Fútbol 5',
    precioPorHora: 20000,
    estado: 'Disponible',
  },
  {
    id: 2,
    numero: 2,
    sedeId: 1,
    sedeNombre: 'Sede Central',
    tipoCanchaId: 1,
    tipoCanchaNombre: 'Fútbol 5',
    precioPorHora: 20000,
    estado: 'Mantenimiento',
  },
];

describe('CanchasSection Component', () => {
  it('renderiza la lista de canchas correctamente con sus datos', () => {
    render(
      <CanchasSection
        canchas={mockCanchas}
        sedes={mockSedes}
        tiposCancha={mockTipos}
        isLoading={false}
        errorMessage={null}
        canManage={false} // Rol Cliente
        onRefresh={vi.fn()}
        onNavigateToTab={vi.fn()}
      />
    );

    expect(screen.getByText('Cancha #1')).toBeInTheDocument();
    expect(screen.getByText('Cancha #2')).toBeInTheDocument();
    expect(screen.getAllByText('Sede Central').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/20\.000/).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Disponible').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Mantenimiento').length).toBeGreaterThan(0);
  });

  it('el rol Cliente NO ve los botones de crear, editar ni eliminar (Criterio 6)', () => {
    render(
      <CanchasSection
        canchas={mockCanchas}
        sedes={mockSedes}
        tiposCancha={mockTipos}
        isLoading={false}
        errorMessage={null}
        canManage={false} // Rol Cliente
        onRefresh={vi.fn()}
        onNavigateToTab={vi.fn()}
      />
    );

    // No debe existir el botón Nueva Cancha
    expect(screen.queryByRole('button', { name: /nueva cancha/i })).not.toBeInTheDocument();

    // No deben existir botones de Editar ni Eliminar
    expect(screen.queryByRole('button', { name: /editar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /eliminar/i })).not.toBeInTheDocument();

    // Pero sí debe ver el botón de Reservar para canchas Disponibles
    expect(screen.getByRole('button', { name: /reservar/i })).toBeInTheDocument();
  });

  it('los roles Administrador/Trabajador (canManage = true) SÍ ven los botones de gestión', () => {
    render(
      <CanchasSection
        canchas={mockCanchas}
        sedes={mockSedes}
        tiposCancha={mockTipos}
        isLoading={false}
        errorMessage={null}
        canManage={true} // Rol Administrador o Trabajador
        onRefresh={vi.fn()}
        onNavigateToTab={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /nueva cancha/i })).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /editar/i }).length).toBe(2);
    expect(screen.getAllByRole('button', { name: /eliminar/i }).length).toBe(2);
  });
});
