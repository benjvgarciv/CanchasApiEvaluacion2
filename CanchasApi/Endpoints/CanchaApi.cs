using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Endpoints
{
    public static class CanchaApi
    {
        private static readonly string[] EstadosPermitidos = { "Disponible", "Mantenimiento", "Fuera de servicio" };

        public static void MapCanchaApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/canchas").WithTags("Canchas");

            grupo.MapGet("/", async (int? sedeId, int? tipoCanchaId, string? estado, ReservasCanchasDbContext db) =>
            {
                var query = db.Canchas
                    .Include(c => c.Sede)
                    .Include(c => c.TipoCancha)
                    .AsQueryable();

                if (sedeId.HasValue)
                    query = query.Where(c => c.SedeId == sedeId.Value);

                if (tipoCanchaId.HasValue)
                    query = query.Where(c => c.TipoCanchaId == tipoCanchaId.Value);

                if (!string.IsNullOrWhiteSpace(estado))
                    query = query.Where(c => c.Estado == estado);

                var canchas = await query
                    .Select(c => new CanchaResponse(
                        c.Id,
                        c.Numero,
                        c.SedeId,
                        c.Sede.Nombre,
                        c.TipoCanchaId,
                        c.TipoCancha.Nombre,
                        c.TipoCancha.PrecioPorHora,
                        c.Estado
                    ))
                    .ToListAsync();

                return Results.Ok(canchas);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var cancha = await db.Canchas
                    .Include(c => c.Sede)
                    .Include(c => c.TipoCancha)
                    .Where(c => c.Id == id)
                    .Select(c => new CanchaResponse(
                        c.Id,
                        c.Numero,
                        c.SedeId,
                        c.Sede.Nombre,
                        c.TipoCanchaId,
                        c.TipoCancha.Nombre,
                        c.TipoCancha.PrecioPorHora,
                        c.Estado
                    ))
                    .FirstOrDefaultAsync();

                return cancha is not null
                    ? Results.Ok(cancha)
                    : Results.NotFound(new { error = "Cancha no encontrada." });
            });

            grupo.MapPost("/", async (CanchaCreateRequest request, ReservasCanchasDbContext db) =>
            {
                if (request.Numero <= 0)
                    return Results.BadRequest(new { error = "El número de cancha debe ser mayor a 0." });

                var sede = await db.Sedes.FindAsync(request.SedeId);
                if (sede is null)
                    return Results.BadRequest(new { error = "La sede especificada no existe." });

                var tipo = await db.TiposCancha.FindAsync(request.TipoCanchaId);
                if (tipo is null)
                    return Results.BadRequest(new { error = "El tipo de cancha especificado no existe." });

                var estado = string.IsNullOrWhiteSpace(request.Estado) ? "Disponible" : request.Estado;
                if (!EstadosPermitidos.Contains(estado))
                    return Results.BadRequest(new { error = $"Estado no válido. Opciones: {string.Join(", ", EstadosPermitidos)}" });

                var cancha = new Cancha
                {
                    Numero = request.Numero,
                    SedeId = request.SedeId,
                    TipoCanchaId = request.TipoCanchaId,
                    Estado = estado
                };

                db.Canchas.Add(cancha);
                await db.SaveChangesAsync();

                var response = new CanchaResponse(
                    cancha.Id,
                    cancha.Numero,
                    cancha.SedeId,
                    sede.Nombre,
                    cancha.TipoCanchaId,
                    tipo.Nombre,
                    tipo.PrecioPorHora,
                    cancha.Estado
                );

                return Results.Created($"/api/v1/canchas/{cancha.Id}", response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapPut("/{id:int}", async (int id, CanchaUpdateRequest request, ReservasCanchasDbContext db) =>
            {
                var cancha = await db.Canchas
                    .Include(c => c.Sede)
                    .Include(c => c.TipoCancha)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cancha is null)
                    return Results.NotFound(new { error = "Cancha no encontrada." });

                if (request.Numero.HasValue && request.Numero.Value > 0)
                    cancha.Numero = request.Numero.Value;

                if (request.SedeId.HasValue)
                {
                    var sede = await db.Sedes.FindAsync(request.SedeId.Value);
                    if (sede is null)
                        return Results.BadRequest(new { error = "La sede especificada no existe." });
                    cancha.SedeId = request.SedeId.Value;
                }

                if (request.TipoCanchaId.HasValue)
                {
                    var tipo = await db.TiposCancha.FindAsync(request.TipoCanchaId.Value);
                    if (tipo is null)
                        return Results.BadRequest(new { error = "El tipo de cancha especificado no existe." });
                    cancha.TipoCanchaId = request.TipoCanchaId.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Estado))
                {
                    if (!EstadosPermitidos.Contains(request.Estado))
                        return Results.BadRequest(new { error = $"Estado no válido. Opciones: {string.Join(", ", EstadosPermitidos)}" });
                    cancha.Estado = request.Estado;
                }

                await db.SaveChangesAsync();

                await db.Entry(cancha).Reference(c => c.Sede).LoadAsync();
                await db.Entry(cancha).Reference(c => c.TipoCancha).LoadAsync();

                var response = new CanchaResponse(
                    cancha.Id,
                    cancha.Numero,
                    cancha.SedeId,
                    cancha.Sede.Nombre,
                    cancha.TipoCanchaId,
                    cancha.TipoCancha.Nombre,
                    cancha.TipoCancha.PrecioPorHora,
                    cancha.Estado
                );

                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var cancha = await db.Canchas.FindAsync(id);
                if (cancha is null)
                    return Results.NotFound(new { error = "Cancha no encontrada." });

                db.Canchas.Remove(cancha);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));
        }

        public record CanchaCreateRequest(int Numero, int SedeId, int TipoCanchaId, string? Estado);
        public record CanchaUpdateRequest(int? Numero, int? SedeId, int? TipoCanchaId, string? Estado);
        public record CanchaResponse(int Id, int Numero, int SedeId, string SedeNombre, int TipoCanchaId, string TipoCanchaNombre, decimal PrecioPorHora, string Estado);
    }
}
