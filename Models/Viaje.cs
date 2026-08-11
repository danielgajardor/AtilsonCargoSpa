using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Viaje
{
    public int Id { get; set; }

    public string? NumeroViaje { get; set; }

    public DateOnly? EtdZarpe { get; set; }

    public DateOnly? EtaArribo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public int? IdNave { get; set; }

    public virtual Nafe? IdNaveNavigation { get; set; }

    public virtual ICollection<Operacione> Operaciones { get; set; } = new List<Operacione>();
}
