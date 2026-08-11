using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Ciudade
{
    public int Id { get; set; }

    public int IdPais { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioCreador { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public string UsuarioModificador { get; set; } = null!;

    public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

    public virtual ICollection<Deposito> Depositos { get; set; } = new List<Deposito>();

    public virtual Paise IdPaisNavigation { get; set; } = null!;

    public virtual ICollection<Origenescarga> Origenescargas { get; set; } = new List<Origenescarga>();

    public virtual ICollection<Puerto> Puertos { get; set; } = new List<Puerto>();

    public virtual ICollection<Tarifasterrestre> TarifasterrestreIdCiudadDestinoNavigations { get; set; } = new List<Tarifasterrestre>();

    public virtual ICollection<Tarifasterrestre> TarifasterrestreIdCiudadOrigenNavigations { get; set; } = new List<Tarifasterrestre>();

    // --- MAGIA ATILSON: NUEVA CONEXIÓN CON PLANTAS ---
    public virtual ICollection<Planta> Plantas { get; set; } = new List<Planta>();
}