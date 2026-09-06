using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Endpoints
{
    public static class HorarioApi
    {
        public static void MapHorarioApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/horarios").WithTags("Horarios");

            grupo.MapGet("/", async (int? canchaId, ReservasCanchasDbContext db) =>
            {
                var query = db.Horarios.Include(h => h.Cancha).AsQueryable();

                if (canchaId.HasValue)
                    query = query.Where(h => h.CanchaId == canchaId.Value);

                var horarios = await query
                    .Select(h => new HorarioResponse(
                        h.Id,
                        h.CanchaId,
                        h.Cancha.Numero,
                        h.DiaSemana,
                        h.HoraApertura,
                        h.HoraCierre
                    ))
                    .ToListAsync();

                return Results.Ok(horarios);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var horario = await db.Horarios
                    .Include(h => h.Cancha)
                    .Where(h => h.Id == id)
                    .Select(h => new HorarioResponse(
                        h.Id,
                        h.CanchaId,
                        h.Cancha.Numero,
                        h.DiaSemana,
                        h.HoraApertura,
                        h.HoraCierre
                    ))
                    .FirstOrDefaultAsync();

                return horario is not null
                    ? Results.Ok(horario)
                    : Results.NotFound(new { error = "Horario no encontrado." });
            });

            grupo.MapPost("/", async (HorarioCreateRequest request, ReservasCanchasDbContext db) =>
            {
                if (request.DiaSemana < 1 || request.DiaSemana > 7)
                    return Results.BadRequest(new { error = "Día no válido. Ingrese un valor entre 1 (Lunes) y 7 (Domingo)." });

                if (request.HoraApertura >= request.HoraCierre)
                    return Results.BadRequest(new { error = "La hora de apertura debe ser menor a la de cierre." });

                var cancha = await db.Canchas.FindAsync(request.CanchaId);
                if (cancha is null)
                    return Results.BadRequest(new { error = "La cancha especificada no existe." });

                var horario = new Horario
                {
                    CanchaId = request.CanchaId,
                    DiaSemana = request.DiaSemana,
                    HoraApertura = request.HoraApertura,
                    HoraCierre = request.HoraCierre
                };

                db.Horarios.Add(horario);
                await db.SaveChangesAsync();

                var response = new HorarioResponse(
                    horario.Id,
                    horario.CanchaId,
                    cancha.Numero,
                    horario.DiaSemana,
                    horario.HoraApertura,
                    horario.HoraCierre
                );

                return Results.Created($"/api/v1/horarios/{horario.Id}", response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapPut("/{id:int}", async (int id, HorarioUpdateRequest request, ReservasCanchasDbContext db) =>
            {
                var horario = await db.Horarios.Include(h => h.Cancha).FirstOrDefaultAsync(h => h.Id == id);
                if (horario is null)
                    return Results.NotFound(new { error = "Horario no encontrado." });

                if (request.DiaSemana.HasValue)
                {
                    if (request.DiaSemana.Value < 1 || request.DiaSemana.Value > 7)
                        return Results.BadRequest(new { error = "Día no válido. Ingrese un valor entre 1 (Lunes) y 7 (Domingo)." });
                    horario.DiaSemana = request.DiaSemana.Value;
                }

                var apertura = request.HoraApertura ?? horario.HoraApertura;
                var cierre = request.HoraCierre ?? horario.HoraCierre;

                if (apertura >= cierre)
                    return Results.BadRequest(new { error = "La hora de apertura debe ser menor a la de cierre." });

                horario.HoraApertura = apertura;
                horario.HoraCierre = cierre;

                await db.SaveChangesAsync();

                var response = new HorarioResponse(
                    horario.Id,
                    horario.CanchaId,
                    horario.Cancha.Numero,
                    horario.DiaSemana,
                    horario.HoraApertura,
                    horario.HoraCierre
                );

                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var horario = await db.Horarios.FindAsync(id);
                if (horario is null)
                    return Results.NotFound(new { error = "Horario no encontrado." });

                db.Horarios.Remove(horario);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));
        }

        public record HorarioCreateRequest(int CanchaId, byte DiaSemana, TimeOnly HoraApertura, TimeOnly HoraCierre);
        public record HorarioUpdateRequest(byte? DiaSemana, TimeOnly? HoraApertura, TimeOnly? HoraCierre);
        public record HorarioResponse(int Id, int CanchaId, int CanchaNumero, byte DiaSemana, TimeOnly HoraApertura, TimeOnly HoraCierre);
    }
}
