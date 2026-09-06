using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Endpoints
{
    public static class TipoCanchaApi
    {
        public static void MapTipoCanchaApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/tipos-cancha").WithTags("TiposCancha");

            grupo.MapGet("/", async (ReservasCanchasDbContext db) =>
            {
                var tipos = await db.TiposCancha
                    .Select(t => new TipoCanchaResponse(t.Id, t.Nombre, t.Descripcion, t.PrecioPorHora))
                    .ToListAsync();

                return Results.Ok(tipos);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var tipo = await db.TiposCancha
                    .Where(t => t.Id == id)
                    .Select(t => new TipoCanchaResponse(t.Id, t.Nombre, t.Descripcion, t.PrecioPorHora))
                    .FirstOrDefaultAsync();

                return tipo is not null
                    ? Results.Ok(tipo)
                    : Results.NotFound(new { error = "Tipo de cancha no encontrado." });
            });

            grupo.MapPost("/", async (TipoCanchaRequest request, ReservasCanchasDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Nombre) || request.PrecioPorHora <= 0)
                    return Results.BadRequest(new { error = "Nombre y PrecioPorHora (> 0) son requeridos." });

                var tipo = new TipoCancha
                {
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    PrecioPorHora = request.PrecioPorHora
                };

                db.TiposCancha.Add(tipo);
                await db.SaveChangesAsync();

                var response = new TipoCanchaResponse(tipo.Id, tipo.Nombre, tipo.Descripcion, tipo.PrecioPorHora);
                return Results.Created($"/api/v1/tipos-cancha/{tipo.Id}", response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapPut("/{id:int}", async (int id, TipoCanchaRequest request, ReservasCanchasDbContext db) =>
            {
                var tipo = await db.TiposCancha.FindAsync(id);
                if (tipo is null)
                    return Results.NotFound(new { error = "Tipo de cancha no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Nombre))
                    tipo.Nombre = request.Nombre;

                tipo.Descripcion = request.Descripcion;

                if (request.PrecioPorHora > 0)
                    tipo.PrecioPorHora = request.PrecioPorHora;

                await db.SaveChangesAsync();

                var response = new TipoCanchaResponse(tipo.Id, tipo.Nombre, tipo.Descripcion, tipo.PrecioPorHora);
                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var tipo = await db.TiposCancha.FindAsync(id);
                if (tipo is null)
                    return Results.NotFound(new { error = "Tipo de cancha no encontrado." });

                db.TiposCancha.Remove(tipo);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));
        }

        public record TipoCanchaRequest(string Nombre, string? Descripcion, decimal PrecioPorHora);
        public record TipoCanchaResponse(int Id, string Nombre, string? Descripcion, decimal PrecioPorHora);
    }
}
