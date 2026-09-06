using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual Trabajador? Trabajador { get; set; }

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
}
