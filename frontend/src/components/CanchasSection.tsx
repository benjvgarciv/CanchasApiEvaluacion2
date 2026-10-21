import React, { useState } from 'react';
import type {
  Cancha,
  CanchaCreateRequest,
  CanchaUpdateRequest,
  Sede,
  TipoCancha,
  EstadoCancha,
  ReservaCreateRequest,
} from '../types/cancha';
import { canchasService } from '../api/canchasService';
import { Plus, Search, Filter, Edit2, Trash2, AlertCircle, RefreshCw, Calendar, CheckCircle } from 'lucide-react';

interface CanchasSectionProps {
  canchas: Cancha[];
  sedes: Sede[];
  tiposCancha: TipoCancha[];
  isLoading: boolean;
  errorMessage: string | null;
  canManage: boolean;
  onRefresh: () => Promise<void>;
  onNavigateToTab: (tab: 'sedes' | 'tipos' | 'reservas') => void;
}

const ESTADOS_VALIDOS: EstadoCancha[] = ['Disponible', 'Mantenimiento', 'Fuera de servicio'];

interface FormState {
  numero: number | '';
  sedeId: number | '';
  tipoCanchaId: number | '';
  estado: EstadoCancha;
}

const INITIAL_FORM: FormState = {
  numero: '',
  sedeId: '',
  tipoCanchaId: '',
  estado: 'Disponible',
};

export const CanchasSection: React.FC<CanchasSectionProps> = ({
  canchas,
  sedes,
  tiposCancha,
  isLoading,
  errorMessage,
  canManage,
  onRefresh,
  onNavigateToTab,
}) => {
  // Filtros locales
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [filterSede, setFilterSede] = useState<string>('');
  const [filterEstado, setFilterEstado] = useState<string>('');

  // Modal Crear / Editar (Administrativo)
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<FormState>(INITIAL_FORM);
  const [formSubmitting, setFormSubmitting] = useState<boolean>(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Modal Reservar Cancha (Flujo Cliente)
  const [reservaModalOpen, setReservaModalOpen] = useState<boolean>(false);
  const [canchaReservar, setCanchaReservar] = useState<Cancha | null>(null);
  const [reservaFecha, setReservaFecha] = useState<string>('');
  const [reservaHoraInicio, setReservaHoraInicio] = useState<string>('18:00');
  const [reservaHoraFin, setReservaHoraFin] = useState<string>('19:00');
  const [reservaSubmitting, setReservaSubmitting] = useState<boolean>(false);
  const [reservaError, setReservaError] = useState<string | null>(null);

  // Feedback general
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const canchasFiltradas = canchas.filter((c) => {
    const matchNumero = c.numero.toString().includes(searchTerm.trim());
    const matchSedeNombre = c.sedeNombre.toLowerCase().includes(searchTerm.toLowerCase());
    const matchTipo = c.tipoCanchaNombre.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesSearch = searchTerm ? matchNumero || matchSedeNombre || matchTipo : true;
    const matchesSede = filterSede ? c.sedeId.toString() === filterSede : true;
    const matchesEstado = filterEstado ? c.estado === filterEstado : true;
    return matchesSearch && matchesSede && matchesEstado;
  });

  // Manejo de Modal Administrativo
  const handleAbrirCrear = () => {
    setFormError(null);
    setEditingId(null);
    setFormData({
      numero: '',
      sedeId: sedes.length > 0 ? sedes[0].id : '',
      tipoCanchaId: tiposCancha.length > 0 ? tiposCancha[0].id : '',
      estado: 'Disponible',
    });
    setIsModalOpen(true);
  };

  const handleAbrirEditar = (cancha: Cancha) => {
    setFormError(null);
    setEditingId(cancha.id);
    setFormData({
      numero: cancha.numero,
      sedeId: cancha.sedeId,
      tipoCanchaId: cancha.tipoCanchaId,
      estado: cancha.estado,
    });
    setIsModalOpen(true);
  };

  const handleCerrarModal = () => {
    setIsModalOpen(false);
    setEditingId(null);
    setFormData(INITIAL_FORM);
    setFormError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFeedback(null);

    if (formData.numero === '' || Number(formData.numero) <= 0) {
      setFormError('El número de cancha debe ser mayor a 0.');
      return;
    }
    if (formData.sedeId === '') {
      setFormError('Selecciona una sede válida.');
      return;
    }
    if (formData.tipoCanchaId === '') {
      setFormError('Selecciona un tipo de cancha válido.');
      return;
    }

    setFormSubmitting(true);
    try {
      if (editingId !== null) {
        const updatePayload: CanchaUpdateRequest = {
          numero: Number(formData.numero),
          sedeId: Number(formData.sedeId),
          tipoCanchaId: Number(formData.tipoCanchaId),
          estado: formData.estado,
        };
        await canchasService.actualizar(editingId, updatePayload);
        setFeedback({ type: 'success', text: `Cancha #${formData.numero} actualizada con éxito.` });
      } else {
        const createPayload: CanchaCreateRequest = {
          numero: Number(formData.numero),
          sedeId: Number(formData.sedeId),
          tipoCanchaId: Number(formData.tipoCanchaId),
          estado: formData.estado,
        };
        await canchasService.crear(createPayload);
        setFeedback({ type: 'success', text: `Cancha #${formData.numero} creada con éxito.` });
      }
      handleCerrarModal();
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string; message?: string } }; message?: string };
      setFormError(
        errorObj.response?.data?.error ||
        errorObj.response?.data?.message ||
        errorObj.message ||
        'Error al guardar la cancha.'
      );
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleEliminar = async (cancha: Cancha) => {
    const confirmacion = window.confirm(
      `¿Deseas eliminar la Cancha #${cancha.numero} (${cancha.sedeNombre})?`
    );
    if (!confirmacion) return;

    setFeedback(null);
    try {
      await canchasService.eliminar(cancha.id);
      setFeedback({ type: 'success', text: `Cancha #${cancha.numero} eliminada exitosamente.` });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string; message?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text:
          errorObj.response?.data?.error ||
          errorObj.response?.data?.message ||
          'No se pudo eliminar la cancha.',
      });
    }
  };

  // Manejo de Modal de Reserva (Cliente)
  const handleAbrirReserva = (cancha: Cancha) => {
    setCanchaReservar(cancha);
    setReservaError(null);
    // Establecer fecha por defecto para mañana
    const manana = new Date();
    manana.setDate(manana.getDate() + 1);
    const fechaISO = manana.toISOString().split('T')[0];
    setReservaFecha(fechaISO);
    setReservaHoraInicio('18:00');
    setReservaHoraFin('19:00');
    setReservaModalOpen(true);
  };

  const handleCerrarReserva = () => {
    setReservaModalOpen(false);
    setCanchaReservar(null);
    setReservaError(null);
  };

  const calcularHoras = (): number => {
    if (!reservaHoraInicio || !reservaHoraFin) return 1;
    const [hInicio, mInicio] = reservaHoraInicio.split(':').map(Number);
    const [hFin, mFin] = reservaHoraFin.split(':').map(Number);
    const minutos = hFin * 60 + mFin - (hInicio * 60 + mInicio);
    const horas = minutos / 60;
    return horas > 0 ? horas : 1;
  };

  const handleConfirmarReserva = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canchaReservar) return;
    setReservaError(null);

    if (!reservaFecha) {
      setReservaError('Por favor selecciona la fecha de la reserva.');
      return;
    }
    if (reservaHoraInicio >= reservaHoraFin) {
      setReservaError('La hora de inicio debe ser anterior a la hora de fin.');
      return;
    }

    const fechaInicioStr = `${reservaFecha}T${reservaHoraInicio}:00`;
    const fechaFinStr = `${reservaFecha}T${reservaHoraFin}:00`;

    setReservaSubmitting(true);
    try {
      const payload: ReservaCreateRequest = {
        canchaId: canchaReservar.id,
        fechaInicio: new Date(fechaInicioStr).toISOString(),
        fechaFin: new Date(fechaFinStr).toISOString(),
      };

      await canchasService.crearReserva(payload);
      handleCerrarReserva();
      setFeedback({
        type: 'success',
        text: `¡Reserva para la Cancha #${canchaReservar.numero} confirmada exitosamente!`,
      });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string; message?: string } }; message?: string };
      setReservaError(
        errorObj.response?.data?.error ||
        errorObj.response?.data?.message ||
        errorObj.message ||
        'Error al crear la reserva.'
      );
    } finally {
      setReservaSubmitting(false);
    }
  };

  const getBadgeStyle = (estado: EstadoCancha): React.CSSProperties => {
    switch (estado) {
      case 'Disponible':
        return {
          backgroundColor: '#dcfce7',
          color: '#15803d',
          border: '1px solid #bbf7d0',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
      case 'Mantenimiento':
        return {
          backgroundColor: '#fef9c3',
          color: '#854d0e',
          border: '1px solid #fde047',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
      case 'Fuera de servicio':
      default:
        return {
          backgroundColor: '#fee2e2',
          color: '#b91c1c',
          border: '1px solid #fecaca',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
    }
  };

  return (
    <div>
      {/* Banner de Feedback */}
      {feedback && (
        <div
          role={feedback.type === 'error' ? 'alert' : 'status'}
          style={{
            backgroundColor: feedback.type === 'error' ? 'var(--color-danger-bg)' : 'var(--color-success-bg)',
            border: `1px solid ${feedback.type === 'error' ? 'var(--color-danger-border)' : 'var(--color-success-border)'}`,
            color: feedback.type === 'error' ? 'var(--color-danger-text)' : 'var(--color-success-text)',
            padding: '0.75rem 1.25rem',
            borderRadius: 'var(--radius-md)',
            marginBottom: '1.25rem',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            {feedback.type === 'success' && <CheckCircle size={18} />}
            <span>{feedback.text}</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            {feedback.type === 'success' && (
              <button
                onClick={() => onNavigateToTab('reservas')}
                style={{
                  padding: '0.25rem 0.65rem',
                  fontSize: '0.8rem',
                  backgroundColor: '#16a34a',
                  color: '#ffffff',
                  border: 'none',
                  borderRadius: 'var(--radius-sm)',
                  cursor: 'pointer',
                  minHeight: 'auto',
                }}
              >
                Ver en Mis Reservas
              </button>
            )}
            <button
              onClick={() => setFeedback(null)}
              style={{
                background: 'transparent',
                border: 'none',
                cursor: 'pointer',
                fontWeight: 'bold',
                minHeight: 'auto',
                padding: '0 0.5rem',
                color: 'inherit',
              }}
            >
              ✕
            </button>
          </div>
        </div>
      )}

      {/* Barra de herramientas y filtros */}
      <div className="portal-toolbar">
        <div className="portal-filters">
          <div style={{ position: 'relative', minWidth: '220px' }}>
            <Search
              size={18}
              style={{
                position: 'absolute',
                left: '12px',
                top: '50%',
                transform: 'translateY(-50%)',
                color: 'var(--color-text-muted)',
              }}
            />
            <input
              type="text"
              placeholder="Buscar por #, sede o tipo..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{ paddingLeft: '2.4rem' }}
            />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Filter size={18} style={{ color: 'var(--color-text-muted)' }} />
            <select
              value={filterSede}
              onChange={(e) => setFilterSede(e.target.value)}
              style={{ minWidth: '160px' }}
            >
              <option value="">Todas las Sedes</option>
              {sedes.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.nombre}
                </option>
              ))}
            </select>

            <select
              value={filterEstado}
              onChange={(e) => setFilterEstado(e.target.value)}
              style={{ minWidth: '150px' }}
            >
              <option value="">Todos los Estados</option>
              {ESTADOS_VALIDOS.map((e) => (
                <option key={e} value={e}>
                  {e}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Criterio 6: Botón Crear visible solo para Administrador o Trabajador */}
        {canManage && (
          <button
            id="btn-crear-cancha"
            onClick={handleAbrirCrear}
            className="btn-primary"
            style={{ gap: '0.5rem' }}
          >
            <Plus size={18} />
            Nueva Cancha
          </button>
        )}
      </div>

      {/* Advertencia si no hay Sedes o Tipos para crear Canchas */}
      {canManage && (sedes.length === 0 || tiposCancha.length === 0) && (
        <div
          role="status"
          style={{
            backgroundColor: '#eff6ff',
            border: '1px solid #bfdbfe',
            color: '#1e40af',
            padding: '1rem 1.25rem',
            borderRadius: 'var(--radius-md)',
            marginBottom: '1.5rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: '1rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <AlertCircle size={22} />
            <div>
              <strong>Requisitos previos pendientes:</strong> Se necesitan al menos una Sede y un Tipo de Cancha para registrar canchas.
            </div>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {sedes.length === 0 && (
              <button
                onClick={() => onNavigateToTab('sedes')}
                style={{
                  fontSize: '0.85rem',
                  padding: '0.4rem 0.8rem',
                  minHeight: '36px',
                  backgroundColor: '#1e40af',
                  color: '#ffffff',
                }}
              >
                Ir a Sedes
              </button>
            )}
            {tiposCancha.length === 0 && (
              <button
                onClick={() => onNavigateToTab('tipos')}
                style={{
                  fontSize: '0.85rem',
                  padding: '0.4rem 0.8rem',
                  minHeight: '36px',
                  backgroundColor: '#1e40af',
                  color: '#ffffff',
                }}
              >
                Ir a Tipos
              </button>
            )}
          </div>
        </div>
      )}

      {/* 3 ESTADOS VISUALES DE RÚBRICA */}

      {/* ESTADO 1: CARGANDO */}
      {isLoading && (
        <div
          role="status"
          aria-live="polite"
          style={{
            backgroundColor: '#ffffff',
            border: '1px solid var(--color-border)',
            borderRadius: 'var(--radius-lg)',
            padding: '4rem 2rem',
            textAlign: 'center',
            boxShadow: 'var(--shadow-sm)',
          }}
        >
          <div
            style={{
              width: '44px',
              height: '44px',
              margin: '0 auto 1.25rem',
              border: '4px solid var(--color-border)',
              borderTopColor: 'var(--color-primary)',
              borderRadius: '50%',
              animation: 'spin 0.8s linear infinite',
            }}
          />
          <h4 style={{ margin: '0 0 0.5rem', fontSize: '1.1rem' }}>Cargando catálogo de canchas...</h4>
          <p style={{ margin: 0, fontSize: '0.9rem', color: 'var(--color-text-muted)' }}>
            Consultando la API de C# en tiempo real.
          </p>
        </div>
      )}

      {/* ESTADO 2: ERROR DE API */}
      {!isLoading && errorMessage && (
        <div
          role="alert"
          style={{
            backgroundColor: 'var(--color-danger-bg)',
            border: '1.5px solid var(--color-danger-border)',
            borderRadius: 'var(--radius-lg)',
            padding: '2.5rem 2rem',
            textAlign: 'center',
          }}
        >
          <div style={{ fontSize: '2.5rem', marginBottom: '0.5rem' }}>⚠️</div>
          <h4 style={{ color: 'var(--color-danger-text)', margin: '0 0 0.5rem', fontSize: '1.2rem' }}>
            Error al consultar Canchas
          </h4>
          <p style={{ color: 'var(--color-danger-text)', maxWidth: '600px', margin: '0 auto 1.5rem', fontSize: '0.95rem' }}>
            {errorMessage}
          </p>
          <button onClick={onRefresh} className="btn-primary" style={{ gap: '0.5rem' }}>
            <RefreshCw size={16} />
            Reintentar
          </button>
        </div>
      )}

      {/* ESTADO 3: LISTA VACÍA */}
      {!isLoading && !errorMessage && canchasFiltradas.length === 0 && (
        <div
          style={{
            backgroundColor: '#ffffff',
            border: '1px dashed var(--color-border-hover)',
            borderRadius: 'var(--radius-lg)',
            padding: '4rem 2rem',
            textAlign: 'center',
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '0.75rem' }}>🏟️</div>
          <h4 style={{ margin: '0 0 0.5rem', fontSize: '1.2rem' }}>
            {canchas.length === 0 ? 'No hay canchas registradas' : 'No hay canchas que coincidan con los filtros'}
          </h4>
          <p style={{ maxWidth: '460px', margin: '0 auto 1.5rem', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
            {canchas.length === 0
              ? canManage
                ? 'Comienza creando la primera cancha utilizando el botón "+ Nueva Cancha".'
                : 'El catálogo se encuentra vacío actualmente.'
              : 'Prueba cambiando o limpiando los términos de búsqueda o filtros.'}
          </p>
          {canManage && canchas.length === 0 && sedes.length > 0 && tiposCancha.length > 0 && (
            <button onClick={handleAbrirCrear} className="btn-primary">
              Crear Primera Cancha
            </button>
          )}
        </div>
      )}

      {/* TABLA DE CANCHAS */}
      {!isLoading && !errorMessage && canchasFiltradas.length > 0 && (
        <div className="portal-card">
          <div className="portal-table-wrapper">
            <table className="portal-table">
              <thead>
                <tr>
                  <th># Número</th>
                  <th>Sede</th>
                  <th>Tipo de Cancha</th>
                  <th>Precio / Hora</th>
                  <th>Estado</th>
                  <th style={{ textAlign: 'right' }}>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {canchasFiltradas.map((c) => (
                  <tr key={c.id}>
                    <td>
                      <strong style={{ fontSize: '1rem', color: 'var(--color-text-main)' }}>
                        Cancha #{c.numero}
                      </strong>
                    </td>
                    <td>
                      <div style={{ fontWeight: 600 }}>{c.sedeNombre}</div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                        ID Sede: {c.sedeId}
                      </div>
                    </td>
                    <td>
                      <div style={{ fontWeight: 500 }}>{c.tipoCanchaNombre}</div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                        ID Tipo: {c.tipoCanchaId}
                      </div>
                    </td>
                    <td style={{ fontWeight: 600, color: 'var(--color-primary)' }}>
                      ${c.precioPorHora.toLocaleString('es-CL')} / hr
                    </td>
                    <td>
                      <span style={getBadgeStyle(c.estado)}>{c.estado}</span>
                    </td>

                    {/* Acciones diferenciadas por rol */}
                    <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                      {/* Botón de Reservar disponible para canchas en estado 'Disponible' */}
                      {c.estado === 'Disponible' && (
                        <button
                          onClick={() => handleAbrirReserva(c)}
                          className="btn-primary"
                          style={{
                            padding: '0.35rem 0.85rem',
                            fontSize: '0.82rem',
                            minHeight: '34px',
                            marginRight: canManage ? '0.5rem' : '0',
                            gap: '0.35rem',
                            backgroundColor: '#16a34a',
                            borderColor: '#16a34a',
                          }}
                        >
                          <Calendar size={14} />
                          Reservar
                        </button>
                      )}

                      {/* Criterio 6: Botones de Editar y Eliminar solo para Administrador o Trabajador */}
                      {canManage && (
                        <>
                          <button
                            onClick={() => handleAbrirEditar(c)}
                            style={{
                              padding: '0.35rem 0.75rem',
                              fontSize: '0.8rem',
                              minHeight: '34px',
                              backgroundColor: 'var(--color-primary-light)',
                              color: 'var(--color-primary)',
                              border: '1px solid var(--color-border)',
                              borderRadius: 'var(--radius-sm)',
                              cursor: 'pointer',
                              marginRight: '0.5rem',
                              gap: '0.35rem',
                            }}
                          >
                            <Edit2 size={14} />
                            Editar
                          </button>
                          <button
                            onClick={() => handleEliminar(c)}
                            style={{
                              padding: '0.35rem 0.75rem',
                              fontSize: '0.8rem',
                              minHeight: '34px',
                              backgroundColor: 'var(--color-danger-bg)',
                              color: 'var(--color-danger)',
                              border: '1px solid var(--color-danger-border)',
                              borderRadius: 'var(--radius-sm)',
                              cursor: 'pointer',
                              gap: '0.35rem',
                            }}
                          >
                            <Trash2 size={14} />
                            Eliminar
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* MODAL RESERVAR CANCHA (FLUJO CLIENTE) */}
      {reservaModalOpen && canchaReservar && (
        <div className="portal-modal-backdrop" role="dialog" aria-modal="true">
          <div className="portal-modal-card">
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '1.25rem',
                borderBottom: '1px solid var(--color-border)',
                paddingBottom: '0.75rem',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Calendar size={20} style={{ color: 'var(--color-primary)' }} />
                <h3 style={{ margin: 0, fontSize: '1.2rem' }}>Agendar Reserva</h3>
              </div>
              <button
                type="button"
                onClick={handleCerrarReserva}
                style={{
                  background: 'transparent',
                  border: 'none',
                  fontSize: '1.3rem',
                  cursor: 'pointer',
                  color: 'var(--color-text-muted)',
                  padding: '0 0.5rem',
                  minHeight: 'auto',
                }}
              >
                ✕
              </button>
            </div>

            {reservaError && (
              <div role="alert" style={{ marginBottom: '1rem' }}>
                {reservaError}
              </div>
            )}

            <form onSubmit={handleConfirmarReserva}>
              {/* Cancha seleccionada (Lectura) */}
              <div
                style={{
                  backgroundColor: '#f8fafc',
                  border: '1px solid var(--color-border)',
                  borderRadius: 'var(--radius-md)',
                  padding: '1rem',
                  marginBottom: '1.25rem',
                }}
              >
                <div style={{ fontWeight: 700, fontSize: '1.05rem', color: 'var(--color-text-main)' }}>
                  Cancha #{canchaReservar.numero} - {canchaReservar.tipoCanchaNombre}
                </div>
                <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)', marginTop: '0.25rem' }}>
                  Sede: {canchaReservar.sedeNombre}
                </div>
                <div style={{ fontSize: '0.9rem', color: 'var(--color-primary)', fontWeight: 600, marginTop: '0.25rem' }}>
                  Tarifa: ${canchaReservar.precioPorHora.toLocaleString('es-CL')} / hora
                </div>
              </div>

              {/* Selector de Fecha */}
              <div style={{ marginBottom: '1.1rem' }}>
                <label htmlFor="reserva-fecha">Fecha del Partido *</label>
                <input
                  id="reserva-fecha"
                  type="date"
                  required
                  min={new Date().toISOString().split('T')[0]}
                  value={reservaFecha}
                  onChange={(e) => setReservaFecha(e.target.value)}
                />
              </div>

              {/* Selectores de Horario */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.25rem' }}>
                <div>
                  <label htmlFor="reserva-hora-inicio">Hora de Inicio *</label>
                  <select
                    id="reserva-hora-inicio"
                    value={reservaHoraInicio}
                    onChange={(e) => setReservaHoraInicio(e.target.value)}
                  >
                    {['08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00', '20:00', '21:00', '22:00'].map((h) => (
                      <option key={h} value={h}>
                        {h} hrs
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label htmlFor="reserva-hora-fin">Hora de Fin *</label>
                  <select
                    id="reserva-hora-fin"
                    value={reservaHoraFin}
                    onChange={(e) => setReservaHoraFin(e.target.value)}
                  >
                    {['09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00', '20:00', '21:00', '22:00', '23:00'].map((h) => (
                      <option key={h} value={h}>
                        {h} hrs
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Resumen del Precio a Pagar */}
              <div
                style={{
                  backgroundColor: '#f0fdf4',
                  border: '1px solid #bbf7d0',
                  borderRadius: 'var(--radius-md)',
                  padding: '1rem',
                  marginBottom: '1.5rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <div>
                  <div style={{ fontSize: '0.85rem', color: '#166534', fontWeight: 600 }}>
                    Duración: {calcularHoras()} hora(s)
                  </div>
                  <div style={{ fontSize: '0.75rem', color: '#15803d' }}>
                    Calculado automáticamente por tarifa
                  </div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontSize: '0.8rem', color: '#166534' }}>Total a Pagar</div>
                  <div style={{ fontSize: '1.3rem', fontWeight: 800, color: '#15803d' }}>
                    ${(calcularHoras() * canchaReservar.precioPorHora).toLocaleString('es-CL')}
                  </div>
                </div>
              </div>

              {/* Botones de acción */}
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                <button
                  type="button"
                  onClick={handleCerrarReserva}
                  disabled={reservaSubmitting}
                  style={{
                    backgroundColor: 'transparent',
                    border: '1.5px solid var(--color-border)',
                    color: 'var(--color-text-main)',
                  }}
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="btn-primary"
                  disabled={reservaSubmitting}
                  style={{
                    backgroundColor: '#16a34a',
                    borderColor: '#16a34a',
                    gap: '0.4rem',
                  }}
                >
                  <CheckCircle size={16} />
                  {reservaSubmitting ? 'Confirmando...' : 'Confirmar Reserva'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL CREAR / EDITAR CANCHA (ADMINISTRATIVO) */}
      {isModalOpen && canManage && (
        <div className="portal-modal-backdrop" role="dialog" aria-modal="true">
          <div className="portal-modal-card">
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '1.25rem',
                borderBottom: '1px solid var(--color-border)',
                paddingBottom: '0.75rem',
              }}
            >
              <h3 style={{ margin: 0, fontSize: '1.2rem' }}>
                {editingId !== null ? 'Editar Cancha' : 'Nueva Cancha'}
              </h3>
              <button
                type="button"
                onClick={handleCerrarModal}
                style={{
                  background: 'transparent',
                  border: 'none',
                  fontSize: '1.3rem',
                  cursor: 'pointer',
                  color: 'var(--color-text-muted)',
                  padding: '0 0.5rem',
                  minHeight: 'auto',
                }}
              >
                ✕
              </button>
            </div>

            {formError && (
              <div role="alert" style={{ marginBottom: '1rem' }}>
                {formError}
              </div>
            )}

            <form onSubmit={handleSubmit}>
              <div style={{ marginBottom: '1.1rem' }}>
                <label htmlFor="numero">Número de Cancha *</label>
                <input
                  id="numero"
                  type="number"
                  min="1"
                  required
                  placeholder="Ej: 1, 2, 3..."
                  value={formData.numero}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      numero: e.target.value === '' ? '' : parseInt(e.target.value, 10),
                    })
                  }
                />
              </div>

              <div style={{ marginBottom: '1.1rem' }}>
                <label htmlFor="sedeId">Sede *</label>
                <select
                  id="sedeId"
                  required
                  value={formData.sedeId}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      sedeId: e.target.value === '' ? '' : parseInt(e.target.value, 10),
                    })
                  }
                >
                  <option value="" disabled>
                    -- Seleccionar Sede --
                  </option>
                  {sedes.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.nombre} ({s.direccion})
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ marginBottom: '1.1rem' }}>
                <label htmlFor="tipoCanchaId">Tipo de Cancha *</label>
                <select
                  id="tipoCanchaId"
                  required
                  value={formData.tipoCanchaId}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      tipoCanchaId: e.target.value === '' ? '' : parseInt(e.target.value, 10),
                    })
                  }
                >
                  <option value="" disabled>
                    -- Seleccionar Tipo de Cancha --
                  </option>
                  {tiposCancha.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.nombre} (${t.precioPorHora.toLocaleString('es-CL')}/hr)
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ marginBottom: '1.5rem' }}>
                <label htmlFor="estado">Estado *</label>
                <select
                  id="estado"
                  required
                  value={formData.estado}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      estado: e.target.value as EstadoCancha,
                    })
                  }
                >
                  {ESTADOS_VALIDOS.map((est) => (
                    <option key={est} value={est}>
                      {est}
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                <button
                  type="button"
                  onClick={handleCerrarModal}
                  disabled={formSubmitting}
                  style={{
                    backgroundColor: 'transparent',
                    border: '1.5px solid var(--color-border)',
                    color: 'var(--color-text-main)',
                  }}
                >
                  Cancelar
                </button>
                <button type="submit" className="btn-primary" disabled={formSubmitting}>
                  {formSubmitting ? 'Guardando...' : editingId !== null ? 'Actualizar' : 'Crear Cancha'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
