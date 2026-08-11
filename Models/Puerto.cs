using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Puerto
{
    public int Id { get; set; }

    public int? IdCiudad { get; set; }

    public string NombrePuerto { get; set; } = null!;

    public string? TerminalPortuario { get; set; }

    public string? Pais { get; set; }

    public int? IdTipoPuerto { get; set; }

    public byte? Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual Ciudade? IdCiudadNavigation { get; set; }

    public virtual ICollection<Operacione> OperacioneIdPuertoDestinoNavigations { get; set; } = new List<Operacione>();

    public virtual ICollection<Operacione> OperacioneIdPuertoOrigenNavigations { get; set; } = new List<Operacione>();
}
