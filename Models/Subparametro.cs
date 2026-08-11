using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Subparametro
{
    public int Id { get; set; }

    public int ParametroId { get; set; }

    public string Valor { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public virtual Parametro Parametro { get; set; } = null!;
}
