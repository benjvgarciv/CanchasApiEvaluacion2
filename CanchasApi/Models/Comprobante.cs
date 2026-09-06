using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Comprobante
{
    public int Id { get; set; }

    public int PagoId { get; set; }

    public string NumeroComprobante { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public DateTime FechaEmision { get; set; }

    public virtual Pago Pago { get; set; } = null!;
}
