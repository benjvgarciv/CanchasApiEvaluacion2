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

app.Run();

public partial class Program { }
