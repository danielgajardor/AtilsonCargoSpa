using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Origenescarga
{
    public int Id { get; set; }

    public string NombrePlanta { get; set; } = null!;

    public int? IdCiudad { get; set; }

    public string? Direccion { get; set; }

    public byte? Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual Ciudade? IdCiudadNavigation { get; set; }

    public virtual ICollection<Operacione> Operaciones { get; set; } = new List<Operacione>();
}
