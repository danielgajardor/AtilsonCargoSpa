using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Paise
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? CodigoAlfa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual ICollection<Ciudade> Ciudades { get; set; } = new List<Ciudade>();
}
