using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Deposito
{
    public int Id { get; set; }

    public int? IdNaviera { get; set; }

    public string NombreDeposito { get; set; } = null!;

    public string? Direccion { get; set; }

    public int? IdCiudad { get; set; }

    public byte? Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual Ciudade? IdCiudadNavigation { get; set; }

    public virtual Naviera? IdNavieraNavigation { get; set; }
    public string? Pais { get; set; }

    public virtual ICollection<Operacione> Operaciones { get; set; } = new List<Operacione>();
}
