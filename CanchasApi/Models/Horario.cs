using System;
using System.Collections.Generic;

namespace CanchasApi.Models;

public partial class Horario
{
    public int Id { get; set; }

    public int CanchaId { get; set; }

    public byte DiaSemana { get; set; }

    public TimeOnly HoraApertura { get; set; }

    public TimeOnly HoraCierre { get; set; }

    public virtual Cancha Cancha { get; set; } = null!;
}
