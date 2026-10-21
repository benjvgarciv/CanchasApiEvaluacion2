# Informe Técnico del Sistema de Gestión de Canchas

**Proyecto:** Plataforma de Gestión y Reservas de Complejos Deportivos  
**Stack Tecnológico:** ASP.NET Core Minimal APIs (.NET 10) + React 19 + TypeScript + Vite  

---

## 1. Justificación de la Arquitectura

### 1.1. React 19 + Vite + TypeScript (Frontend SPA)
- **Rendimiento y Tiempo de Carga:** El uso de Vite proporciona compilación y recarga de módulos ultrarrápida (HMR en milisegundos) gracias al preempaquetado nativo con ES Modules y Rollup para producción.
- **Tipado Fuerte y Consistencia de Contratos:** TypeScript garantiza una correspondencia estricta **1:1** con los DTOs y records del backend C# (`CanchaResponse`, `CanchaCreateRequest`, `ReservaResponse`, etc.). Esto erradica errores en tiempo de ejecución ocasionados por discrepancias en tipos de datos o nombres de propiedades.
- **Modularidad por Componentes:** La interfaz se dividió en secciones especializadas e independientes (`InicioSection`, `CanchasSection`, `SedesSection`, `TiposCanchaSection`, `ReservasSection`), facilitando el mantenimiento y permitiendo pruebas unitarias desacopladas.

### 1.2. Autenticación Centralizada mediante JWT
- **Stateless y Escalabilidad:** El uso de tokens JWT (*JSON Web Tokens*) permite que el backend no requiera almacenar sesiones en memoria, delegando la verificación criptográfica de autenticidad en cada petición mediante firma simétrica HMAC-SHA256 (`Jwt:Key`).
- **Control de Acceso Basado en Roles (RBAC):** El token transporta en sus claims el identificador (`NameIdentifier`) y el rol del usuario (`Role`). Tanto el backend como el frontend validan estos valores para permitir o restringir operaciones.
- **Intercepción y Ciclo de Vida del Token:** Mediante `apiClient` (Axios), se inyecta automáticamente la cabecera `Authorization: Bearer <token>`. Asimismo, el interceptor de respuesta detecta códigos `401 Unauthorized` para purgar credenciales obsoletas y redirigir al usuario al login con aviso de expiración (`?expired=1`), evitando estados zombi.

---

## 2. Matriz de Riesgos OWASP API Security Mitigados

| Vulnerabilidad OWASP | Riesgo Potencial | Mitigación Implementada | Evidencia en el Código |
| :--- | :--- | :--- | :--- |
| **API1:2023 — Broken Object Level Authorization (BOLA)** | Un usuario `Cliente` podría consultar o cancelar reservas ajenas cambiando el ID en la URL. | El backend verifica que el `UsuarioId` de la reserva coincida con el `NameIdentifier` del token JWT. Si no coincide ni es Administrador, responde `403 Forbidden`. | [ReservaApi.cs:L193](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/CanchasApi/Endpoints/ReservaApi.cs) |
| **API2:2023 — Broken Authentication & Session Fixation** | Suplantación de identidad mediante almacenamiento de contraseñas débiles o reutilización de tokens inválidos. | Las contraseñas se almacenan mediante hash criptográfico usando `PasswordHasher<Usuario>`. En logout y expiración (401), se eliminan por completo los tokens del almacenamiento local. | [AuthService.cs](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/CanchasApi/Services/AuthService.cs) y [client.ts:L33](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/frontend/src/api/client.ts) |
| **API3:2023 — Broken Object Property Level Authorization (Mass Assignment)** | Un atacante podría autoasignarse el rol `Administrador` al registrarse o enviar cargas JSON adulteradas. | En el registro, el rol asignado por defecto siempre es `Cliente`. El parámetro `Rol` solo es considerado si el usuario solicitante tiene rol `Administrador`. | [UsuarioApi.cs:L30](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/CanchasApi/Endpoints/UsuarioApi.cs) |
| **API4:2023 — Unrestricted Resource Consumption (Fuerza Bruta)** | Ataques de denegación de servicio o diccionario contra el formulario de inicio de sesión. | Implementación de Rate Limiting con política `AddFixedWindowLimiter` (máximo 5 intentos por minuto por IP/cliente). Respuestas `429 Too Many Requests`. | [Program.cs:L28](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/CanchasApi/Program.cs) |
| **API5:2023 — Broken Function Level Authorization & XSS** | Clientes invocando endpoints de eliminación o inyección de scripts en cabeceras HTTP. | Políticas explícitas `.RequireRole("Administrador", "Trabajador")` en backend, componentes `ProtectedRoute` y filtrado estricto en Sidebar. Headers HTTP de seguridad (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`). | [Program.cs:L83](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/CanchasApi/Program.cs) y [ProtectedRoute.tsx](file:///c:/Users/MATI/Desktop/Evaliacion%201%20enso/CanchasApi26-08/CanchasApi26-08/frontend/src/components/ProtectedRoute.tsx) |

---

## 3. Ficha de Accesibilidad y Usabilidad (WCAG 2.2 Nivel AA)

El frontend ha sido diseñado y validado bajo directrices internacionales de accesibilidad:

### 3.1. Tamaño del Objetivo Táctil (Criterio WCAG 2.5.8 - Target Size)
- **Requisito:** Todo control interactivo primario debe poseer una dimensión mínima de 44x44 píxeles para facilitar su activación en dispositivos móviles o por usuarios con dificultades motoras.
- **Implementación en CSS:**
  ```css
  input[type="text"], input[type="email"], input[type="password"],
  input[type="number"], select, textarea, button, .btn {
    min-height: 44px; /* WCAG 2.2 AA Target Size */
  }
  ```

### 3.2. Contraste de Color y Tipografía (Criterio WCAG 1.4.3 - Contrast Minimum)
- Ratio de contraste superior a **4.5:1** entre el texto principal (`#0f172a`) y la superficie de fondo (`#ffffff`, `#f8fafc`).
- Botones de acción primaria (`#1e40af` sobre blanco) y alertas de peligro (`#991b1b` sobre fondo `#fef2f2` con borde `#fecaca`) cumplen holgadamente los umbrales de legibilidad.

### 3.3. Semántica ARIA y Estados del Sistema (Criterio WCAG 4.1.2 y 4.1.3)
- Modales configurados con `role="dialog"` y `aria-modal="true"`.
- Mensajes de error en formularios e interfaces provistos de `role="alert"`.
- Indicadores de carga y banners de estado con `role="status"` y atributos `aria-live="polite"` para anunciar cambios a lectores de pantalla.
- Formularios con etiquetas explícitas `<label htmlFor="...">` asociadas unívocamente a cada input.

### 3.4. Diseño Adaptativo (Responsive Design)
- Maquetación fluida y adaptable a anchos de pantalla reducidos a partir de **320px**.
- Menú lateral transformable en barra desplegable (*drawer*) con fondo oscurecido y botón accesible de apertura y cierre para móviles.
