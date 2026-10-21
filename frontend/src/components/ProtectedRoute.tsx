import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { UserRole } from '../types/auth';

interface ProtectedRouteProps {
  allowedRoles?: UserRole[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowedRoles }) => {
  const { usuario, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <p>Cargando sesión segura...</p>
      </div>
    );
  }

  if (!isAuthenticated || !usuario) {
    return <Navigate to="/login" replace />;
  }

  // Validación de roles (Mitigación OWASP API5: Broken Function Level Authorization en cliente)
  if (allowedRoles && !allowedRoles.includes(usuario.rol)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <Outlet />;
};
