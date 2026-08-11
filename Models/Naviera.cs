using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Naviera
{
    public int Id { get; set; }

    public string NombreNaviera { get; set; } = null!;

    public string? ColorRepresentativo { get; set; }

    public string? LinkTracking { get; set; }

    public byte? Activo { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual ICollection<Deposito> Depositos { get; set; } = new List<Deposito>();

    public virtual ICollection<Nafe> Naves { get; set; } = new List<Nafe>();

    public virtual ICollection<Operacione> Operaciones { get; set; } = new List<Operacione>();

    public virtual ICollection<TarifasMaritima> TarifasMaritimas { get; set; } = new List<TarifasMaritima>();
}
