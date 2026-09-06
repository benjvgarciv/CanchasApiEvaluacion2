using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Trabajador
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Cargo { get; set; } = null!;

    public string Turno { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
}
