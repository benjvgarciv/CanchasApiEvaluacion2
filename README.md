# Sistema de Gestión de Canchas Deportivas (CanchasApi & Frontend)

Plataforma integral para administración, reservas y catálogo de canchas deportivas desarrollada con **ASP.NET Core Minimal APIs (.NET 10)** en el backend y **React 19 + TypeScript + Vite** en el frontend.

---

## 🚀 Arquitectura y Puertos del Sistema

| Módulo | Tecnología | Puerto Local | Documentación / URL |
| :--- | :--- | :---: | :--- |
| **Backend API** | ASP.NET Core 10, EF Core, JWT | `http://localhost:5098` | [Scalar API Reference](http://localhost:5098/scalar/v1) \| Salud: `/health` |
| **Frontend SPA** | React 19, TypeScript, Vite | `http://localhost:5173` | Panel de Control: `/dashboard` |

---

## 👥 Cuentas de Prueba Preconfiguradas (Roles del Sistema)

Para evaluar el **Criterio 6 de la Rúbrica (Diferenciación de Vistas por Roles)**, puedes utilizar las siguientes credenciales cargadas mediante el endpoint `POST /crear-usuarios-test`:

| Rol | Correo Electrónico | Contraseña | Permisos y Comportamiento en la Interfaz |
| :--- | :--- | :---: | :--- |
| **Cliente** | `cliente@canchas.com` | `123456` | • Menú lateral simplificado (*Inicio*, *Explorar Canchas*, *Mis Reservas*).<br>• Opciones de *Sedes* y *Tipos de Cancha* **ocultas**.<br>• Acceso a reservar canchas disponibles con cálculo de tarifa por hora.<br>• Sin permisos para crear, editar o eliminar canchas. |
| **Trabajador** | `trabajador@canchas.com` | `123456` | • Menú completo.<br>• Gestión y creación de Canchas, Sedes y Tipos.<br>• Agenda Global con columna **Acciones** para confirmar reservas y asignarse la atención (`UserCheck`). |
| **Administrador** | `admin@admin.com` | `123456` | • Control total del sistema y acceso a todas las configuraciones. |

---

## 🛠️ Instrucciones de Instalación y Ejecución

### 1. Iniciar el Backend (.NET 10)
```bash
cd CanchasApi26-08/CanchasApi
dotnet run --launch-profile http
```
> El servicio iniciará en `http://localhost:5098`.

### 2. Poblar Usuarios de Prueba
Con la API corriendo, ejecuta una petición POST (PowerShell, cURL o Postman):
```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5098/crear-usuarios-test"
```

### 3. Iniciar el Frontend (React + Vite)
```bash
cd CanchasApi26-08/frontend
npm install
npm run dev
```
> Accede a `http://localhost:5173`. Si necesitas regenerar variables de entorno, consulta el archivo `.env.example`.

### 4. Ejecutar Pruebas Automatizadas y Validación
```bash
# Ejecutar suite de pruebas unitarias (Vitest + Testing Library)
npm run test

# Validación de linter
npm run lint

# Compilación de producción (TypeScript + Vite)
npm run build
```

---

## 📑 Documentación Adicional
- [INFORME_TECNICO.md](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/INFORME_TECNICO.md): Justificación técnica, matriz de 5 riesgos OWASP mitigados y ficha de accesibilidad WCAG 2.2 AA.
- [AI_LOG.md](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/AI_LOG.md): Bitácora de prompts, soluciones de errores y decisiones arquitectónicas asistidas por IA.
- [SEGURIDAD.md](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/CanchasApi/SEGURIDAD.md): Detalle técnico de las defensas OWASP implementadas en la API C#.
