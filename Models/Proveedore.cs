using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Proveedore
{
    public int Id { get; set; }

    public string? Rut { get; set; }

    public string NombreProveedor { get; set; } = null!;

    public int IdTipoProveedor { get; set; }

    public string? Contacto { get; set; }

    public string? CorreoOperativo { get; set; }

    public byte? Activo { get; set; }

    public virtual ICollection<Conductore> Conductores { get; set; } = new List<Conductore>();

    public virtual ICollection<Tarifasterrestre> Tarifasterrestres { get; set; } = new List<Tarifasterrestre>();
}
