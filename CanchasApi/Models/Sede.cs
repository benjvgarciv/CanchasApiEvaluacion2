using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Sede
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string? Telefono { get; set; }

    public virtual ICollection<Cancha> Canchas { get; set; } = new List<Cancha>();
}
