using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanchasApi.Endpoints
{
    public static class ComprobanteApi
    {
        private static readonly string[] TiposValidos = { "Boleta", "Factura" };

        public static void MapComprobanteApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/comprobantes")
                .WithTags("Comprobantes")
                .RequireAuthorization();

            grupo.MapGet("/", async (ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var query = db.Comprobantes
                    .Include(c => c.Pago)
                        .ThenInclude(p => p.Reserva)
                            .ThenInclude(r => r.Usuario)
                    .AsQueryable();

                if (currentRole == "Cliente")
                {
                    query = query.Where(c => c.Pago.Reserva.UsuarioId == currentUserId);
                }

                var comprobantes = await query
                    .OrderByDescending(c => c.FechaEmision)
                    .Select(c => new ComprobanteResponse(
                        c.Id,
                        c.PagoId,
                        c.Pago.ReservaId,
                        c.Pago.Reserva.Usuario.Nombre,
                        c.NumeroComprobante,
                        c.Tipo,
                        c.Pago.Monto,
                        c.FechaEmision
                    ))
                    .ToListAsync();

                return Results.Ok(comprobantes);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var comprobante = await db.Comprobantes
                    .Include(c => c.Pago)
                        .ThenInclude(p => p.Reserva)
                            .ThenInclude(r => r.Usuario)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comprobante is null)
                    return Results.NotFound(new { error = "Comprobante no encontrado." });

                if (currentRole == "Cliente" && comprobante.Pago.Reserva.UsuarioId != currentUserId)
                    return Results.Forbid();

                var response = new ComprobanteResponse(
                    comprobante.Id,
                    comprobante.PagoId,
                    comprobante.Pago.ReservaId,
                    comprobante.Pago.Reserva.Usuario.Nombre,
                    comprobante.NumeroComprobante,
                    comprobante.Tipo,
                    comprobante.Pago.Monto,
                    comprobante.FechaEmision
                );

                return Results.Ok(response);
            });

            grupo.MapPost("/", async (ComprobanteCreateRequest request, ReservasCanchasDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.NumeroComprobante))
                    return Results.BadRequest(new { error = "El número de comprobante es requerido." });

                if (!TiposValidos.Contains(request.Tipo))
                    return Results.BadRequest(new { error = $"Tipo de comprobante no válido. Opciones: {string.Join(", ", TiposValidos)}" });

                var pago = await db.Pagos
                    .Include(p => p.Reserva)
                        .ThenInclude(r => r.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == request.PagoId);

                if (pago is null)
                    return Results.BadRequest(new { error = "El pago especificado no existe." });

                var yaTieneComprobante = await db.Comprobantes.AnyAsync(c => c.PagoId == request.PagoId);
                if (yaTieneComprobante)
                    return Results.Conflict(new { error = "El pago ya cuenta con un comprobante emitido (relación 1:1)." });

                var comprobante = new Comprobante
                {
                    PagoId = request.PagoId,
                    NumeroComprobante = request.NumeroComprobante,
                    Tipo = request.Tipo,
                    FechaEmision = DateTime.UtcNow
                };

                db.Comprobantes.Add(comprobante);
                await db.SaveChangesAsync();

                var response = new ComprobanteResponse(
                    comprobante.Id,
                    comprobante.PagoId,
                    pago.ReservaId,
                    pago.Reserva.Usuario.Nombre,
                    comprobante.NumeroComprobante,
                    comprobante.Tipo,
                    pago.Monto,
                    comprobante.FechaEmision
                );

                return Results.Created($"/api/v1/comprobantes/{comprobante.Id}", response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapPut("/{id:int}", async (int id, ComprobanteUpdateRequest request, ReservasCanchasDbContext db) =>
            {
                var comprobante = await db.Comprobantes
                    .Include(c => c.Pago)
                        .ThenInclude(p => p.Reserva)
                            .ThenInclude(r => r.Usuario)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comprobante is null)
                    return Results.NotFound(new { error = "Comprobante no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.NumeroComprobante))
                    comprobante.NumeroComprobante = request.NumeroComprobante;

                if (!string.IsNullOrWhiteSpace(request.Tipo))
                {
                    if (!TiposValidos.Contains(request.Tipo))
                        return Results.BadRequest(new { error = $"Tipo de comprobante no válido. Opciones: {string.Join(", ", TiposValidos)}" });
                    comprobante.Tipo = request.Tipo;
                }

                await db.SaveChangesAsync();

                var response = new ComprobanteResponse(
                    comprobante.Id,
                    comprobante.PagoId,
                    comprobante.Pago.ReservaId,
                    comprobante.Pago.Reserva.Usuario.Nombre,
                    comprobante.NumeroComprobante,
                    comprobante.Tipo,
                    comprobante.Pago.Monto,
                    comprobante.FechaEmision
                );

                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var comprobante = await db.Comprobantes.FindAsync(id);
                if (comprobante is null)
                    return Results.NotFound(new { error = "Comprobante no encontrado." });

                db.Comprobantes.Remove(comprobante);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static int? GetUserId(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        public record ComprobanteCreateRequest(int PagoId, string NumeroComprobante, string Tipo);
        public record ComprobanteUpdateRequest(string? NumeroComprobante, string? Tipo);
        public record ComprobanteResponse(
            int Id,
            int PagoId,
            int ReservaId,
            string UsuarioNombre,
            string NumeroComprobante,
            string Tipo,
            decimal Monto,
            DateTime FechaEmision
        );
    }
}
