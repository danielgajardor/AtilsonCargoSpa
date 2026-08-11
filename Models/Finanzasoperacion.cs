using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Finanzasoperacion
{
    public int Id { get; set; }
    public int IdOperacion { get; set; }

    // --- CACHÉ DE TOTALES (Se actualizarán por código calculando las transacciones) ---
    public decimal? VentaMaritimo { get; set; }
    public decimal? VentaTerrestre { get; set; }
    public decimal? VentaDocumental { get; set; }
    public decimal? VentaGate { get; set; }

    public decimal? CostoMaritimoNeto { get; set; }
    public decimal? CostoTerrestreNeto { get; set; }
    public decimal? CostoAgenciaNeto { get; set; }
    public decimal? CostoGateNeto { get; set; }

    public bool? CostoTerrestreManual { get; set; }
    public bool? CostoGateManual { get; set; }

    // --- DATOS OPERATIVOS ---
    public int? IdCondicionFlete { get; set; }
    public int? DiasLibresOrigen { get; set; }
    public int? DiasLibresDestino { get; set; }
    public string? ObservacionesFinanzas { get; set; }

    public DateTime FechaCreacion { get; set; }
    public string UsuarioCreador { get; set; } = null!;
    public DateTime FechaModificacion { get; set; }
    public string UsuarioModificador { get; set; } = null!;

    public virtual Operacione IdOperacionNavigation { get; set; } = null!;
}