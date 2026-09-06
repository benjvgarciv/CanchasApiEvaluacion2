using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanchasApi.Endpoints
{
    public static class ResenaApi
    {
        public static void MapResenaApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/resenas").WithTags("Reseñas");

            grupo.MapGet("/", async (int? canchaId, ReservasCanchasDbContext db) =>
            {
                var query = db.Resenas
                    .Include(r => r.Usuario)
                    .Include(r => r.Reserva)
                        .ThenInclude(res => res.Cancha)
                    .AsQueryable();

                if (canchaId.HasValue)
                    query = query.Where(r => r.Reserva.CanchaId == canchaId.Value);

                var resenas = await query
                    .OrderByDescending(r => r.Fecha)
                    .Select(r => new ResenaResponse(
                        r.Id,
                        r.ReservaId,
                        r.UsuarioId,
                        r.Usuario.Nombre,
                        r.Reserva.CanchaId,
                        r.Reserva.Cancha.Numero,
                        r.Calificacion,
                        r.Comentario,
                        r.Fecha
                    ))
                    .ToListAsync();

                return Results.Ok(resenas);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var resena = await db.Resenas
                    .Include(r => r.Usuario)
                    .Include(r => r.Reserva)
                        .ThenInclude(res => res.Cancha)
                    .Where(r => r.Id == id)
                    .Select(r => new ResenaResponse(
                        r.Id,
                        r.ReservaId,
                        r.UsuarioId,
                        r.Usuario.Nombre,
                        r.Reserva.CanchaId,
                        r.Reserva.Cancha.Numero,
                        r.Calificacion,
                        r.Comentario,
                        r.Fecha
                    ))
                    .FirstOrDefaultAsync();

                return resena is not null
                    ? Results.Ok(resena)
                    : Results.NotFound(new { error = "Reseña no encontrada." });
            });

            grupo.MapPost("/", async (ResenaCreateRequest request, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (!currentUserId.HasValue)
                    return Results.Unauthorized();

                if (request.Calificacion < 1 || request.Calificacion > 5)
                    return Results.BadRequest(new { error = "La calificación debe estar entre 1 y 5 estrellas." });

                var reserva = await db.Reservas
                    .Include(r => r.Cancha)
                    .Include(r => r.Usuario)
                    .FirstOrDefaultAsync(r => r.Id == request.ReservaId);

                if (reserva is null)
                    return Results.BadRequest(new { error = "La reserva especificada no existe." });

                if (currentRole == "Cliente" && reserva.UsuarioId != currentUserId.Value)
                    return Results.Forbid();

                var yaExiste = await db.Resenas.AnyAsync(r => r.ReservaId == request.ReservaId);
                if (yaExiste)
                    return Results.Conflict(new { error = "Esta reserva ya cuenta con una reseña asociada (relación 1:1)." });

                var resena = new Resena
                {
                    ReservaId = request.ReservaId,
                    UsuarioId = reserva.UsuarioId,
                    Calificacion = request.Calificacion,
                    Comentario = request.Comentario,
                    Fecha = DateTime.UtcNow
                };

                db.Resenas.Add(resena);
                await db.SaveChangesAsync();

                var response = new ResenaResponse(
                    resena.Id,
                    resena.ReservaId,
                    resena.UsuarioId,
                    reserva.Usuario.Nombre,
                    reserva.CanchaId,
                    reserva.Cancha.Numero,
                    resena.Calificacion,
                    resena.Comentario,
                    resena.Fecha
                );

                return Results.Created($"/api/v1/resenas/{resena.Id}", response);
            }).RequireAuthorization();

            grupo.MapPut("/{id:int}", async (int id, ResenaUpdateRequest request, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var resena = await db.Resenas
                    .Include(r => r.Usuario)
                    .Include(r => r.Reserva)
                        .ThenInclude(res => res.Cancha)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (resena is null)
                    return Results.NotFound(new { error = "Reseña no encontrada." });

                if (currentRole != "Administrador" && resena.UsuarioId != currentUserId)
                    return Results.Forbid();

                if (request.Calificacion.HasValue)
                {
                    if (request.Calificacion.Value < 1 || request.Calificacion.Value > 5)
                        return Results.BadRequest(new { error = "La calificación debe estar entre 1 y 5 estrellas." });
                    resena.Calificacion = request.Calificacion.Value;
                }

                if (request.Comentario != null)
                    resena.Comentario = request.Comentario;

                await db.SaveChangesAsync();

                var response = new ResenaResponse(
                    resena.Id,
                    resena.ReservaId,
                    resena.UsuarioId,
                    resena.Usuario.Nombre,
                    resena.Reserva.CanchaId,
                    resena.Reserva.Cancha.Numero,
                    resena.Calificacion,
                    resena.Comentario,
                    resena.Fecha
                );

                return Results.Ok(response);
            }).RequireAuthorization();

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var resena = await db.Resenas.FindAsync(id);
                if (resena is null)
                    return Results.NotFound(new { error = "Reseña no encontrada." });

                if (currentRole != "Administrador" && resena.UsuarioId != currentUserId)
                    return Results.Forbid();

                db.Resenas.Remove(resena);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization();
        }

        private static int? GetUserId(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        public record ResenaCreateRequest(int ReservaId, byte Calificacion, string? Comentario);
        public record ResenaUpdateRequest(byte? Calificacion, string? Comentario);
        public record ResenaResponse(
            int Id,
            int ReservaId,
            int UsuarioId,
            string UsuarioNombre,
            int CanchaId,
            int CanchaNumero,
            byte Calificacion,
            string? Comentario,
            DateTime Fecha
        );
    }
}
