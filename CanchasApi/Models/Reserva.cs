using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Reserva
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int CanchaId { get; set; }

    public int? TrabajadorId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Cancha Cancha { get; set; } = null!;

    public virtual Trabajador? Trabajador { get; set; }

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual Resena? Resena { get; set; }
}
