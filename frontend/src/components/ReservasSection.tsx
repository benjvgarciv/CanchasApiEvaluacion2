import React, { useState } from 'react';
import type { Reserva, EstadoReserva } from '../types/cancha';
import { canchasService } from '../api/canchasService';
import { User, Clock, RefreshCw, Plus, Calendar, CheckCircle2, UserCheck, XCircle } from 'lucide-react';

interface ReservasSectionProps {
  reservas: Reserva[];
  isLoading: boolean;
  errorMessage: string | null;
  isCliente: boolean;
  canManage: boolean;
  currentUserId?: number;
  currentUserNombre?: string;
  onRefresh: () => Promise<void>;
  onNavigateToCanchas: () => void;
}

export const ReservasSection: React.FC<ReservasSectionProps> = ({
  reservas,
  isLoading,
  errorMessage,
  isCliente,
  canManage,
  currentUserId,
  currentUserNombre,
  onRefresh,
  onNavigateToCanchas,
}) => {
  const [filterEstado, setFilterEstado] = useState<string>('');
  const [localReservas, setLocalReservas] = useState<Reserva[]>(reservas);
  const [loadingAccionId, setLoadingAccionId] = useState<number | null>(null);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // Sincronizar estado local cuando las props cambien
  React.useEffect(() => {
    setLocalReservas(reservas);
  }, [reservas]);

  const reservasFiltradas = localReservas.filter((r) =>
    filterEstado ? r.estado === filterEstado : true
  );

  const getBadgeStyle = (estado: EstadoReserva): React.CSSProperties => {
    switch (estado) {
      case 'Confirmada':
        return {
          backgroundColor: '#dcfce7',
          color: '#15803d',
          border: '1px solid #bbf7d0',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
      case 'Pendiente':
        return {
          backgroundColor: '#fef9c3',
          color: '#854d0e',
          border: '1px solid #fde047',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
      case 'Cancelada':
        return {
          backgroundColor: '#fee2e2',
          color: '#b91c1c',
          border: '1px solid #fecaca',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
      case 'Completada':
      default:
        return {
          backgroundColor: '#eff6ff',
          color: '#1d4ed8',
          border: '1px solid #bfdbfe',
          padding: '0.2rem 0.65rem',
          borderRadius: '9999px',
          fontSize: '0.75rem',
          fontWeight: 600,
        };
    }
  };

  const formatearFecha = (fechaStr: string) => {
    try {
      const fecha = new Date(fechaStr);
      return fecha.toLocaleString('es-CL', {
        dateStyle: 'medium',
        timeStyle: 'short',
      });
    } catch {
      return fechaStr;
    }
  };

  // 2. Acciones de Gestión Rápida con actualización visual en tiempo real
  const handleAprobarConfirmar = async (reserva: Reserva) => {
    setLoadingAccionId(reserva.id);
    setFeedback(null);
    try {
      const updated = await canchasService.actualizarReserva(reserva.id, {
        estado: 'Confirmada',
        trabajadorId: currentUserId ?? undefined,
      });

      // Actualización visual reactiva inmediata con la respuesta real de la API
      setLocalReservas((prev) =>
        prev.map((r) =>
          r.id === reserva.id
            ? {
                ...r,
                estado: updated.estado,
                trabajadorId: updated.trabajadorId,
                trabajadorNombre: updated.trabajadorNombre || currentUserNombre || 'Trabajador',
              }
            : r
        )
      );

      setFeedback({
        type: 'success',
        text: `Reserva #${reserva.id} para Cancha #${reserva.canchaNumero} aprobada y confirmada.`,
      });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'No se pudo confirmar la reserva.',
      });
    } finally {
      setLoadingAccionId(null);
    }
  };

  const handleAsignarme = async (reserva: Reserva) => {
    setLoadingAccionId(reserva.id);
    setFeedback(null);
    try {
      // Enviar el ID del usuario actual para asignar la atención en la base de datos
      const updated = await canchasService.actualizarReserva(reserva.id, {
        estado: reserva.estado,
        trabajadorId: currentUserId ?? undefined,
      });

      // Actualizar visualmente la fila de atención con el nombre devuelto o logueado
      const nombreAsignado = updated.trabajadorNombre || currentUserNombre || 'Trabajador';
      setLocalReservas((prev) =>
        prev.map((r) =>
          r.id === reserva.id
            ? {
                ...r,
                trabajadorId: updated.trabajadorId,
                trabajadorNombre: nombreAsignado,
              }
            : r
        )
      );

      setFeedback({
        type: 'success',
        text: `Te has asignado exitosamente a la atención de la Reserva #${reserva.id} (${nombreAsignado}).`,
      });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'No se pudo registrar la asignación.',
      });
    } finally {
      setLoadingAccionId(null);
    }
  };

  const handleCancelar = async (reserva: Reserva) => {
    const confirmacion = window.confirm(
      `¿Estás seguro de cancelar la Reserva #${reserva.id} de ${reserva.usuarioNombre}?`
    );
    if (!confirmacion) return;

    setLoadingAccionId(reserva.id);
    setFeedback(null);
    try {
      await canchasService.actualizarReserva(reserva.id, {
        estado: 'Cancelada',
      });

      // Actualización visual reactiva inmediata
      setLocalReservas((prev) =>
        prev.map((r) =>
          r.id === reserva.id
            ? {
                ...r,
                estado: 'Cancelada' as EstadoReserva,
              }
            : r
        )
      );

      setFeedback({
        type: 'success',
        text: `La Reserva #${reserva.id} ha sido cancelada.`,
      });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'No se pudo cancelar la reserva.',
      });
    } finally {
      setLoadingAccionId(null);
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
          <span>{feedback.text}</span>
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
      )}

      {/* Encabezado y Filtros */}
      <div className="portal-toolbar">
        <div>
          <h3 style={{ margin: 0, fontSize: '1.25rem' }}>
            {isCliente ? 'Mis Reservas' : 'Agenda Global de Reservas'}
          </h3>
          <p style={{ margin: 0, fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>
            {isCliente
              ? 'Consulta el estado de tus partidos agendados en nuestro complejo.'
              : 'Supervisión en tiempo real, confirmación de horarios y asignación de atención (ReservaApi.cs).'}
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <select
            value={filterEstado}
            onChange={(e) => setFilterEstado(e.target.value)}
            style={{ minWidth: '160px' }}
          >
            <option value="">Todos los Estados</option>
            <option value="Pendiente">Pendiente</option>
            <option value="Confirmada">Confirmada</option>
            <option value="Completada">Completada</option>
            <option value="Cancelada">Cancelada</option>
          </select>

          <button
            onClick={onRefresh}
            className="btn"
            style={{
              padding: '0.5rem 0.85rem',
              minHeight: '40px',
              backgroundColor: 'var(--color-surface)',
              border: '1px solid var(--color-border)',
              gap: '0.4rem',
            }}
          >
            <RefreshCw size={16} />
            Actualizar
          </button>

          {isCliente && (
            <button
              onClick={onNavigateToCanchas}
              className="btn-primary"
              style={{ gap: '0.4rem' }}
            >
              <Plus size={18} />
              Agendar Nueva Reserva
            </button>
          )}
        </div>
      </div>

      {/* ESTADO 1: CARGANDO */}
      {isLoading && (
        <div
          role="status"
          style={{
            backgroundColor: '#ffffff',
            border: '1px solid var(--color-border)',
            borderRadius: 'var(--radius-lg)',
            padding: '3rem 2rem',
            textAlign: 'center',
          }}
        >
          <div
            style={{
              width: '40px',
              height: '40px',
              margin: '0 auto 1rem',
              border: '4px solid var(--color-border)',
              borderTopColor: 'var(--color-primary)',
              borderRadius: '50%',
              animation: 'spin 0.8s linear infinite',
            }}
          />
          <p style={{ margin: 0, color: 'var(--color-text-muted)' }}>
            {isCliente ? 'Cargando tus reservas...' : 'Cargando reservas del sistema...'}
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
            padding: '2rem',
            textAlign: 'center',
          }}
        >
          <p style={{ color: 'var(--color-danger-text)', margin: '0 0 1rem' }}>{errorMessage}</p>
          <button onClick={onRefresh} className="btn-primary" style={{ gap: '0.5rem' }}>
            <RefreshCw size={16} />
            Reintentar
          </button>
        </div>
      )}

      {/* ESTADO 3: LISTA VACÍA */}
      {!isLoading && !errorMessage && reservasFiltradas.length === 0 && (
        <div
          style={{
            backgroundColor: '#ffffff',
            border: '1px dashed var(--color-border-hover)',
            borderRadius: 'var(--radius-lg)',
            padding: '3.5rem 2rem',
            textAlign: 'center',
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '0.75rem' }}>📅</div>
          <h4 style={{ margin: '0 0 0.5rem', fontSize: '1.2rem' }}>
            {isCliente
              ? 'No tienes reservas agendadas'
              : 'No hay reservas registradas en el sistema'}
          </h4>
          <p style={{ maxWidth: '460px', margin: '0 auto 1.5rem', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
            {isCliente
              ? 'Aún no has reservado ninguna cancha para jugar. ¡Explora nuestro catálogo y agenda tu horario preferido!'
              : 'Aún no se han registrado turnos o no coinciden con el filtro seleccionado.'}
          </p>

          {isCliente && (
            <button
              onClick={onNavigateToCanchas}
              className="btn-primary"
              style={{
                gap: '0.5rem',
                backgroundColor: '#16a34a',
                borderColor: '#16a34a',
              }}
            >
              <Calendar size={18} />
              Agendar Nueva Reserva
            </button>
          )}
        </div>
      )}

      {/* TABLA DE RESERVAS */}
      {!isLoading && !errorMessage && reservasFiltradas.length > 0 && (
        <div className="portal-card">
          <div className="portal-table-wrapper">
            <table className="portal-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Cancha</th>
                  {!isCliente && <th>Cliente</th>}
                  <th>Horario (Inicio / Fin)</th>
                  <th>Atención</th>
                  <th>Estado</th>
                  {/* 1. Columna ACCIONES en vista de Trabajador/Administrador */}
                  {canManage && <th style={{ textAlign: 'center' }}>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {reservasFiltradas.map((r) => {
                  const isOperating = loadingAccionId === r.id;
                  const isPendiente = r.estado === 'Pendiente';

                  return (
                    <tr key={r.id}>
                      <td>
                        <span style={{ fontWeight: 600, color: 'var(--color-text-muted)' }}>#{r.id}</span>
                      </td>
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                          <span style={{ fontSize: '1.2rem' }}>⚽</span>
                          <strong>Cancha #{r.canchaNumero}</strong>
                        </div>
                      </td>
                      {!isCliente && (
                        <td>
                          <div style={{ fontWeight: 600 }}>{r.usuarioNombre}</div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                            {r.usuarioEmail}
                          </div>
                        </td>
                      )}
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.85rem' }}>
                          <Clock size={14} style={{ color: 'var(--color-primary)' }} />
                          <span>{formatearFecha(r.fechaInicio)}</span>
                        </div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginLeft: '1.25rem' }}>
                          Hasta: {formatearFecha(r.fechaFin)}
                        </div>
                      </td>
                      <td>
                        {r.trabajadorNombre ? (
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                            <User size={14} style={{ color: '#16a34a' }} />
                            <strong style={{ color: '#15803d', fontSize: '0.88rem' }}>
                              {r.trabajadorNombre}
                            </strong>
                          </div>
                        ) : (
                          <span style={{ color: 'var(--color-text-muted)', fontSize: '0.8rem' }}>
                            Sin asignar
                          </span>
                        )}
                      </td>
                      <td>
                        <span style={getBadgeStyle(r.estado)}>{r.estado}</span>
                      </td>

                      {/* 2. Botones de Gestión Rápida */}
                      {canManage && (
                        <td style={{ textAlign: 'center', whiteSpace: 'nowrap' }}>
                          {isOperating ? (
                            <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>
                              Actualizando...
                            </span>
                          ) : (
                            <div style={{ display: 'inline-flex', gap: '0.4rem', alignItems: 'center' }}>
                              {/* 🟢 Botón Aprobar / Confirmar */}
                              {isPendiente && (
                                <button
                                  onClick={() => handleAprobarConfirmar(r)}
                                  title="Aprobar y Confirmar Reserva"
                                  style={{
                                    padding: '0.35rem 0.65rem',
                                    fontSize: '0.8rem',
                                    minHeight: '32px',
                                    backgroundColor: '#dcfce7',
                                    color: '#15803d',
                                    border: '1px solid #bbf7d0',
                                    borderRadius: 'var(--radius-sm)',
                                    cursor: 'pointer',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    gap: '0.3rem',
                                    fontWeight: 600,
                                  }}
                                >
                                  <CheckCircle2 size={14} />
                                  Confirmar
                                </button>
                              )}

                              {/* 👤 Botón Asignarme (si no está asignado o para cambiar la atención) */}
                              {r.estado !== 'Cancelada' && (
                                <button
                                  onClick={() => handleAsignarme(r)}
                                  title="Asignarme en la Atención"
                                  style={{
                                    padding: '0.35rem 0.65rem',
                                    fontSize: '0.8rem',
                                    minHeight: '32px',
                                    backgroundColor: '#eff6ff',
                                    color: '#1d4ed8',
                                    border: '1px solid #bfdbfe',
                                    borderRadius: 'var(--radius-sm)',
                                    cursor: 'pointer',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    gap: '0.3rem',
                                    fontWeight: 600,
                                  }}
                                >
                                  <UserCheck size={14} />
                                  Asignarme
                                </button>
                              )}

                              {/* 🔴 Botón Cancelar */}
                              {r.estado !== 'Cancelada' && (
                                <button
                                  onClick={() => handleCancelar(r)}
                                  title="Cancelar Reserva"
                                  style={{
                                    padding: '0.35rem 0.65rem',
                                    fontSize: '0.8rem',
                                    minHeight: '32px',
                                    backgroundColor: '#fee2e2',
                                    color: '#b91c1c',
                                    border: '1px solid #fecaca',
                                    borderRadius: 'var(--radius-sm)',
                                    cursor: 'pointer',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    gap: '0.3rem',
                                    fontWeight: 600,
                                  }}
                                >
                                  <XCircle size={14} />
                                  Cancelar
                                </button>
                              )}
                            </div>
                          )}
                        </td>
                      )}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
