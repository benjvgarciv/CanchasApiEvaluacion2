using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Resena
{
    public int Id { get; set; }

    public int ReservaId { get; set; }

    public int UsuarioId { get; set; }

    public byte Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime Fecha { get; set; }

    public virtual Reserva Reserva { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
