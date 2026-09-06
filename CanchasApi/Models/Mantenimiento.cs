using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Mantenimiento
{
    public int Id { get; set; }

    public int CanchaId { get; set; }

    public int TrabajadorId { get; set; }

    public string Descripcion { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public virtual Cancha Cancha { get; set; } = null!;

    public virtual Trabajador Trabajador { get; set; } = null!;
}
