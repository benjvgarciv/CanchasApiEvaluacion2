using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Notificacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Mensaje { get; set; } = null!;

    public bool Leido { get; set; }

    public DateTime FechaEnvio { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
