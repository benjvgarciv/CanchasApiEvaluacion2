import React, { useState } from 'react';
import type { Sede, SedeRequest } from '../types/cancha';
import { canchasService } from '../api/canchasService';
import { Plus, Edit2, Trash2, Database, MapPin, Phone, RefreshCw } from 'lucide-react';

interface SedesSectionProps {
  sedes: Sede[];
  isLoading: boolean;
  errorMessage: string | null;
  canManage: boolean;
  isAdmin?: boolean;
  onRefresh: () => Promise<void>;
}

const INITIAL_FORM: SedeRequest = {
  nombre: '',
  direccion: '',
  telefono: '',
};

export const SedesSection: React.FC<SedesSectionProps> = ({
  sedes,
  isLoading,
  errorMessage,
  canManage: _canManage,
  isAdmin = false,
  onRefresh,
}) => {
  const puedeModificar = isAdmin;
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<SedeRequest>(INITIAL_FORM);
  const [formSubmitting, setFormSubmitting] = useState<boolean>(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [isSeeding, setIsSeeding] = useState<boolean>(false);

  const handleAbrirCrear = () => {
    setFormError(null);
    setEditingId(null);
    setFormData(INITIAL_FORM);
    setIsModalOpen(true);
  };

  const handleAbrirEditar = (sede: Sede) => {
    setFormError(null);
    setEditingId(sede.id);
    setFormData({
      nombre: sede.nombre,
      direccion: sede.direccion,
      telefono: sede.telefono || '',
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

    if (!formData.nombre.trim() || !formData.direccion.trim()) {
      setFormError('Nombre y Dirección son requeridos por la API.');
      return;
    }

    setFormSubmitting(true);
    try {
      const payload: SedeRequest = {
        nombre: formData.nombre.trim(),
        direccion: formData.direccion.trim(),
        telefono: formData.telefono?.trim() || null,
      };

      if (editingId !== null) {
        await canchasService.actualizarSede(editingId, payload);
        setFeedback({ type: 'success', text: `Sede "${payload.nombre}" actualizada exitosamente.` });
      } else {
        await canchasService.crearSede(payload);
        setFeedback({ type: 'success', text: `Sede "${payload.nombre}" creada exitosamente.` });
      }

      handleCerrarModal();
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFormError(errorObj.response?.data?.error || errorObj.message || 'Error al guardar la sede.');
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleEliminar = async (sede: Sede) => {
    const confirmacion = window.confirm(`¿Estás seguro de eliminar la Sede "${sede.nombre}"?`);
    if (!confirmacion) return;

    setFeedback(null);
    try {
      await canchasService.eliminarSede(sede.id);
      setFeedback({ type: 'success', text: `Sede "${sede.nombre}" eliminada exitosamente.` });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'No se pudo eliminar la sede (puede tener canchas asociadas).',
      });
    }
  };

  // Requisito 2: Botón de Carga de Datos Iniciales (Seed Data)
  const handleSeedData = async () => {
    setIsSeeding(true);
    setFeedback(null);
    try {
      const sedesEjemplo: SedeRequest[] = [
        {
          nombre: 'Sede Central Providencia',
          direccion: 'Av. Providencia 1234, Santiago',
          telefono: '+56 2 2233 4455',
        },
        {
          nombre: 'Sede Las Condes Deportivo',
          direccion: 'Av. Apoquindo 4500, Santiago',
          telefono: '+56 2 2299 8877',
        },
      ];

      for (const s of sedesEjemplo) {
        await canchasService.crearSede(s);
      }

      setFeedback({
        type: 'success',
        text: '¡Se han creado 2 sedes de ejemplo en la base de datos correctamente!',
      });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'Error al sembrar datos de sedes de ejemplo.',
      });
    } finally {
      setIsSeeding(false);
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

      {/* Encabezado y Acción */}
      <div className="portal-toolbar">
        <div>
          <h3 style={{ margin: 0, fontSize: '1.25rem' }}>Gestión de Sedes</h3>
          <p style={{ margin: 0, fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>
            Configuración de complejos y sucursales (SedeApi.cs).
          </p>
        </div>

        {puedeModificar && (
          <button onClick={handleAbrirCrear} className="btn-primary" style={{ gap: '0.5rem' }}>
            <Plus size={18} />
            Nueva Sede
          </button>
        )}
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
          <p style={{ margin: 0, color: 'var(--color-text-muted)' }}>Cargando sedes...</p>
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

      {/* ESTADO 3: LISTA VACÍA + BOTÓN DE SEED DATA (Requisito 2) */}
      {!isLoading && !errorMessage && sedes.length === 0 && (
        <div
          style={{
            backgroundColor: '#ffffff',
            border: '1px dashed var(--color-border-hover)',
            borderRadius: 'var(--radius-lg)',
            padding: '3.5rem 2rem',
            textAlign: 'center',
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '0.75rem' }}>🏢</div>
          <h4 style={{ margin: '0 0 0.5rem', fontSize: '1.2rem' }}>No hay sedes registradas</h4>
          <p style={{ maxWidth: '450px', margin: '0 auto 1.5rem', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
            La base de datos no contiene sedes aún. Puedes crearlas manualmente o utilizar el cargador inicial para poblarla automáticamente.
          </p>

          {puedeModificar && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
              <button
                onClick={handleSeedData}
                disabled={isSeeding}
                className="btn-primary"
                style={{
                  backgroundColor: '#059669',
                  borderColor: '#059669',
                  gap: '0.5rem',
                }}
              >
                <Database size={18} />
                {isSeeding ? 'Poblando sedes...' : 'Cargar Datos Iniciales de Ejemplo'}
              </button>
              <button onClick={handleAbrirCrear} className="btn">
                Crear Sede Manual
              </button>
            </div>
          )}
        </div>
      )}

      {/* LISTADO / TABLA DE SEDES */}
      {!isLoading && !errorMessage && sedes.length > 0 && (
        <div className="portal-card">
          <div className="portal-table-wrapper">
            <table className="portal-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nombre</th>
                  <th>Dirección</th>
                  <th>Teléfono</th>
                  {puedeModificar && <th style={{ textAlign: 'right' }}>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {sedes.map((s) => (
                  <tr key={s.id}>
                    <td>
                      <span style={{ fontWeight: 600, color: 'var(--color-text-muted)' }}>#{s.id}</span>
                    </td>
                    <td>
                      <strong>{s.nombre}</strong>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                        <MapPin size={15} style={{ color: 'var(--color-text-muted)' }} />
                        <span>{s.direccion}</span>
                      </div>
                    </td>
                    <td>
                      {s.telefono ? (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                          <Phone size={14} style={{ color: 'var(--color-text-muted)' }} />
                          <span>{s.telefono}</span>
                        </div>
                      ) : (
                        <span style={{ color: 'var(--color-text-muted)', fontSize: '0.8rem' }}>Sin teléfono</span>
                      )}
                    </td>
                    {puedeModificar && (
                      <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                        <button
                          onClick={() => handleAbrirEditar(s)}
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
                          onClick={() => handleEliminar(s)}
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
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* MODAL CREAR / EDITAR SEDE */}
      {isModalOpen && puedeModificar && (
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
                {editingId !== null ? 'Editar Sede' : 'Nueva Sede'}
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
                <label htmlFor="sede-nombre">Nombre de la Sede *</label>
                <input
                  id="sede-nombre"
                  type="text"
                  required
                  placeholder="Ej: Sede Central"
                  value={formData.nombre}
                  onChange={(e) => setFormData({ ...formData, nombre: e.target.value })}
                />
              </div>

              <div style={{ marginBottom: '1.1rem' }}>
                <label htmlFor="sede-direccion">Dirección *</label>
                <input
                  id="sede-direccion"
                  type="text"
                  required
                  placeholder="Ej: Av. Deportes 101"
                  value={formData.direccion}
                  onChange={(e) => setFormData({ ...formData, direccion: e.target.value })}
                />
              </div>

              <div style={{ marginBottom: '1.5rem' }}>
                <label htmlFor="sede-telefono">Teléfono (opcional)</label>
                <input
                  id="sede-telefono"
                  type="text"
                  placeholder="Ej: +56 9 1234 5678"
                  value={formData.telefono || ''}
                  onChange={(e) => setFormData({ ...formData, telefono: e.target.value })}
                />
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
                  {formSubmitting ? 'Guardando...' : editingId !== null ? 'Actualizar Sede' : 'Crear Sede'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
