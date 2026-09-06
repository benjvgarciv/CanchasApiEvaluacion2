using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class TipoCancha
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal PrecioPorHora { get; set; }

    public virtual ICollection<Cancha> Canchas { get; set; } = new List<Cancha>();
}
