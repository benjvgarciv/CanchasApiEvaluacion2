import React from 'react';
import { Link } from 'react-router-dom';

export const Unauthorized: React.FC = () => {
  return (
    <main style={{ maxWidth: '600px', margin: '4rem auto', padding: '1.5rem', textAlign: 'center' }}>
      <h1 style={{ color: '#d9534f' }}>403 - Acceso Denegado</h1>
      <p style={{ margin: '1rem 0' }}>
        No tienes los permisos o el rol necesario para acceder a este recurso.
      </p>
      <div style={{ marginTop: '2rem' }}>
        <Link
          to="/dashboard"
          style={{
            display: 'inline-block',
            padding: '0.75rem 1.5rem',
            backgroundColor: '#0275d8',
            color: '#fff',
            textDecoration: 'none',
            borderRadius: '4px',
            minHeight: '44px',
            lineHeight: '28px',
          }}
        >
          Volver al Inicio
        </Link>
      </div>
    </main>
  );
};
