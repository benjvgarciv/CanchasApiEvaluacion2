using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanchasApi.Endpoints
{
    public static class ReservaApi
    {
        private static readonly string[] EstadosValidos = { "Pendiente", "Confirmada", "Cancelada", "Completada" };

        public static void MapReservaApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/reservas")
                .WithTags("Reservas")
                .RequireAuthorization();

            grupo.MapGet("/", async (int? canchaId, string? estado, int? page, int? pageSize, ReservasCanchasDbContext db, ClaimsPrincipal user, HttpContext httpContext) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var query = db.Reservas
                    .Include(r => r.Usuario)
                    .Include(r => r.Cancha)
                    .Include(r => r.Trabajador)
                        .ThenInclude(t => t!.Usuario)
                    .AsQueryable();

                // El cliente solo puede consultar sus propias reservas
                if (currentRole == "Cliente")
                {
                    query = query.Where(r => r.UsuarioId == currentUserId);
                }

                if (canchaId.HasValue)
                    query = query.Where(r => r.CanchaId == canchaId.Value);

                if (!string.IsNullOrWhiteSpace(estado))
                    query = query.Where(r => r.Estado == estado);

                var totalCount = await query.CountAsync();
                httpContext.Response.Headers["X-Total-Count"] = totalCount.ToString();

                var orderedQuery = query.OrderByDescending(r => r.FechaInicio);

                if (page.HasValue && page.Value > 0 && pageSize.HasValue && pageSize.Value > 0)
                {
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);
                    httpContext.Response.Headers["X-Page"] = page.Value.ToString();
                    httpContext.Response.Headers["X-Page-Size"] = pageSize.Value.ToString();
                    httpContext.Response.Headers["X-Total-Pages"] = totalPages.ToString();

                    var pagedReservas = await orderedQuery
                        .Skip((page.Value - 1) * pageSize.Value)
                        .Take(pageSize.Value)
                        .Select(r => new ReservaResponse(
                            r.Id,
                            r.UsuarioId,
                            r.Usuario.Nombre,
                            r.Usuario.Email,
                            r.CanchaId,
                            r.Cancha.Numero,
                            r.TrabajadorId,
                            r.Trabajador != null ? r.Trabajador.Usuario.Nombre : null,
                            r.FechaInicio,
                            r.FechaFin,
                            r.Estado
                        ))
                        .ToListAsync();

                    return Results.Ok(pagedReservas);
                }

                var reservas = await orderedQuery
                    .Select(r => new ReservaResponse(
                        r.Id,
                        r.UsuarioId,
                        r.Usuario.Nombre,
                        r.Usuario.Email,
                        r.CanchaId,
                        r.Cancha.Numero,
                        r.TrabajadorId,
                        r.Trabajador != null ? r.Trabajador.Usuario.Nombre : null,
                        r.FechaInicio,
                        r.FechaFin,
                        r.Estado
                    ))
                    .ToListAsync();

                return Results.Ok(reservas);
            });

            grupo.MapGet("/{id:int}", async (int id, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var reserva = await db.Reservas
                    .Include(r => r.Usuario)
                    .Include(r => r.Cancha)
                    .Include(r => r.Trabajador)
                        .ThenInclude(t => t!.Usuario)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reserva is null)
                    return Results.NotFound(new { error = "Reserva no encontrada." });

                if (currentRole == "Cliente" && reserva.UsuarioId != currentUserId)
                    return Results.Forbid();

                var response = new ReservaResponse(
                    reserva.Id,
                    reserva.UsuarioId,
                    reserva.Usuario.Nombre,
                    reserva.Usuario.Email,
                    reserva.CanchaId,
                    reserva.Cancha.Numero,
                    reserva.TrabajadorId,
                    reserva.Trabajador?.Usuario.Nombre,
                    reserva.FechaInicio,
                    reserva.FechaFin,
                    reserva.Estado
                );

                return Results.Ok(response);
            });

            grupo.MapPost("/", async (ReservaCreateRequest request, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (request.FechaInicio >= request.FechaFin)
                    return Results.BadRequest(new { error = "La fecha y hora de inicio debe ser anterior a la de fin." });

                var cancha = await db.Canchas.FindAsync(request.CanchaId);
                if (cancha is null)
                    return Results.BadRequest(new { error = "La cancha especificada no existe." });

                if (cancha.Estado != "Disponible")
                    return Results.BadRequest(new { error = $"La cancha no está disponible (Estado actual: {cancha.Estado})." });

                // Asignar el dueño de la reserva
                int usuarioId;
                if (currentRole == "Cliente")
                {
                    if (!currentUserId.HasValue)
                        return Results.Unauthorized();
                    usuarioId = currentUserId.Value;
                }
                else
                {
                    usuarioId = request.UsuarioId ?? currentUserId!.Value;
                }

                // Verificar superposición horaria en la misma cancha
                var hayConflicto = await db.Reservas.AnyAsync(r =>
                    r.CanchaId == request.CanchaId &&
                    r.Estado != "Cancelada" &&
                    r.FechaInicio < request.FechaFin &&
                    r.FechaFin > request.FechaInicio);

                if (hayConflicto)
                    return Results.Conflict(new { error = "La cancha ya se encuentra reservada en el rango horario solicitado." });

                int? trabajadorId = (currentRole is "Trabajador" or "Administrador") ? request.TrabajadorId : null;

                var nuevaReserva = new Reserva
                {
                    UsuarioId = usuarioId,
                    CanchaId = request.CanchaId,
                    TrabajadorId = trabajadorId,
                    FechaInicio = request.FechaInicio,
                    FechaFin = request.FechaFin,
                    Estado = "Pendiente"
                };

                db.Reservas.Add(nuevaReserva);
                await db.SaveChangesAsync();

                // Cargar datos para la respuesta
                await db.Entry(nuevaReserva).Reference(r => r.Usuario).LoadAsync();
                await db.Entry(nuevaReserva).Reference(r => r.Cancha).LoadAsync();
                if (nuevaReserva.TrabajadorId.HasValue)
                {
                    await db.Entry(nuevaReserva).Reference(r => r.Trabajador).LoadAsync();
                    if (nuevaReserva.Trabajador != null)
                        await db.Entry(nuevaReserva.Trabajador).Reference(t => t.Usuario).LoadAsync();
                }

                var response = new ReservaResponse(
                    nuevaReserva.Id,
                    nuevaReserva.UsuarioId,
                    nuevaReserva.Usuario.Nombre,
                    nuevaReserva.Usuario.Email,
                    nuevaReserva.CanchaId,
                    nuevaReserva.Cancha.Numero,
                    nuevaReserva.TrabajadorId,
                    nuevaReserva.Trabajador?.Usuario.Nombre,
                    nuevaReserva.FechaInicio,
                    nuevaReserva.FechaFin,
                    nuevaReserva.Estado
                );

                return Results.Created($"/api/v1/reservas/{nuevaReserva.Id}", response);
            });

            grupo.MapPut("/{id:int}", async (int id, ReservaUpdateRequest request, ReservasCanchasDbContext db, ClaimsPrincipal user) =>
            {
                var currentUserId = GetUserId(user);
                var currentRole = user.FindFirst(ClaimTypes.Role)?.Value;

                var reserva = await db.Reservas
                    .Include(r => r.Usuario)
                    .Include(r => r.Cancha)
                    .Include(r => r.Trabajador)
                        .ThenInclude(t => t!.Usuario)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reserva is null)
                    return Results.NotFound(new { error = "Reserva no encontrada." });

                // Regla de transición de estados: estados terminales no se pueden reactivar ni alterar
                if (reserva.Estado == "Cancelada")
                    return Results.BadRequest(new { error = "No se puede modificar una reserva que ya ha sido cancelada." });

                if (reserva.Estado == "Completada")
                    return Results.BadRequest(new { error = "No se puede modificar una reserva que ya ha sido completada." });

                if (currentRole == "Cliente")
                {
                    if (reserva.UsuarioId != currentUserId)
                        return Results.Forbid();

                    // Cliente solo puede cancelar
                    if (request.Estado == "Cancelada")
                    {
                        reserva.Estado = "Cancelada";
                    }
                    else
                    {
                        return Results.BadRequest(new { error = "Los clientes solo pueden cambiar el estado a 'Cancelada'." });
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.Estado))
                    {
                        if (!EstadosValidos.Contains(request.Estado))
                            return Results.BadRequest(new { error = $"Estado no válido. Opciones: {string.Join(", ", EstadosValidos)}" });
                        reserva.Estado = request.Estado;
                    }

                    if (request.FechaInicio.HasValue && request.FechaFin.HasValue)
                    {
                        if (request.FechaInicio.Value >= request.FechaFin.Value)
                            return Results.BadRequest(new { error = "La fecha y hora de inicio debe ser anterior a la de fin." });

                        var conflicto = await db.Reservas.AnyAsync(r =>
                            r.Id != id &&
                            r.CanchaId == reserva.CanchaId &&
                            r.Estado != "Cancelada" &&
                            r.FechaInicio < request.FechaFin.Value &&
                            r.FechaFin > request.FechaInicio.Value);

                        if (conflicto)
                            return Results.Conflict(new { error = "Horario ocupado por otra reserva." });

                        reserva.FechaInicio = request.FechaInicio.Value;
                        reserva.FechaFin = request.FechaFin.Value;
                    }

                    if (request.TrabajadorId.HasValue)
                    {
                        var trabajador = await db.Trabajadores
                            .Include(t => t.Usuario)
                            .FirstOrDefaultAsync(t => t.Id == request.TrabajadorId.Value || t.UsuarioId == request.TrabajadorId.Value);

                        if (trabajador is null)
                        {
                            // Si el usuario existe pero no tiene registro en Trabajadores aún, crearlo automáticamente
                            var usuarioTrabajador = await db.Usuarios.FindAsync(request.TrabajadorId.Value);
                            if (usuarioTrabajador != null && (usuarioTrabajador.Rol == "Trabajador" || usuarioTrabajador.Rol == "Administrador"))
                            {
                                trabajador = new Trabajador
                                {
                                    UsuarioId = usuarioTrabajador.Id,
                                    Cargo = usuarioTrabajador.Rol,
                                    Turno = "Completo"
                                };
                                db.Trabajadores.Add(trabajador);
                                await db.SaveChangesAsync();
                            }
                        }

                        if (trabajador != null)
                        {
                            reserva.TrabajadorId = trabajador.Id;
                        }
                    }
                }

                await db.SaveChangesAsync();

                if (reserva.TrabajadorId.HasValue)
                {
                    await db.Entry(reserva).Reference(r => r.Trabajador).Query().Include(t => t.Usuario).LoadAsync();
                }

                var response = new ReservaResponse(
                    reserva.Id,
                    reserva.UsuarioId,
                    reserva.Usuario.Nombre,
                    reserva.Usuario.Email,
                    reserva.CanchaId,
                    reserva.Cancha.Numero,
                    reserva.TrabajadorId,
                    reserva.Trabajador?.Usuario.Nombre,
                    reserva.FechaInicio,
                    reserva.FechaFin,
                    reserva.Estado
                );

                return Results.Ok(response);
            });

            grupo.MapDelete("/{id:int}", async (int id, ReservasCanchasDbContext db) =>
            {
                var reserva = await db.Reservas.FindAsync(id);
                if (reserva is null)
                    return Results.NotFound(new { error = "Reserva no encontrada." });

                db.Reservas.Remove(reserva);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private static int? GetUserId(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        public record ReservaCreateRequest(int CanchaId, DateTime FechaInicio, DateTime FechaFin, int? UsuarioId, int? TrabajadorId);
        public record ReservaUpdateRequest(DateTime? FechaInicio, DateTime? FechaFin, string? Estado, int? TrabajadorId);
        public record ReservaResponse(
            int Id,
            int UsuarioId,
            string UsuarioNombre,
            string UsuarioEmail,
            int CanchaId,
            int CanchaNumero,
            int? TrabajadorId,
            string? TrabajadorNombre,
            DateTime FechaInicio,
            DateTime FechaFin,
            string Estado
        );
    }
}
