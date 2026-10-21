import React, { useState } from 'react';
import type { TipoCancha, TipoCanchaRequest } from '../types/cancha';
import { canchasService } from '../api/canchasService';
import { Plus, Edit2, Trash2, Database, Tag, RefreshCw } from 'lucide-react';

interface TiposCanchaSectionProps {
  tiposCancha: TipoCancha[];
  isLoading: boolean;
  errorMessage: string | null;
  canManage: boolean;
  onRefresh: () => Promise<void>;
}

const INITIAL_FORM: TipoCanchaRequest = {
  nombre: '',
  descripcion: '',
  precioPorHora: 15000,
};

export const TiposCanchaSection: React.FC<TiposCanchaSectionProps> = ({
  tiposCancha,
  isLoading,
  errorMessage,
  canManage,
  onRefresh,
}) => {
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<TipoCanchaRequest>(INITIAL_FORM);
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

  const handleAbrirEditar = (tipo: TipoCancha) => {
    setFormError(null);
    setEditingId(tipo.id);
    setFormData({
      nombre: tipo.nombre,
      descripcion: tipo.descripcion || '',
      precioPorHora: tipo.precioPorHora,
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

    if (!formData.nombre.trim()) {
      setFormError('El nombre del tipo de cancha es obligatorio.');
      return;
    }
    if (formData.precioPorHora <= 0) {
      setFormError('El precio por hora debe ser mayor a 0.');
      return;
    }

    setFormSubmitting(true);
    try {
      const payload: TipoCanchaRequest = {
        nombre: formData.nombre.trim(),
        descripcion: formData.descripcion?.trim() || null,
        precioPorHora: Number(formData.precioPorHora),
      };

      if (editingId !== null) {
        await canchasService.actualizarTipoCancha(editingId, payload);
        setFeedback({ type: 'success', text: `Tipo "${payload.nombre}" actualizado con éxito.` });
      } else {
        await canchasService.crearTipoCancha(payload);
        setFeedback({ type: 'success', text: `Tipo "${payload.nombre}" creado con éxito.` });
      }

      handleCerrarModal();
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFormError(
        errorObj.response?.data?.error || errorObj.message || 'Error al guardar el tipo de cancha.'
      );
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleEliminar = async (tipo: TipoCancha) => {
    const confirmacion = window.confirm(
      `¿Deseas eliminar el Tipo de Cancha "${tipo.nombre}"?`
    );
    if (!confirmacion) return;

    setFeedback(null);
    try {
      await canchasService.eliminarTipoCancha(tipo.id);
      setFeedback({ type: 'success', text: `Tipo "${tipo.nombre}" eliminado exitosamente.` });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'No se pudo eliminar el tipo (puede estar en uso por canchas).',
      });
    }
  };

  // Requisito 2: Botón de Carga de Datos Iniciales (Seed Data)
  const handleSeedData = async () => {
    setIsSeeding(true);
    setFeedback(null);
    try {
      const tiposEjemplo: TipoCanchaRequest[] = [
        {
          nombre: 'Fútbol 5 Sintético',
          descripcion: 'Cancha de césped sintético de alta densidad para 10 jugadores.',
          precioPorHora: 22000,
        },
        {
          nombre: 'Fútbol 7 Techada',
          descripcion: 'Cancha con iluminación LED y techo cubierto para lluvia.',
          precioPorHora: 30000,
        },
        {
          nombre: 'Pádel Cristal Panorámico',
          descripcion: 'Cancha de pádel reglamentaria con vidrio templado de 12mm.',
          precioPorHora: 18000,
        },
      ];

      for (const t of tiposEjemplo) {
        await canchasService.crearTipoCancha(t);
      }

      setFeedback({
        type: 'success',
        text: '¡Se han creado 3 tipos de cancha de ejemplo exitosamente en el servidor!',
      });
      await onRefresh();
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { error?: string } }; message?: string };
      setFeedback({
        type: 'error',
        text: errorObj.response?.data?.error || 'Error al poblar tipos de cancha iniciales.',
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

      {/* Barra de Encabezado y Acción */}
      <div className="portal-toolbar">
        <div>
          <h3 style={{ margin: 0, fontSize: '1.25rem' }}>Tipos de Cancha</h3>
          <p style={{ margin: 0, fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>
            Configuración de disciplinas, superficies y precios por hora (TipoCanchaApi.cs).
          </p>
        </div>

        {canManage && (
          <button onClick={handleAbrirCrear} className="btn-primary" style={{ gap: '0.5rem' }}>
            <Plus size={18} />
            Nuevo Tipo
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
          <p style={{ margin: 0, color: 'var(--color-text-muted)' }}>Cargando tipos de cancha...</p>
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
      {!isLoading && !errorMessage && tiposCancha.length === 0 && (
        <div
          style={{
            backgroundColor: '#ffffff',
            border: '1px dashed var(--color-border-hover)',
            borderRadius: 'var(--radius-lg)',
            padding: '3.5rem 2rem',
            textAlign: 'center',
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '0.75rem' }}>🏷️</div>
          <h4 style={{ margin: '0 0 0.5rem', fontSize: '1.2rem' }}>No hay tipos de cancha registrados</h4>
          <p style={{ maxWidth: '450px', margin: '0 auto 1.5rem', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
            No se han definido categorías ni tarifas por hora. Puedes sembrar 3 tipos de ejemplo inmediatamente.
          </p>

          {canManage && (
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
                {isSeeding ? 'Poblando tipos...' : 'Cargar Datos Iniciales de Ejemplo'}
              </button>
              <button onClick={handleAbrirCrear} className="btn">
                Crear Tipo Manual
              </button>
            </div>
          )}
        </div>
      )}

      {/* LISTA / TABLA DE TIPOS */}
      {!isLoading && !errorMessage && tiposCancha.length > 0 && (
        <div className="portal-card">
          <div className="portal-table-wrapper">
            <table className="portal-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Nombre</th>
                  <th>Descripción</th>
                  <th>Precio por Hora</th>
                  {canManage && <th style={{ textAlign: 'right' }}>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {tiposCancha.map((t) => (
                  <tr key={t.id}>
                    <td>
                      <span style={{ fontWeight: 600, color: 'var(--color-text-muted)' }}>#{t.id}</span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                        <Tag size={15} style={{ color: 'var(--color-primary)' }} />
                        <strong>{t.nombre}</strong>
                      </div>
                    </td>
                    <td>
                      <span style={{ color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>
                        {t.descripcion || 'Sin descripción'}
                      </span>
                    </td>
                    <td style={{ fontWeight: 700, color: 'var(--color-primary)' }}>
                      ${t.precioPorHora.toLocaleString('es-CL')} / hr
                    </td>
                    {canManage && (
                      <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                        <button
                          onClick={() => handleAbrirEditar(t)}
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
                          onClick={() => handleEliminar(t)}
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

      {/* MODAL CREAR / EDITAR TIPO DE CANCHA */}
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
                {editingId !== null ? 'Editar Tipo de Cancha' : 'Nuevo Tipo de Cancha'}
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
                <label htmlFor="tipo-nombre">Nombre del Tipo *</label>
                <input
                  id="tipo-nombre"
                  type="text"
                  required
                  placeholder="Ej: Fútbol 5 Sintético"
                  value={formData.nombre}
                  onChange={(e) => setFormData({ ...formData, nombre: e.target.value })}
                />
              </div>

              <div style={{ marginBottom: '1.1rem' }}>
                <label htmlFor="tipo-precio">Precio por Hora ($ CLP) *</label>
                <input
                  id="tipo-precio"
                  type="number"
                  min="1"
                  required
                  placeholder="Ej: 20000"
                  value={formData.precioPorHora}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      precioPorHora: e.target.value === '' ? 0 : parseFloat(e.target.value),
                    })
                  }
                />
              </div>

              <div style={{ marginBottom: '1.5rem' }}>
                <label htmlFor="tipo-desc">Descripción (opcional)</label>
                <textarea
                  id="tipo-desc"
                  rows={3}
                  placeholder="Detalles sobre superficie, equipamiento o medidas..."
                  value={formData.descripcion || ''}
                  onChange={(e) => setFormData({ ...formData, descripcion: e.target.value })}
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
                  {formSubmitting ? 'Guardando...' : editingId !== null ? 'Actualizar Tipo' : 'Crear Tipo'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
