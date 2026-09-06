# ReservasCanchasDB API

API REST construida con ASP.NET Core Minimal APIs (.NET 10), Entity Framework Core, Autenticación JWT y documentación interactiva con Scalar.

## Configuración y Base de Datos

La base de datos `ReservasCanchasDB` utiliza por defecto **LocalDB**:
```
Server=.\\SQLEXPRESS;Initial Catalog=ReservasCanchasDB;Integrated Security=True;Encrypt=False
```

### Configuración con dotnet user-secrets
Para gestionar las credenciales y la clave JWT en desarrollo:
```bash
# Conexión LocalDB (Recomendada para desarrollo local):
dotnet user-secrets set "ConnectionStrings:miconexion" "Server=(localdb)\MSSQLLocalDB;Initial Catalog=ReservasCanchasDB;Integrated Security=True;Encrypt=False"

# Alternativa: SQLEXPRESS con autenticación SQL:
# dotnet user-secrets set "ConnectionStrings:miconexion" "Server=.\sqlexpress;Initial Catalog=ReservasCanchasDB;user id=sa;password=ipvg2026;Encrypt=False"

# Clave secreta JWT (Requerida):
dotnet user-secrets set "Jwt:Key" "TuClaveSecretaJWT_Minimo32CaracteresLongitud!"
```

> **Nota:** Por seguridad, `Jwt:Key` no tiene valor por defecto en el código fuente. Debe configurarse mediante `dotnet user-secrets` o variables de entorno antes de ejecutar la API.

## Ejecución
```bash
dotnet run --launch-profile http
```

- **Scalar API Reference:** `http://localhost:5098/scalar/v1`
- **Comprobación de Salud:** `http://localhost:5098/health`
