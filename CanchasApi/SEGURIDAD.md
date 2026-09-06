# Seguridad — Mitigaciones OWASP API Security Top 10

Este documento detalla las vulnerabilidades del OWASP API Security Top 10 que fueron mitigadas en CanchasApi de acuerdo a los requerimientos del Trabajo 1.

## 1. API1:2023 — Broken Object Level Authorization (BOLA)

**Riesgo:** Un usuario autenticado podría acceder o manipular datos de otro usuario cambiando un identificador en la URL (por ejemplo, `GET /api/v1/reservas/57` consultando la reserva de otra persona).

**Mitigación:** En los endpoints que manejan recursos asociados a usuarios (`/api/v1/usuarios/{id}`, `/api/v1/reservas`, `/api/v1/reservas/{id}`, pagos, notificaciones y comprobantes), se valida que el `UsuarioId` del recurso coincida con el `NameIdentifier` extraído del token JWT:
- Si el usuario tiene rol `Cliente`, únicamente puede consultar o interactuar con sus propios recursos.
- Si intenta consultar o alterar un recurso ajeno, la API responde `403 Forbidden`.
- Los usuarios con rol `Administrador` (o `Trabajador` según corresponda) conservan el acceso global de gestión.

```csharp
if (currentRole != "Administrador" && currentUserId != id)
    return Results.Forbid();
```

## 2. API2:2023 — Broken Authentication

**Riesgo:** Autenticación vulnerable debido a almacenamiento inadecuado de credenciales o uso de claves secretas predecibles en la firma de tokens JWT.

**Mitigación:**
- La clave secreta de JWT (`Jwt:Key`) se carga dinámicamente desde la configuración (`dotnet user-secrets` en desarrollo o variables de entorno). La API no inicia si la clave no está configurada.
- Las contraseñas se almacenan con hash criptográfico utilizando `PasswordHasher<Usuario>` de ASP.NET Core Identity.
- En la validación de tokens JWT se comprueban estrictamente firma, emisor, audiencia y tiempo de expiración (`ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey`, `ValidateLifetime = true`).

## 3. API3:2023 — Broken Object Property Level Authorization (Mass Assignment)

**Riesgo:** Un usuario común podría elevar sus propios privilegios enviando parámetros no autorizados en el cuerpo de la petición (por ejemplo, enviando `"Rol": "Administrador"` al registrarse o actualizar su perfil).

**Mitigación:** En los endpoints de registro y actualización de usuario, el rol asignado por defecto siempre es `Cliente`. El parámetro `Rol` solo es procesado si la petición proviene de un usuario autenticado con rol `Administrador`:

```csharp
if (currentRole == "Administrador" && !string.IsNullOrWhiteSpace(request.Rol))
    usuario.Rol = request.Rol;
```

## 4. API5:2023 — Broken Function Level Authorization

**Riesgo:** Acceso a funciones administrativas críticas por parte de usuarios con roles estándar.

**Mitigación:** Endpoints administrativos sensibles (como listar la totalidad de usuarios del sistema en `GET /api/v1/usuarios` o eliminar usuarios en `DELETE /api/v1/usuarios/{id}`) están protegidos explícitamente mediante políticas de rol:

```csharp
.RequireAuthorization(p => p.RequireRole("Administrador"));
```

Cualquier usuario sin el rol correspondiente recibe automáticamente un `403 Forbidden`.

## 5. API8:2023 — Security Misconfiguration

**Riesgo:** Exposición accidental de secretos de conexión o llaves de cifrado en repositorios, o fuga de información técnica a través de mensajes de error en producción.

**Mitigación:**
- `appsettings.json` no contiene claves ni credenciales sensibles en texto plano; estas se administran a través del gestor de secretos de .NET en desarrollo.
- La página de detalles de excepción (`UseDeveloperExceptionPage`) se encuentra condicionada estrictamente al entorno de desarrollo (`builder.Environment.IsDevelopment()`), evitando la exposición de stack traces en producción.

---

## Observaciones Adicionales

- **Consumo de recursos (API4)**: Como mejora futura se puede incorporar limitación de tasa de peticiones (*rate limiting*) mediante el middleware `AddRateLimiter` de ASP.NET Core.
- **Inventario de APIs (API9)**: Se implementó versionamiento explícito en las rutas (`/api/v1/...`) y documentación mediante OpenAPI y Scalar.
