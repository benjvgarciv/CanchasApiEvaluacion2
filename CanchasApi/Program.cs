using CanchasApi.Endpoints;
using CanchasApi.Middleware;
using CanchasApi.Models;
using CanchasApi.Repositories;
using CanchasApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS explícita para consumo desde el frontend
const string FrontEndCorsPolicy = "FrontendPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontEndCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configuración de Rate Limiting (Mitigación OWASP API4: Unrestricted Resource Consumption / Fuerza bruta)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login-policy", opt =>
    {
        opt.PermitLimit = 5; // 5 intentos permitidos
        opt.Window = TimeSpan.FromMinutes(1); // por ventana de 1 minuto
        opt.QueueLimit = 0;
    });
});

// Cargamos todo lo referente a JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta configurar 'Jwt:Key' en la configuración de la aplicación.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudince = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudince,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
            };
        });
builder.Services.AddAuthorization();

// Conexión a la base de datos
builder.Services.AddDbContext<ReservasCanchasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("miconexion")));

// OpenAPI / Scalar
builder.Services.AddOpenApi();

// Invocamos la clase de autenticación
builder.Services.AddScoped<AuthService>();

// Repositorios
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

var app = builder.Build();

app.UseRequestLogging();

// Middleware de Cabeceras de Seguridad HTTP (Mitigación OWASP API8: Security Misconfiguration)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Habilitar CORS y Rate Limiting antes de Auth y endpoints
app.UseCors(FrontEndCorsPolicy);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Endpoint para comprobar conexión a la base de datos
app.MapGet("/health", async (ReservasCanchasDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
            return Results.Ok(new { database = "connected" });
        return Results.StatusCode(503);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 503);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Creación de EndPoints
app.MapUsuarioApi();
app.MapTrabajadorApi();
app.MapSedeApi();
app.MapTipoCanchaApi();
app.MapCanchaApi();
app.MapHorarioApi();
app.MapReservaApi();
app.MapPagoApi();
app.MapComprobanteApi();
app.MapResenaApi();
app.MapMantenimientoApi();
app.MapNotificacionApi();

app.MapPost("/crear-admin-test", async (ReservasCanchasDbContext db, AuthService auth) =>
{
    var existingAdmin = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@admin.com");
    if (existingAdmin != null)
    {
        existingAdmin.Rol = "Administrador";
        existingAdmin.PasswordHash = auth.HashPassword(existingAdmin, "123456");
        await db.SaveChangesAsync();
        return Results.Ok(new { mensaje = "Usuario admin@admin.com actualizado exitosamente", usuarioId = existingAdmin.Id });
    }

    var admin = new Usuario
    {
        Nombre = "Administrador",
        Email = "admin@admin.com",
        Rol = "Administrador",
        FechaCreacion = DateTime.UtcNow
    };

    admin.PasswordHash = auth.HashPassword(admin, "123456");

    db.Usuarios.Add(admin);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/usuarios/{admin.Id}", new { mensaje = "Usuario admin@admin.com creado exitosamente", usuarioId = admin.Id });
});

app.MapPost("/crear-usuarios-test", async (ReservasCanchasDbContext db, AuthService auth) =>
{
    var usuariosSeed = new[]
    {
        new { Nombre = "Cliente Test", Email = "cliente@canchas.com", Password = "123456", Rol = "Cliente" },
        new { Nombre = "Trabajador Test", Email = "trabajador@canchas.com", Password = "123456", Rol = "Trabajador" },
        new { Nombre = "Administrador", Email = "admin@admin.com", Password = "123456", Rol = "Administrador" }
    };

    var creados = new List<object>();

    foreach (var item in usuariosSeed)
    {
        var usuarioExistente = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == item.Email);
        if (usuarioExistente != null)
        {
            usuarioExistente.Nombre = item.Nombre;
            usuarioExistente.Rol = item.Rol;
            usuarioExistente.PasswordHash = auth.HashPassword(usuarioExistente, item.Password);
            creados.Add(new { item.Email, item.Rol, Estado = "Actualizado" });
        }
        else
        {
            var nuevoUsuario = new Usuario
            {
                Nombre = item.Nombre,
                Email = item.Email,
                Rol = item.Rol,
                FechaCreacion = DateTime.UtcNow
            };
            nuevoUsuario.PasswordHash = auth.HashPassword(nuevoUsuario, item.Password);
            db.Usuarios.Add(nuevoUsuario);
            creados.Add(new { item.Email, item.Rol, Estado = "Creado" });
        }
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        mensaje = "Usuarios de prueba verificados y configurados correctamente en la base de datos.",
        usuarios = creados
    });
});

app.MapPost("/crear-seed-500", async (ReservasCanchasDbContext db, AuthService auth) =>
{
    // 1. Asegurar Sedes maestras
    if (!await db.Sedes.AnyAsync())
    {
        db.Sedes.AddRange(
            new Sede { Nombre = "Sede Central Cordillera", Direccion = "Av. Las Condes 1234", Telefono = "+56911223344" },
            new Sede { Nombre = "Sede Poniente Estadio", Direccion = "Av. Pajaritos 5678", Telefono = "+56955667788" },
            new Sede { Nombre = "Sede Norte El Roble", Direccion = "Av. Recoleta 9101", Telefono = "+56999887766" }
        );
        await db.SaveChangesAsync();
    }

    // 2. Asegurar Tipos de Cancha
    if (!await db.TiposCancha.AnyAsync())
    {
        db.TiposCancha.AddRange(
            new TipoCancha { Nombre = "Fútbol 7 Sintético", Descripcion = "Pasto sintético monofilamento con iluminación LED", PrecioPorHora = 35000m },
            new TipoCancha { Nombre = "Pádel Panorámico", Descripcion = "Cancha de cristal panorámico techada", PrecioPorHora = 22000m },
            new TipoCancha { Nombre = "Tenis Arcilla", Descripcion = "Superficie de polvo de ladrillo reglamentaria", PrecioPorHora = 20000m }
        );
        await db.SaveChangesAsync();
    }

    var sedes = await db.Sedes.ToListAsync();
    var tipos = await db.TiposCancha.ToListAsync();

    // 3. Asegurar Canchas
    if (await db.Canchas.CountAsync() < 6)
    {
        for (int i = 1; i <= 6; i++)
        {
            var sede = sedes[(i - 1) % sedes.Count];
            var tipo = tipos[(i - 1) % tipos.Count];
            if (!await db.Canchas.AnyAsync(c => c.Numero == i && c.SedeId == sede.Id))
            {
                db.Canchas.Add(new Cancha
                {
                    Numero = i,
                    SedeId = sede.Id,
                    TipoCanchaId = tipo.Id,
                    Estado = "Disponible"
                });
            }
        }
        await db.SaveChangesAsync();
    }

    var canchas = await db.Canchas.Include(c => c.TipoCancha).ToListAsync();

    // 4. Asegurar Clientes Sintéticos
    var usuariosSinteticos = new List<Usuario>();
    for (int u = 1; u <= 15; u++)
    {
        var email = $"jugador{u:D2}@complejosport.cl";
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        if (usuario == null)
        {
            usuario = new Usuario
            {
                Nombre = $"Jugador Sintético {u}",
                Email = email,
                Rol = "Cliente",
                FechaCreacion = DateTime.UtcNow.AddMonths(-12)
            };
            usuario.PasswordHash = auth.HashPassword(usuario, "123456");
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
        }
        usuariosSinteticos.Add(usuario);
    }

    // 5. Asegurar Trabajador
    var trabajadorUser = await db.Usuarios.FirstOrDefaultAsync(u => u.Rol == "Trabajador" || u.Rol == "Administrador");
    Trabajador? trabajador = null;
    if (trabajadorUser != null)
    {
        trabajador = await db.Trabajadores.FirstOrDefaultAsync(t => t.UsuarioId == trabajadorUser.Id);
        if (trabajador == null)
        {
            trabajador = new Trabajador { UsuarioId = trabajadorUser.Id, Cargo = "Coordinador de Canchas", Turno = "Tarde/Noche" };
            db.Trabajadores.Add(trabajador);
            await db.SaveChangesAsync();
        }
    }

    // 6. Generar 500 reservas con 12 meses de historia
    int reservasExistentes = await db.Reservas.CountAsync();
    int reservasParaCrear = Math.Max(0, 500 - reservasExistentes);

    if (reservasParaCrear > 0)
    {
        var random = new Random(42); // Semilla fija para determinismo y reproducibilidad
        var now = DateTime.UtcNow;
        var startHistory = now.AddDays(-365);
        int totalDays = 365;

        for (int i = 0; i < reservasParaCrear; i++)
        {
            var cancha = canchas[random.Next(canchas.Count)];
            var usuario = usuariosSinteticos[random.Next(usuariosSinteticos.Count)];

            // Distribución temporal en los últimos 12 meses
            int dayOffset = (int)((double)i / reservasParaCrear * totalDays);
            var reservationDate = startHistory.AddDays(dayOffset);

            // Franjas horarias realistas: entre 09:00 y 22:00
            int startHour = random.Next(9, 22);
            var fechaInicio = new DateTime(reservationDate.Year, reservationDate.Month, reservationDate.Day, startHour, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddHours(1);

            // Ponderación de estados según antigüedad
            string estado;
            bool esPasada = fechaInicio < now.AddDays(-1);

            if (esPasada)
            {
                int r = random.Next(100);
                if (r < 85) estado = "Completada";
                else estado = "Cancelada";
            }
            else
            {
                int r = random.Next(100);
                if (r < 60) estado = "Confirmada";
                else estado = "Pendiente";
            }

            var reserva = new Reserva
            {
                CanchaId = cancha.Id,
                UsuarioId = usuario.Id,
                TrabajadorId = trabajador?.Id,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = estado
            };

            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();

            // Si es completada o confirmada, generar Pago y Comprobante
            if (estado == "Completada" || estado == "Confirmada")
            {
                string[] metodos = { "Tarjeta", "Transferencia", "Efectivo" };
                var metodo = metodos[random.Next(metodos.Length)];
                var monto = cancha.TipoCancha?.PrecioPorHora ?? 30000m;

                var pago = new Pago
                {
                    ReservaId = reserva.Id,
                    Monto = monto,
                    FechaPago = fechaInicio.AddMinutes(-30),
                    MetodoPago = metodo,
                    Estado = "Pagado"
                };
                db.Pagos.Add(pago);
                await db.SaveChangesAsync();

                var comprobante = new Comprobante
                {
                    PagoId = pago.Id,
                    NumeroComprobante = $"BOL-{fechaInicio.Year}-{reserva.Id:D6}",
                    Tipo = (random.Next(10) > 8) ? "Factura" : "Boleta",
                    FechaEmision = fechaInicio.AddMinutes(-25)
                };
                db.Comprobantes.Add(comprobante);

                // 30% de probabilidad de tener reseña en arriendos completados
                if (estado == "Completada" && random.Next(100) < 30)
                {
                    string[] comentarios = {
                        "Excelente estado del pasto sintético y buena iluminación.",
                        "Muy puntual la atención y los vestuarios limpios.",
                        "Cancha impecable, fácil estacionamiento.",
                        "Muy buena experiencia, volveremos la próxima semana."
                    };
                    db.Resenas.Add(new Resena
                    {
                        ReservaId = reserva.Id,
                        UsuarioId = usuario.Id,
                        Calificacion = (byte)random.Next(4, 6), // 4 o 5 estrellas
                        Comentario = comentarios[random.Next(comentarios.Length)],
                        Fecha = fechaFin.AddHours(2)
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    var totalFinal = await db.Reservas.CountAsync();
    var totalPagos = await db.Pagos.CountAsync();
    var totalComprobantes = await db.Comprobantes.CountAsync();

    return Results.Ok(new
    {
        mensaje = "Sembrado masivo de 500 registros y 12 meses de historia completado exitosamente.",
        totalReservas = totalFinal,
        totalPagos = totalPagos,
        totalComprobantes = totalComprobantes
    });
});

app.Run();

public partial class Program { }
