import React from 'react';
import { Calendar, Trophy, ArrowRight, ShieldCheck, Clock, MapPin, Sparkles } from 'lucide-react';
import type { Cancha, Sede, Reserva } from '../types/cancha';
import type { Usuario } from '../types/auth';

interface InicioSectionProps {
  usuario: Usuario | null;
  canchas: Cancha[];
  sedes: Sede[];
  reservas: Reserva[];
  onNavigateTab: (tab: 'canchas' | 'reservas' | 'sedes' | 'tipos') => void;
}

export const InicioSection: React.FC<InicioSectionProps> = ({
  usuario,
  canchas,
  sedes,
  reservas,
  onNavigateTab,
}) => {
  const isCliente = usuario?.rol === 'Cliente';
  const canchasDisponibles = canchas.filter((c) => c.estado === 'Disponible').length;
  const misReservasCount = isCliente
    ? reservas.filter((r) => r.usuarioId === usuario?.id).length
    : reservas.length;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
      {/* Tarjeta de bienvenida personalizada */}
      <div
        style={{
          background: 'linear-gradient(135deg, #1e40af 0%, #1e293b 100%)',
          borderRadius: 'var(--radius-lg)',
          padding: '2.5rem 2rem',
          color: '#ffffff',
          boxShadow: 'var(--shadow-card)',
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        <div
          style={{
            position: 'absolute',
            top: '-20px',
            right: '-20px',
            opacity: 0.1,
            pointerEvents: 'none',
          }}
        >
          <Trophy size={200} />
        </div>

        <div style={{ position: 'relative', zIndex: 1, maxWidth: '650px' }}>
          <div
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.5rem',
              backgroundColor: 'rgba(255, 255, 255, 0.15)',
              padding: '0.35rem 0.85rem',
              borderRadius: '9999px',
              fontSize: '0.85rem',
              fontWeight: 600,
              marginBottom: '1rem',
              backdropFilter: 'blur(4px)',
            }}
          >
            <Sparkles size={16} />
            <span>Panel de Gestión y Reservas</span>
          </div>

          <h2 style={{ fontSize: '2rem', margin: '0 0 0.5rem', color: '#ffffff', fontWeight: 800 }}>
            ¡Bienvenido, {usuario?.nombre}!
          </h2>
          <p style={{ fontSize: '1.05rem', color: '#cbd5e1', margin: '0 0 1.75rem', lineHeight: 1.5 }}>
            {isCliente
              ? 'Explora las canchas disponibles de nuestro complejo deportivo, revisa horarios en tiempo real y agenda tu próximo partido en segundos.'
              : 'Administra integralmente el catálogo de canchas, tarifas, sedes operativas y supervisa la agenda global de reservas.'}
          </p>

          {/* Accesos rápidos según el rol */}
          <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
            <button
              onClick={() => onNavigateTab('canchas')}
              style={{
                backgroundColor: '#ffffff',
                color: '#1e40af',
                border: 'none',
                padding: '0.75rem 1.4rem',
                borderRadius: 'var(--radius-md)',
                fontWeight: 700,
                fontSize: '0.95rem',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.5rem',
                cursor: 'pointer',
                boxShadow: 'var(--shadow-md)',
              }}
            >
              <Trophy size={18} />
              {isCliente ? 'Reservar una Cancha' : 'Gestionar Canchas'}
              <ArrowRight size={16} />
            </button>

            <button
              onClick={() => onNavigateTab('reservas')}
              style={{
                backgroundColor: 'rgba(255, 255, 255, 0.15)',
                color: '#ffffff',
                border: '1px solid rgba(255, 255, 255, 0.3)',
                padding: '0.75rem 1.4rem',
                borderRadius: 'var(--radius-md)',
                fontWeight: 600,
                fontSize: '0.95rem',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.5rem',
                cursor: 'pointer',
                backdropFilter: 'blur(4px)',
              }}
            >
              <Calendar size={18} />
              {isCliente ? 'Ver mis Reservas' : 'Ver Todas las Reservas'}
            </button>
          </div>
        </div>
      </div>

      {/* Resumen Visual del Complejo */}
      <div>
        <h3 style={{ fontSize: '1.25rem', marginBottom: '1rem', color: 'var(--color-text-main)' }}>
          Resumen del Complejo Deportivo
        </h3>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
            gap: '1.25rem',
          }}
        >
          {/* Métrica 1: Canchas */}
          <div
            className="portal-card"
            style={{
              padding: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
            }}
          >
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '12px',
                backgroundColor: 'var(--color-primary-light)',
                color: 'var(--color-primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Trophy size={24} />
            </div>
            <div>
              <div style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--color-text-main)' }}>
                {canchasDisponibles} / {canchas.length}
              </div>
              <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                Canchas Disponibles
              </div>
            </div>
          </div>

          {/* Métrica 2: Sedes */}
          <div
            className="portal-card"
            style={{
              padding: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
            }}
          >
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '12px',
                backgroundColor: '#f0fdf4',
                color: '#16a34a',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <MapPin size={24} />
            </div>
            <div>
              <div style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--color-text-main)' }}>
                {sedes.length}
              </div>
              <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                Sedes Deportivas
              </div>
            </div>
          </div>

          {/* Métrica 3: Reservas */}
          <div
            className="portal-card"
            style={{
              padding: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
            }}
          >
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '12px',
                backgroundColor: '#eff6ff',
                color: '#2563eb',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Calendar size={24} />
            </div>
            <div>
              <div style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--color-text-main)' }}>
                {misReservasCount}
              </div>
              <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                {isCliente ? 'Mis Reservas Agendadas' : 'Total de Reservas'}
              </div>
            </div>
          </div>

          {/* Métrica 4: Rol y Estado */}
          <div
            className="portal-card"
            style={{
              padding: '1.5rem',
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
            }}
          >
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '12px',
                backgroundColor: '#faf5ff',
                color: '#9333ea',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <ShieldCheck size={24} />
            </div>
            <div>
              <div style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--color-text-main)' }}>
                {usuario?.rol}
              </div>
              <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                {usuario?.email}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Tarjeta de Información y Horarios */}
      <div
        className="portal-card"
        style={{
          padding: '1.75rem',
          display: 'flex',
          flexDirection: 'column',
          gap: '1rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <Clock size={20} style={{ color: 'var(--color-primary)' }} />
          <h4 style={{ margin: 0, fontSize: '1.1rem' }}>Horarios de Atención y Reservas</h4>
        </div>
        <p style={{ margin: 0, color: 'var(--color-text-muted)', fontSize: '0.92rem', lineHeight: 1.6 }}>
          Nuestras canchas atienden de <strong>Lunes a Domingo de 08:00 a 23:00 hrs</strong>. Puedes
          reservar bloques horarios de 1 o más horas para fútbol 5, fútbol 7 o pádel con iluminación LED
          y estacionamiento privado garantizado.
        </p>
      </div>
    </div>
  );
};
