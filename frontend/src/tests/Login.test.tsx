import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { Login } from '../pages/Login';
import { AuthProvider } from '../context/AuthContext';

describe('Login Component', () => {
  it('renderiza correctamente el formulario con inputs accesibles', () => {
    render(
      <BrowserRouter>
        <AuthProvider>
          <Login />
        </AuthProvider>
      </BrowserRouter>
    );

    expect(screen.getByRole('heading', { name: /iniciar sesión/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/correo electrónico/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /entrar/i })).toBeInTheDocument();
  });

  it('muestra mensaje de validación si los campos requeridos están vacíos', async () => {
    render(
      <BrowserRouter>
        <AuthProvider>
          <Login />
        </AuthProvider>
      </BrowserRouter>
    );

    const submitBtn = screen.getByRole('button', { name: /entrar/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/completa todos los campos/i);
    });
  });

  it('permite escribir en los campos de correo y contraseña', () => {
    render(
      <BrowserRouter>
        <AuthProvider>
          <Login />
        </AuthProvider>
      </BrowserRouter>
    );

    const emailInput = screen.getByLabelText(/correo electrónico/i) as HTMLInputElement;
    const passwordInput = screen.getByLabelText(/contraseña/i) as HTMLInputElement;

    fireEvent.change(emailInput, { target: { value: 'cliente@canchas.com' } });
    fireEvent.change(passwordInput, { target: { value: '123456' } });

    expect(emailInput.value).toBe('cliente@canchas.com');
    expect(passwordInput.value).toBe('123456');
  });
});
