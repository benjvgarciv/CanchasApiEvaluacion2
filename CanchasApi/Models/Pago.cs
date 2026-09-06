using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Pago
{
    public int Id { get; set; }

    public int ReservaId { get; set; }

    public decimal Monto { get; set; }

    public string MetodoPago { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime? FechaPago { get; set; }

    public virtual Reserva Reserva { get; set; } = null!;

    public virtual Comprobante? Comprobante { get; set; }
}
