using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Endpoints
{
    public static class TrabajadorApi
    {
        public static void MapTrabajadorApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/trabajadores")
                .WithTags("Trabajadores")
                .RequireAuthorization(p => p.RequireRole("Administrador"));

            grupo.MapGet("/", async (ReservasCanchasDbContext db) =>
            {
                var trabajadores = await db.Trabajadores
                    .Include(t => t.Usuario)
                    .Select(t => new TrabajadorResponse(
                        t.Id,
                        t.UsuarioId,
                        t.Usuario.Nombre,
                        t.Usuario.Email,
                        t.Cargo,
                        t.Turno
                    ))
                    .ToListAsync();

                return Results.Ok(trabajadores);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var trabajador = await db.Trabajadores
                    .Include(t => t.Usuario)
                    .Where(t => t.Id == id)
                    .Select(t => new TrabajadorResponse(
                        t.Id,
                        t.UsuarioId,
                        t.Usuario.Nombre,
                        t.Usuario.Email,
                        t.Cargo,
                        t.Turno
                    ))
                    .FirstOrDefaultAsync();

                return trabajador is not null
                    ? Results.Ok(trabajador)
                    : Results.NotFound(new { error = "Trabajador no encontrado." });
            });

            grupo.MapPost("/", async (TrabajadorCreateRequest request, ReservasCanchasDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Cargo) || string.IsNullOrWhiteSpace(request.Turno))
                    return Results.BadRequest(new { error = "Cargo y Turno son requeridos." });

                var usuario = await db.Usuarios.FindAsync(request.UsuarioId);
                if (usuario is null)
                    return Results.BadRequest(new { error = "El usuario especificado no existe." });

                var yaEsTrabajador = await db.Trabajadores.AnyAsync(t => t.UsuarioId == request.UsuarioId);
                if (yaEsTrabajador)
                    return Results.Conflict(new { error = "El usuario ya está registrado como trabajador." });

                var trabajador = new Trabajador
                {
                    UsuarioId = request.UsuarioId,
                    Cargo = request.Cargo,
                    Turno = request.Turno
                };

                if (usuario.Rol == "Cliente")
                {
                    usuario.Rol = "Trabajador";
                }

                db.Trabajadores.Add(trabajador);
                await db.SaveChangesAsync();

                var response = new TrabajadorResponse(
                    trabajador.Id,
                    trabajador.UsuarioId,
                    usuario.Nombre,
                    usuario.Email,
                    trabajador.Cargo,
                    trabajador.Turno
                );

                return Results.Created($"/api/v1/trabajadores/{trabajador.Id}", response);
            });

            grupo.MapPut("/{id:int}", async (int id, TrabajadorUpdateRequest request, ReservasCanchasDbContext db) =>
            {
                var trabajador = await db.Trabajadores.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.Id == id);
                if (trabajador is null)
                    return Results.NotFound(new { error = "Trabajador no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Cargo))
                    trabajador.Cargo = request.Cargo;

                if (!string.IsNullOrWhiteSpace(request.Turno))
                    trabajador.Turno = request.Turno;

                await db.SaveChangesAsync();

                var response = new TrabajadorResponse(
                    trabajador.Id,
                    trabajador.UsuarioId,
                    trabajador.Usuario.Nombre,
                    trabajador.Usuario.Email,
                    trabajador.Cargo,
                    trabajador.Turno
                );

                return Results.Ok(response);
            });

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var trabajador = await db.Trabajadores.FindAsync(id);
                if (trabajador is null)
                    return Results.NotFound(new { error = "Trabajador no encontrado." });

                db.Trabajadores.Remove(trabajador);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }

        public record TrabajadorCreateRequest(int UsuarioId, string Cargo, string Turno);
        public record TrabajadorUpdateRequest(string? Cargo, string? Turno);
        public record TrabajadorResponse(int Id, int UsuarioId, string UsuarioNombre, string UsuarioEmail, string Cargo, string Turno);
    }
}
