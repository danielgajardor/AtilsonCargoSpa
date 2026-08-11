using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Cliente
{
    public int Id { get; set; }

    public string? Rut { get; set; }

    public string? NombreCliente { get; set; }

    public string RazonSocial { get; set; } = null!;

    public int? IdCiudad { get; set; }

    public string? Direccion { get; set; }

    public string? Contacto { get; set; }

    public byte? Activo { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? UsuarioCreador { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificador { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Ciudade? IdCiudadNavigation { get; set; }

    public virtual ICollection<Operacione> Operaciones { get; set; } = new List<Operacione>();

    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public virtual ICollection<TarifasCliente> TarifasClientes { get; set; } = new List<TarifasCliente>();
}
