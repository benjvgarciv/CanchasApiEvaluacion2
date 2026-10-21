import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { canchasService } from '../api/canchasService';
import type { Cancha, Sede, TipoCancha, Reserva } from '../types/cancha';
import { InicioSection } from '../components/InicioSection';
import { CanchasSection } from '../components/CanchasSection';
import { SedesSection } from '../components/SedesSection';
import { TiposCanchaSection } from '../components/TiposCanchaSection';
import { ReservasSection } from '../components/ReservasSection';
import {
  Home,
  Trophy,
  Building2,
  Tag,
  CalendarCheck,
  LogOut,
  Menu,
  X,
  UserCheck,
} from 'lucide-react';

type TabType = 'inicio' | 'canchas' | 'sedes' | 'tipos' | 'reservas';

export const Dashboard: React.FC = () => {
  const { usuario, logout } = useAuth();
  const navigate = useNavigate();

  // 1. Apartado 'inicio' establecido como pestaña principal por defecto
  const [activeTab, setActiveTab] = useState<TabType>('inicio');
  const [mobileMenuOpen, setMobileMenuOpen] = useState<boolean>(false);

  // Estados de datos sincronizados
  const [canchas, setCanchas] = useState<Cancha[]>([]);
  const [sedes, setSedes] = useState<Sede[]>([]);
  const [tiposCancha, setTiposCancha] = useState<TipoCancha[]>([]);
  const [reservas, setReservas] = useState<Reserva[]>([]);

  // Estados de carga y error individuales
  const [loadingCanchas, setLoadingCanchas] = useState<boolean>(true);
  const [loadingSedes, setLoadingSedes] = useState<boolean>(true);
  const [loadingTipos, setLoadingTipos] = useState<boolean>(true);
  const [loadingReservas, setLoadingReservas] = useState<boolean>(true);

  const [errorCanchas, setErrorCanchas] = useState<string | null>(null);
  const [errorSedes, setErrorSedes] = useState<string | null>(null);
  const [errorTipos, setErrorTipos] = useState<string | null>(null);
  const [errorReservas, setErrorReservas] = useState<string | null>(null);

  // 2. Roles y permisos (Criterios 3 y 6 de Rúbrica)
  const isCliente = usuario?.rol === 'Cliente';
  const canManage = usuario?.rol === 'Administrador' || usuario?.rol === 'Trabajador';

  // Funciones de carga por apartado
  const fetchCanchas = useCallback(async () => {
    setLoadingCanchas(true);
    setErrorCanchas(null);
    try {
      const data = await canchasService.listar();
      setCanchas(data);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setErrorCanchas(
        errorObj.response?.data?.error || errorObj.message || 'Error al conectar con la API de Canchas.'
      );
    } finally {
      setLoadingCanchas(false);
    }
  }, []);

  const fetchSedes = useCallback(async () => {
    setLoadingSedes(true);
    setErrorSedes(null);
    try {
      const data = await canchasService.listarSedes();
      setSedes(data);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setErrorSedes(
        errorObj.response?.data?.error || errorObj.message || 'Error al conectar con la API de Sedes.'
      );
    } finally {
      setLoadingSedes(false);
    }
  }, []);

  const fetchTipos = useCallback(async () => {
    setLoadingTipos(true);
    setErrorTipos(null);
    try {
      const data = await canchasService.listarTiposCancha();
      setTiposCancha(data);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setErrorTipos(
        errorObj.response?.data?.error || errorObj.message || 'Error al conectar con la API de Tipos.'
      );
    } finally {
      setLoadingTipos(false);
    }
  }, []);

  const fetchReservas = useCallback(async () => {
    setLoadingReservas(true);
    setErrorReservas(null);
    try {
      const data = await canchasService.listarReservas();
      setReservas(data);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setErrorReservas(
        errorObj.response?.data?.error || errorObj.message || 'Error al conectar con la API de Reservas.'
      );
    } finally {
      setLoadingReservas(false);
    }
  }, []);

  // Carga global al montar
  const refreshAll = useCallback(async () => {
    await Promise.all([fetchCanchas(), fetchSedes(), fetchTipos(), fetchReservas()]);
  }, [fetchCanchas, fetchSedes, fetchTipos, fetchReservas]);

  useEffect(() => {
    refreshAll();
  }, [refreshAll]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleSelectTab = (tab: TabType) => {
    setActiveTab(tab);
    setMobileMenuOpen(false);
  };

  return (
    <div className="portal-layout">
      {/* Overlay para móviles */}
      <div
        className={`portal-sidebar-overlay ${mobileMenuOpen ? 'open' : ''}`}
        onClick={() => setMobileMenuOpen(false)}
      />

      {/* 1. BARRA LATERAL (SIDEBAR) CON FILTRADO ESTRICTO SEGÚN ROL */}
      <aside className={`portal-sidebar ${mobileMenuOpen ? 'open' : ''}`}>
        <div className="portal-sidebar-brand">
          <div
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '8px',
              backgroundColor: 'var(--color-primary)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#ffffff',
              fontWeight: 700,
            }}
          >
            🏟️
          </div>
          <div>
            <h1>CanchasApi</h1>
            <span style={{ fontSize: '0.72rem', color: '#64748b' }}>
              {isCliente ? 'Portal del Jugador' : 'Portal de Administración'}
            </span>
          </div>
        </div>

        <ul className="portal-nav-list">
          {/* Opción 1: Inicio (Disponible para todos) */}
          <li>
            <button
              className={`portal-nav-item ${activeTab === 'inicio' ? 'active' : ''}`}
              onClick={() => handleSelectTab('inicio')}
            >
              <Home size={18} />
              <span>Inicio</span>
            </button>
          </li>

          {/* Opción 2: Canchas ("Explorar Canchas" para Cliente o "Canchas" para Admin/Trabajador) */}
          <li>
            <button
              className={`portal-nav-item ${activeTab === 'canchas' ? 'active' : ''}`}
              onClick={() => handleSelectTab('canchas')}
            >
              <Trophy size={18} />
              <span>{isCliente ? 'Explorar Canchas' : 'Canchas'}</span>
            </button>
          </li>

          {/* Opciones 3 y 4: Sedes y Tipos de Cancha (SOLO para Administrador o Trabajador; OCULTO para Cliente) */}
          {canManage && (
            <>
              <li>
                <button
                  className={`portal-nav-item ${activeTab === 'sedes' ? 'active' : ''}`}
                  onClick={() => handleSelectTab('sedes')}
                >
                  <Building2 size={18} />
                  <span>Sedes</span>
                </button>
              </li>
              <li>
                <button
                  className={`portal-nav-item ${activeTab === 'tipos' ? 'active' : ''}`}
                  onClick={() => handleSelectTab('tipos')}
                >
                  <Tag size={18} />
                  <span>Tipos de Cancha</span>
                </button>
              </li>
            </>
          )}

          {/* Opción 5: Reservas ("Mis Reservas" para Cliente o "Reservas" para Admin/Trabajador) */}
          <li>
            <button
              className={`portal-nav-item ${activeTab === 'reservas' ? 'active' : ''}`}
              onClick={() => handleSelectTab('reservas')}
            >
              <CalendarCheck size={18} />
              <span>{isCliente ? 'Mis Reservas' : 'Reservas'}</span>
            </button>
          </li>
        </ul>

        {/* Footer del sidebar con info de usuario y botón de logout */}
        <div className="portal-sidebar-footer">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
            <div
              style={{
                width: '34px',
                height: '34px',
                borderRadius: '50%',
                backgroundColor: '#1e293b',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: isCliente ? '#4ade80' : '#38bdf8',
              }}
            >
              <UserCheck size={18} />
            </div>
            <div style={{ overflow: 'hidden' }}>
              <div
                style={{
                  fontSize: '0.85rem',
                  fontWeight: 600,
                  color: '#f8fafc',
                  whiteSpace: 'nowrap',
                  textOverflow: 'ellipsis',
                  overflow: 'hidden',
                }}
              >
                {usuario?.nombre || 'Usuario'}
              </div>
              <div style={{ fontSize: '0.72rem', color: '#94a3b8' }}>{usuario?.rol}</div>
            </div>
          </div>

          <button
            onClick={handleLogout}
            style={{
              width: '100%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '0.5rem',
              padding: '0.55rem',
              backgroundColor: '#dc2626',
              color: '#ffffff',
              border: 'none',
              borderRadius: 'var(--radius-md)',
              fontSize: '0.85rem',
              fontWeight: 600,
              cursor: 'pointer',
              minHeight: '38px',
            }}
          >
            <LogOut size={16} />
            Cerrar Sesión
          </button>
        </div>
      </aside>

      {/* ÁREA DE CONTENIDO */}
      <main className="portal-main">
        {/* Barra superior Topbar */}
        <div className="portal-topbar">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <button
              className="btn-mobile-menu"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              aria-label="Abrir menú"
            >
              {mobileMenuOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
            <h2 style={{ margin: 0, fontSize: '1.15rem' }}>
              {activeTab === 'inicio' && 'Inicio'}
              {activeTab === 'canchas' && (isCliente ? 'Explorar Canchas Disponibles' : 'Gestión de Canchas')}
              {activeTab === 'sedes' && 'Complejos y Sedes Deportivas'}
              {activeTab === 'tipos' && 'Tipos de Cancha y Tarifas'}
              {activeTab === 'reservas' && (isCliente ? 'Mis Reservas Agendadas' : 'Agenda de Reservas')}
            </h2>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
            <span
              style={{
                fontSize: '0.8rem',
                padding: '0.25rem 0.65rem',
                backgroundColor: isCliente ? '#f0fdf4' : 'var(--color-primary-light)',
                color: isCliente ? '#166534' : 'var(--color-primary)',
                borderRadius: '9999px',
                fontWeight: 600,
                border: '1px solid var(--color-border)',
              }}
            >
              Rol: {usuario?.rol}
            </span>
          </div>
        </div>

        {/* Cuerpo del contenido dinámico */}
        <div className="portal-content-body">
          {/* 1. Vista Inicio (Pestaña por defecto) */}
          {activeTab === 'inicio' && (
            <InicioSection
              usuario={usuario}
              canchas={canchas}
              sedes={sedes}
              reservas={reservas}
              onNavigateTab={(tab) => setActiveTab(tab)}
            />
          )}

          {/* 2. Vista Canchas */}
          {activeTab === 'canchas' && (
            <CanchasSection
              canchas={canchas}
              sedes={sedes}
              tiposCancha={tiposCancha}
              isLoading={loadingCanchas}
              errorMessage={errorCanchas}
              canManage={canManage}
              onRefresh={async () => {
                await fetchCanchas();
                await fetchReservas();
              }}
              onNavigateToTab={(tab) => setActiveTab(tab)}
            />
          )}

          {/* 3. Vista Sedes (Solo Administrador / Trabajador) */}
          {activeTab === 'sedes' && canManage && (
            <SedesSection
              sedes={sedes}
              isLoading={loadingSedes}
              errorMessage={errorSedes}
              canManage={canManage}
              onRefresh={async () => {
                await fetchSedes();
                await fetchCanchas();
              }}
            />
          )}

          {/* 4. Vista Tipos de Cancha (Solo Administrador / Trabajador) */}
          {activeTab === 'tipos' && canManage && (
            <TiposCanchaSection
              tiposCancha={tiposCancha}
              isLoading={loadingTipos}
              errorMessage={errorTipos}
              canManage={canManage}
              onRefresh={async () => {
                await fetchTipos();
                await fetchCanchas();
              }}
            />
          )}

          {/* 5. Vista Reservas */}
          {activeTab === 'reservas' && (
            <ReservasSection
              reservas={reservas}
              isLoading={loadingReservas}
              errorMessage={errorReservas}
              isCliente={isCliente}
              canManage={canManage}
              currentUserId={usuario?.id}
              currentUserNombre={usuario?.nombre}
              onRefresh={fetchReservas}
              onNavigateToCanchas={() => setActiveTab('canchas')}
            />
          )}
        </div>
      </main>
    </div>
  );
};
