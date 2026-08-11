using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Conductore
{
    public int Id { get; set; }

    public int IdProveedor { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Rut { get; set; }

    public string? Telefono { get; set; }

    public string Patente { get; set; } = null!;

    public bool? Activo { get; set; }

    public virtual Proveedore IdProveedorNavigation { get; set; } = null!;
}
