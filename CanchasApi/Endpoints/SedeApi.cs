using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Endpoints
{
    public static class SedeApi
    {
        public static void MapSedeApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/sedes").WithTags("Sedes");

            grupo.MapGet("/", async (ReservasCanchasDbContext db) =>
            {
                var sedes = await db.Sedes
                    .Select(s => new SedeResponse(s.Id, s.Nombre, s.Direccion, s.Telefono))
                    .ToListAsync();

                return Results.Ok(sedes);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var sede = await db.Sedes
                    .Where(s => s.Id == id)
                    .Select(s => new SedeResponse(s.Id, s.Nombre, s.Direccion, s.Telefono))
                    .FirstOrDefaultAsync();

                return sede is not null
                    ? Results.Ok(sede)
                    : Results.NotFound(new { error = "Sede no encontrada." });
            });

            grupo.MapPost("/", async (SedeRequest request, ReservasCanchasDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Direccion))
                    return Results.BadRequest(new { error = "Nombre y Direccion son requeridos." });

                var sede = new Sede
                {
                    Nombre = request.Nombre,
                    Direccion = request.Direccion,
                    Telefono = request.Telefono
                };

                db.Sedes.Add(sede);
                await db.SaveChangesAsync();

                var response = new SedeResponse(sede.Id, sede.Nombre, sede.Direccion, sede.Telefono);
                return Results.Created($"/api/v1/sedes/{sede.Id}", response);
            }).RequireAuthorization(p => p.RequireRole("Administrador"));

            grupo.MapPut("/{id:int}", async (int id, SedeRequest request, ReservasCanchasDbContext db) =>
            {
                var sede = await db.Sedes.FindAsync(id);
                if (sede is null)
                    return Results.NotFound(new { error = "Sede no encontrada." });

                if (!string.IsNullOrWhiteSpace(request.Nombre))
                    sede.Nombre = request.Nombre;

                if (!string.IsNullOrWhiteSpace(request.Direccion))
                    sede.Direccion = request.Direccion;

                if (request.Telefono != null)
                    sede.Telefono = request.Telefono;

                await db.SaveChangesAsync();

                var response = new SedeResponse(sede.Id, sede.Nombre, sede.Direccion, sede.Telefono);
                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Administrador"));

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var sede = await db.Sedes.FindAsync(id);
                if (sede is null)
                    return Results.NotFound(new { error = "Sede no encontrada." });

                db.Sedes.Remove(sede);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        public record SedeRequest(string Nombre, string Direccion, string? Telefono);
        public record SedeResponse(int Id, string Nombre, string Direccion, string? Telefono);
    }
}
