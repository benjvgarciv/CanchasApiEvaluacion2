using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Endpoints
{
    public static class MantenimientoApi
    {
        public static void MapMantenimientoApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/mantenimientos")
                .WithTags("Mantenimientos")
                .RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapGet("/", async (int? canchaId, int? trabajadorId, ReservasCanchasDbContext db) =>
            {
                var query = db.Mantenimientos
                    .Include(m => m.Cancha)
                    .Include(m => m.Trabajador)
                        .ThenInclude(t => t.Usuario)
                    .AsQueryable();

                if (canchaId.HasValue)
                    query = query.Where(m => m.CanchaId == canchaId.Value);

                if (trabajadorId.HasValue)
                    query = query.Where(m => m.TrabajadorId == trabajadorId.Value);

                var mantenimientos = await query
                    .OrderByDescending(m => m.FechaInicio)
                    .Select(m => new MantenimientoResponse(
                        m.Id,
                        m.CanchaId,
                        m.Cancha.Numero,
                        m.TrabajadorId,
                        m.Trabajador.Usuario.Nombre,
                        m.Descripcion,
                        m.FechaInicio,
                        m.FechaFin
                    ))
                    .ToListAsync();

                return Results.Ok(mantenimientos);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var mantenimiento = await db.Mantenimientos
                    .Include(m => m.Cancha)
                    .Include(m => m.Trabajador)
                        .ThenInclude(t => t.Usuario)
                    .Where(m => m.Id == id)
                    .Select(m => new MantenimientoResponse(
                        m.Id,
                        m.CanchaId,
                        m.Cancha.Numero,
                        m.TrabajadorId,
                        m.Trabajador.Usuario.Nombre,
                        m.Descripcion,
                        m.FechaInicio,
                        m.FechaFin
                    ))
                    .FirstOrDefaultAsync();

                return mantenimiento is not null
                    ? Results.Ok(mantenimiento)
                    : Results.NotFound(new { error = "Registro de mantenimiento no encontrado." });
            });

            grupo.MapPost("/", async (MantenimientoCreateRequest request, ReservasCanchasDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Descripcion))
                    return Results.BadRequest(new { error = "La descripción es requerida." });

                var cancha = await db.Canchas.FindAsync(request.CanchaId);
                if (cancha is null)
                    return Results.BadRequest(new { error = "La cancha especificada no existe." });

                var trabajador = await db.Trabajadores.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.Id == request.TrabajadorId);
                if (trabajador is null)
                    return Results.BadRequest(new { error = "El trabajador especificado no existe." });

                var mantenimiento = new Mantenimiento
                {
                    CanchaId = request.CanchaId,
                    TrabajadorId = request.TrabajadorId,
                    Descripcion = request.Descripcion,
                    FechaInicio = request.FechaInicio,
                    FechaFin = request.FechaFin
                };

                // Si está en curso, marcar estado de la cancha como 'Mantenimiento'
                if (!request.FechaFin.HasValue || request.FechaFin.Value > DateTime.UtcNow)
                {
                    cancha.Estado = "Mantenimiento";
                }

                db.Mantenimientos.Add(mantenimiento);
                await db.SaveChangesAsync();

                var response = new MantenimientoResponse(
                    mantenimiento.Id,
                    mantenimiento.CanchaId,
                    cancha.Numero,
                    mantenimiento.TrabajadorId,
                    trabajador.Usuario.Nombre,
                    mantenimiento.Descripcion,
                    mantenimiento.FechaInicio,
                    mantenimiento.FechaFin
                );

                return Results.Created($"/api/v1/mantenimientos/{mantenimiento.Id}", response);
            });

            grupo.MapPut("/{id:int}", async (int id, MantenimientoUpdateRequest request, ReservasCanchasDbContext db) =>
            {
                var mantenimiento = await db.Mantenimientos
                    .Include(m => m.Cancha)
                    .Include(m => m.Trabajador)
                        .ThenInclude(t => t.Usuario)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mantenimiento is null)
                    return Results.NotFound(new { error = "Registro de mantenimiento no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Descripcion))
                    mantenimiento.Descripcion = request.Descripcion;

                if (request.FechaInicio.HasValue)
                    mantenimiento.FechaInicio = request.FechaInicio.Value;

                if (request.FechaFin.HasValue)
                {
                    mantenimiento.FechaFin = request.FechaFin.Value;
                    if (request.FechaFin.Value <= DateTime.UtcNow)
                    {
                        mantenimiento.Cancha.Estado = "Disponible";
                    }
                }

                await db.SaveChangesAsync();

                var response = new MantenimientoResponse(
                    mantenimiento.Id,
                    mantenimiento.CanchaId,
                    mantenimiento.Cancha.Numero,
                    mantenimiento.TrabajadorId,
                    mantenimiento.Trabajador.Usuario.Nombre,
                    mantenimiento.Descripcion,
                    mantenimiento.FechaInicio,
                    mantenimiento.FechaFin
                );

                return Results.Ok(response);
            });

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var mantenimiento = await db.Mantenimientos.FindAsync(id);
                if (mantenimiento is null)
                    return Results.NotFound(new { error = "Registro de mantenimiento no encontrado." });

                db.Mantenimientos.Remove(mantenimiento);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        public record MantenimientoCreateRequest(int CanchaId, int TrabajadorId, string Descripcion, DateTime FechaInicio, DateTime? FechaFin);
        public record MantenimientoUpdateRequest(string? Descripcion, DateTime? FechaInicio, DateTime? FechaFin);
        public record MantenimientoResponse(
            int Id,
            int CanchaId,
            int CanchaNumero,
            int TrabajadorId,
            string TrabajadorNombre,
            string Descripcion,
            DateTime FechaInicio,
            DateTime? FechaFin
        );
    }
}
