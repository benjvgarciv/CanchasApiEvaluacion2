using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanchasApi.Endpoints
{
    public static class NotificacionApi
    {
        public static void MapNotificacionApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/notificaciones")
                .WithTags("Notificaciones")
                .RequireAuthorization();

            grupo.MapGet("/", async (bool? soloNoLeidas, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var query = db.Notificaciones.AsQueryable();

                if (currentRole == "Cliente")
                {
                    query = query.Where(n => n.UsuarioId == currentUserId);
                }

                if (soloNoLeidas.HasValue && soloNoLeidas.Value)
                    query = query.Where(n => !n.Leido);

                var notificaciones = await query
                    .OrderByDescending(n => n.FechaEnvio)
                    .Select(n => new NotificacionResponse(
                        n.Id,
                        n.UsuarioId,
                        n.Mensaje,
                        n.Leido,
                        n.FechaEnvio
                    ))
                    .ToListAsync();

                return Results.Ok(notificaciones);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var notificacion = await db.Notificaciones.FindAsync(id);
                if (notificacion is null)
                    return Results.NotFound(new { error = "Notificación no encontrada." });

                if (currentRole == "Cliente" && notificacion.UsuarioId != currentUserId)
                    return Results.Forbid();

                var response = new NotificacionResponse(
                    notificacion.Id,
                    notificacion.UsuarioId,
                    notificacion.Mensaje,
                    notificacion.Leido,
                    notificacion.FechaEnvio
                );

                return Results.Ok(response);
            });

            grupo.MapPost("/", async (NotificacionCreateRequest request, ReservasCanchasDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Mensaje))
                    return Results.BadRequest(new { error = "El mensaje es requerido." });

                var usuario = await db.Usuarios.FindAsync(request.UsuarioId);
                if (usuario is null)
                    return Results.BadRequest(new { error = "El usuario especificado no existe." });

                var notificacion = new Notificacion
                {
                    UsuarioId = request.UsuarioId,
                    Mensaje = request.Mensaje,
                    Leido = false,
                    FechaEnvio = DateTime.UtcNow
                };

                db.Notificaciones.Add(notificacion);
                await db.SaveChangesAsync();

                var response = new NotificacionResponse(
                    notificacion.Id,
                    notificacion.UsuarioId,
                    notificacion.Mensaje,
                    notificacion.Leido,
                    notificacion.FechaEnvio
                );

                return Results.Created($"/api/v1/notificaciones/{notificacion.Id}", response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapPut("/{id:int}/marcar-leida", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var notificacion = await db.Notificaciones.FindAsync(id);
                if (notificacion is null)
                    return Results.NotFound(new { error = "Notificación no encontrada." });

                if (currentRole == "Cliente" && notificacion.UsuarioId != currentUserId)
                    return Results.Forbid();

                notificacion.Leido = true;
                await db.SaveChangesAsync();

                var response = new NotificacionResponse(
                    notificacion.Id,
                    notificacion.UsuarioId,
                    notificacion.Mensaje,
                    notificacion.Leido,
                    notificacion.FechaEnvio
                );

                return Results.Ok(response);
            });

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var notificacion = await db.Notificaciones.FindAsync(id);
                if (notificacion is null)
                    return Results.NotFound(new { error = "Notificación no encontrada." });

                if (currentRole != "Administrador" && notificacion.UsuarioId != currentUserId)
                    return Results.Forbid();

                db.Notificaciones.Remove(notificacion);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }

        private static int? GetUserId(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        public record NotificacionCreateRequest(int UsuarioId, string Mensaje);
        public record NotificacionResponse(int Id, int UsuarioId, string Mensaje, bool Leido, DateTime FechaEnvio);
    }
}
