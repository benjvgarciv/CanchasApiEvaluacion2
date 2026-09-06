using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanchasApi.Endpoints
{
    public static class PagoApi
    {
        private static readonly string[] MetodosValidos = { "Efectivo", "Tarjeta", "Transferencia" };
        private static readonly string[] EstadosValidos = { "Pendiente", "Pagado", "Reembolsado" };

        public static void MapPagoApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/pagos")
                .WithTags("Pagos")
                .RequireAuthorization();

            grupo.MapGet("/", async (int? reservaId, string? estado, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var query = db.Pagos
                    .Include(p => p.Reserva)
                        .ThenInclude(r => r.Usuario)
                    .AsQueryable();

                if (currentRole == "Cliente")
                {
                    query = query.Where(p => p.Reserva.UsuarioId == currentUserId);
                }

                if (reservaId.HasValue)
                    query = query.Where(p => p.ReservaId == reservaId.Value);

                if (!string.IsNullOrWhiteSpace(estado))
                    query = query.Where(p => p.Estado == estado);

                var pagos = await query
                    .OrderByDescending(p => p.FechaPago ?? DateTime.MinValue)
                    .Select(p => new PagoResponse(
                        p.Id,
                        p.ReservaId,
                        p.Reserva.UsuarioId,
                        p.Reserva.Usuario.Nombre,
                        p.Monto,
                        p.MetodoPago,
                        p.Estado,
                        p.FechaPago
                    ))
                    .ToListAsync();

                return Results.Ok(pagos);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var pago = await db.Pagos
                    .Include(p => p.Reserva)
                        .ThenInclude(r => r.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pago is null)
                    return Results.NotFound(new { error = "Pago no encontrado." });

                if (currentRole == "Cliente" && pago.Reserva.UsuarioId != currentUserId)
                    return Results.Forbid();

                var response = new PagoResponse(
                    pago.Id,
                    pago.ReservaId,
                    pago.Reserva.UsuarioId,
                    pago.Reserva.Usuario.Nombre,
                    pago.Monto,
                    pago.MetodoPago,
                    pago.Estado,
                    pago.FechaPago
                );

                return Results.Ok(response);
            });

            grupo.MapPost("/", async (PagoCreateRequest request, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (request.Monto <= 0)
                    return Results.BadRequest(new { error = "El monto debe ser mayor a 0." });

                if (!MetodosValidos.Contains(request.MetodoPago))
                    return Results.BadRequest(new { error = $"Método de pago no válido. Opciones: {string.Join(", ", MetodosValidos)}" });

                var reserva = await db.Reservas.Include(r => r.Usuario).FirstOrDefaultAsync(r => r.Id == request.ReservaId);
                if (reserva is null)
                    return Results.BadRequest(new { error = "La reserva especificada no existe." });

                if (currentRole == "Cliente" && reserva.UsuarioId != currentUserId)
                    return Results.Forbid();

                var estado = (currentRole is "Trabajador" or "Administrador") ? "Pagado" : "Pendiente";

                var nuevoPago = new Pago
                {
                    ReservaId = request.ReservaId,
                    Monto = request.Monto,
                    MetodoPago = request.MetodoPago,
                    Estado = estado,
                    FechaPago = DateTime.UtcNow
                };

                db.Pagos.Add(nuevoPago);

                if (estado == "Pagado" && reserva.Estado == "Pendiente")
                {
                    reserva.Estado = "Confirmada";
                }

                await db.SaveChangesAsync();

                var response = new PagoResponse(
                    nuevoPago.Id,
                    nuevoPago.ReservaId,
                    reserva.UsuarioId,
                    reserva.Usuario.Nombre,
                    nuevoPago.Monto,
                    nuevoPago.MetodoPago,
                    nuevoPago.Estado,
                    nuevoPago.FechaPago
                );

                return Results.Created($"/api/v1/pagos/{nuevoPago.Id}", response);
            });

            grupo.MapPut("/{id:int}", async (int id, PagoUpdateRequest request, ReservasCanchasDbContext db) =>
            {
                var pago = await db.Pagos
                    .Include(p => p.Reserva)
                        .ThenInclude(r => r.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pago is null)
                    return Results.NotFound(new { error = "Pago no encontrado." });

                if (!string.IsNullOrWhiteSpace(request.Estado))
                {
                    if (!EstadosValidos.Contains(request.Estado))
                        return Results.BadRequest(new { error = $"Estado no válido. Opciones: {string.Join(", ", EstadosValidos)}" });

                    pago.Estado = request.Estado;

                    if (pago.Estado == "Pagado")
                    {
                        pago.FechaPago ??= DateTime.UtcNow;
                        if (pago.Reserva.Estado == "Pendiente")
                            pago.Reserva.Estado = "Confirmada";
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.MetodoPago))
                {
                    if (!MetodosValidos.Contains(request.MetodoPago))
                        return Results.BadRequest(new { error = $"Método de pago no válido. Opciones: {string.Join(", ", MetodosValidos)}" });
                    pago.MetodoPago = request.MetodoPago;
                }

                await db.SaveChangesAsync();

                var response = new PagoResponse(
                    pago.Id,
                    pago.ReservaId,
                    pago.Reserva.UsuarioId,
                    pago.Reserva.Usuario.Nombre,
                    pago.Monto,
                    pago.MetodoPago,
                    pago.Estado,
                    pago.FechaPago
                );

                return Results.Ok(response);
            }).RequireAuthorization(p => p.RequireRole("Trabajador", "Administrador"));

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var pago = await db.Pagos.FindAsync(id);
                if (pago is null)
                    return Results.NotFound(new { error = "Pago no encontrado." });

                db.Pagos.Remove(pago);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static int? GetUserId(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        public record PagoCreateRequest(int ReservaId, decimal Monto, string MetodoPago);
        public record PagoUpdateRequest(string? Estado, string? MetodoPago);
        public record PagoResponse(
            int Id,
            int ReservaId,
            int UsuarioId,
            string UsuarioNombre,
            decimal Monto,
            string MetodoPago,
            string Estado,
            DateTime? FechaPago
        );
    }
}
