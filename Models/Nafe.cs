using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Nafe
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int? IdNaviera { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual Naviera? IdNavieraNavigation { get; set; }

    public virtual ICollection<Operacione> Operaciones { get; set; } = new List<Operacione>();

    public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
}
