using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Parametro
{
    public int Id { get; set; }

    public string Categoria { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public virtual ICollection<Subparametro> Subparametros { get; set; } = new List<Subparametro>();
}
