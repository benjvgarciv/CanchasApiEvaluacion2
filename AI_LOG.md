# Bitácora de Desarrollo con Inteligencia Artificial (AI_LOG.md)

Este documento registra los prompts clave, herramientas utilizadas, errores detectados y resueltos, y decisiones arquitectónicas adoptadas durante el desarrollo del sistema de gestión de canchas deportivas (**CanchasApi** y **Frontend SPA**).

---

## 1. Bitácora de Prompts y Requerimientos

### Sesión 1: Resolución de Conectividad y Sembrado Inicial
- **Prompt:** *"Tengo el error 'No se pudo conectar con el servidor' en el frontend y tampoco conozco los usuarios de la base de datos. Revisa el puerto de mi API en Properties/launchSettings.json y asegúrate de que el archivo frontend/.env tenga exactamente la misma URL en VITE_API_URL. Crea un endpoint temporal muy sencillo en Program.cs llamado /crear-admin-test con rol 'Administrador', correo 'admin@admin.com' y contraseña '123456'."*
- **Herramientas Utilizadas:** `view_file`, `replace_file_content`, `run_command` (PowerShell `Get-NetTCPConnection` e `Invoke-WebRequest`).
- **Problema Detectado:** Desfase entre el frontend configurado en HTTPS `7269` y el proceso local de la API corriendo activamente en HTTP `5098`.
- **Solución Aplicada:**
  - Se ajustó `frontend/.env` a `VITE_API_URL=http://localhost:5098`.
  - Se implementó `POST /crear-admin-test` en `Program.cs` usando `PasswordHasher<Usuario>` de ASP.NET Core Identity.

---

### Sesión 2: Transformación del Dashboard con Estados Visuales y Contratos 1:1
- **Prompt:** *"Necesito transformar frontend/src/pages/Dashboard.tsx en el panel principal de gestión, pero debe adaptarse exactamente a la API y modelos que ya tenemos construidos en el backend de C#. Incluye los 3 estados visuales exigidos por rúbrica: Cargando, Error de API y Lista Vacía. Respeta los DTOs de entrada real y oculta botones para el rol Cliente."*
- **Herramientas Utilizadas:** `view_file`, `replace_file_content`, `write_to_file`, `run_command` (`npm run build`).
- **Decisiones de Arquitectura:**
  - Mapeo estricto TypeScript 1:1 con `CanchaResponse`, `CanchaCreateRequest`, `SedeResponse` y `TipoCanchaResponse`.
  - Implementación de los 3 estados visuales en `Dashboard.tsx` con semántica ARIA (`role="status"`, `role="alert"`).
  - Condicionamiento de controles con `canManage = usuario?.rol === 'Administrador' || usuario?.rol === 'Trabajador'`.

---

### Sesión 3: Portal Completo de Administración con Navegación por Apartados y Seed Data
- **Prompt:** *"Crea los componentes necesarios para tener un Portal de Administración Completo con Apartados Estructurados (Sidebar con lucide-react para Canchas, Sedes, Tipos de Cancha y Reservas). Agrega botón de Carga de Datos Iniciales (Seed Data) en Sedes y Tipos si devuelven 0 registros."*
- **Herramientas Utilizadas:** `write_to_file`, `replace_file_content`, `run_command` (`npm run build`).
- **Componentes Creados:**
  - `CanchasSection.tsx`: Búsqueda reactiva, filtros por sede/estado, modal CRUD.
  - `SedesSection.tsx`: Listado, modal CRUD y función `handleSeedData` (POST automático de 2 sedes).
  - `TiposCanchaSection.tsx`: Listado, modal CRUD y función `handleSeedData` (POST automático de 3 tipos de cancha).
  - `ReservasSection.tsx`: Tabla de agenda global y filtros de estado.
  - Maquetación adaptativa a móviles desde 320px en `index.css`.

---

### Sesión 4: Usuarios de Prueba Multirrol (Criterio 6)
- **Prompt:** *"Actualiza o crea un endpoint de prueba en Program.cs llamado POST /crear-usuarios-test que cree automáticamente tres cuentas: Cliente (cliente@canchas.com), Trabajador (trabajador@canchas.com) y Administrador (admin@admin.com / 123456)."*
- **Solución:** Endpoint idempotente en `Program.cs` que verifica existencia y hashea contraseñas con `PasswordHasher<Usuario>`.

---

### Sesión 5: Flujo de Reservas del Jugador y Menú por Roles
- **Prompt:** *"Establece Inicio como pestaña por defecto con bienvenida y accesos rápidos. Si rol es Cliente, muestra solo Inicio, Explorar Canchas y Mis Reservas. En Canchas agrega botón Reservar con modal accesible. En Reservas agrega botón Agendar Nueva Reserva si está vacía."*
- **Componentes Añadidos / Actualizados:**
  - `InicioSection.tsx`: Tarjeta con gradiente, métricas de canchas disponibles, sedes y reservas, accesos directos.
  - Modal de reserva en `CanchasSection.tsx` con cálculo automático de costo por horas.
  - Adaptación de `Dashboard.tsx` para filtrar el menú lateral dinámicamente según `usuario.rol`.

---

### Sesión 6: Gestión y Asignación de Reservas en Tiempo Real
- **Prompt:** *"En Reservas agrega columna Acciones para Trabajador/Administrador con botones Aprobar/Confirmar, Asignarme y Cancelar. Corrige que 'Asignarme' mantenga la columna Atención como 'Sin asignar'."*
- **Solución Backend & Frontend:**
  - En `ReservaApi.cs` (`PUT /api/v1/reservas/{id}`): resolución de `trabajadorId` admitiendo tanto `Trabajador.Id` como `Trabajador.UsuarioId`, autocreación de registro trabajador si no existía, y recarga de relación `Include(t => t.Usuario)`.
  - En `ReservasSection.tsx`: envío de `currentUserId` y actualización reactiva inmediata en la tabla.

---

### Sesión 7: Pruebas Automatizadas y Documentación Final
- **Prompt:** *"Configura Vitest y React Testing Library. Crea pruebas para Login.tsx y CanchasSection.tsx (verificación de botones ocultos para rol Cliente). Genera AI_LOG.md, INFORME_TECNICO.md, actualiza README.md y crea .env.example."*
- **Herramientas:** `npm install -D vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom`.
- **Resultados:** 6 pruebas unitarias aprobadas (100% passing), 0 linter errors, compilación exitosa.

---

## 2. Decisiones de Arquitectura y Tecnologías
1. **Frontend:** React 19 + TypeScript + Vite para máxima velocidad de desarrollo y tipado estricto.
2. **Estilizado:** Vanilla CSS con variables CSS y diseño adaptable a pantallas móviles (320px). Sin dependencias pesadas de estilos externos.
3. **Control de Acceso y Estado:** `AuthContext` + `ProtectedRoute` para validación cliente de roles combinada con interceptores Axios para inyección Bearer JWT y tratamiento de sesiones expiradas (401).
4. **Resiliencia de Conexión:** Manejo de reintentos en pantalla ante caídas del servidor mediante los 3 estados visuales de la rúbrica.
