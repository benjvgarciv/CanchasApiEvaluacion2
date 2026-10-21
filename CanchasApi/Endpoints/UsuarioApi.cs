using CanchasApi.Models;
using CanchasApi.Repositories;
using CanchasApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CanchasApi.Endpoints
{
    public static class UsuarioApi
    {
        public static void MapUsuarioApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/usuarios").WithTags("Usuarios");

            grupo.MapPost("/registro", async (RegistroRequest request, IUsuarioRepository repo, AuthService auth, ClaimsPrincipal user) =>
            {
                if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Nombre, Email y Password son requeridos." });

                var existeEmail = await repo.ExisteEmailAsync(request.Email);
                if (existeEmail)
                    return Results.Conflict(new { error = "El correo electrónico ya está registrado." });

                var rol = "Cliente";
                if (!string.IsNullOrWhiteSpace(request.Rol))
                {
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                    if (userRole == "Administrador")
                    {
                        rol = request.Rol;
                    }
                }

                var nuevoUsuario = new Usuario
                {
                    Nombre = request.Nombre,
                    Email = request.Email,
                    Rol = rol,
                    FechaCreacion = DateTime.UtcNow
                };

                nuevoUsuario.PasswordHash = auth.HashPassword(nuevoUsuario, request.Password);

                await repo.AddAsync(nuevoUsuario);
                await repo.SaveChangesAsync();

                var response = new UsuarioResponse(
                    nuevoUsuario.Id,
                    nuevoUsuario.Nombre,
                    nuevoUsuario.Email,
                    nuevoUsuario.Rol,
                    nuevoUsuario.FechaCreacion
                );

                return Results.Created($"/api/v1/usuarios/{nuevoUsuario.Id}", response);
            });

            grupo.MapPost("/login", async (LoginRequest login, IUsuarioRepository repo, AuthService auth, IConfiguration config) =>
            {
                if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
                    return Results.BadRequest(new { error = "Email y Password son requeridos." });

                var usuario = await repo.GetByEmailAsync(login.Email);
                if (usuario is null)
                    return Results.Unauthorized();

                var verifyResult = auth.VerifyPassword(usuario, login.Password);
                if (verifyResult != PasswordVerificationResult.Success)
                    return Results.Unauthorized();

                var jwtKey = config["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(jwtKey))
                    throw new InvalidOperationException("Falta configurar 'Jwt:Key' en la configuración de la aplicación.");
                var jwtIssuer = config["Jwt:Issuer"] ?? "CanchasApi";
                var jwtAudience = config["Jwt:Audience"] ?? "CanchasApiUsuarios";
                var expiraMinutos = int.TryParse(config["Jwt:ExpiraMinutos"], out var mins) ? mins : 60;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expiraMinutos),
                    signingCredentials: credenciales
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                var userResponse = new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.FechaCreacion);

                return Results.Ok(new
                {
                    token = tokenString,
                    usuario = userResponse
                });
            }).RequireRateLimiting("login-policy");

            grupo.MapGet("/", async (IUsuarioRepository repo) =>
            {
                var usuarios = await repo.GetAllAsync();
                var response = usuarios
                    .Select(u => new UsuarioResponse(u.Id, u.Nombre, u.Email, u.Rol, u.FechaCreacion))
                    .ToList();

                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Administrador"));

            grupo.MapGet("/{id:int}", async (int id, IUsuarioRepository repo, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (currentRole != "Administrador" && currentUserId != id)
                    return Results.Forbid();

                var usuario = await repo.GetByIdAsync(id);
                if (usuario is null)
                    return Results.NotFound(new { error = "Usuario no encontrado." });

                var response = new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.FechaCreacion);
                return Results.Ok(response);
            }).RequireAuthorization();

            grupo.MapPut("/{id:int}", async (int id, UsuarioUpdateRequest request, IUsuarioRepository repo, AuthService auth, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (currentRole != "Administrador" && currentUserId != id)
                    return Results.Forbid();

                var usuario = await repo.GetByIdAsync(id);
                if (usuario is null)
                    return Results.NotFound(new { error = "Usuario no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Nombre))
                    usuario.Nombre = request.Nombre;

                if (!string.IsNullOrWhiteSpace(request.Password))
                    usuario.PasswordHash = auth.HashPassword(usuario, request.Password);

                if (currentRole == "Administrador" && !string.IsNullOrWhiteSpace(request.Rol))
                    usuario.Rol = request.Rol;

                repo.Update(usuario);
                await repo.SaveChangesAsync();

                var response = new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol, usuario.FechaCreacion);
                return Results.Ok(response);
            }).RequireAuthorization();

            grupo.MapDelete("/{id:int}", async (int id, IUsuarioRepository repo) =>
            {
                var usuario = await repo.GetByIdAsync(id);
                if (usuario is null)
                    return Results.NotFound(new { error = "Usuario no encontrado." });

                repo.Remove(usuario);
                await repo.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static int? GetUserId(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        public record RegistroRequest(string Nombre, string Email, string Password, string? Rol);
        public record LoginRequest(string Email, string Password);
        public record UsuarioUpdateRequest(string? Nombre, string? Password, string? Rol);
        public record UsuarioResponse(int Id, string Nombre, string Email, string Rol, DateTime FechaCreacion);
    }
}
