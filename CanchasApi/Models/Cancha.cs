using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Cancha
{
    public int Id { get; set; }

    public int Numero { get; set; }

    public int SedeId { get; set; }

    public int TipoCanchaId { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Sede Sede { get; set; } = null!;

    public virtual TipoCancha TipoCancha { get; set; } = null!;

    public virtual ICollection<Horario> Horarios { get; set; } = new List<Horario>();

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
}
