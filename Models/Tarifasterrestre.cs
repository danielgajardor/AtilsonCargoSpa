using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models;

public partial class Tarifasterrestre
{
    public int Id { get; set; }
    public int? IdProveedor { get; set; }
    public int? IdCiudadOrigen { get; set; }
    public int? IdCiudadDestino { get; set; }
    public int? IdTipoCarga { get; set; }
    public string? NombreTramo { get; set; }
    public decimal? ValorNeto { get; set; }
    public string? RutaRespaldo { get; set; }
    public string? Comentarios { get; set; }

    // --- HISTORIAL COMERCIAL ---
    public DateOnly? FechaInicioVigencia { get; set; }
    public DateOnly? FechaFinVigencia { get; set; }
    public bool EsActiva { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
    public string UsuarioCreador { get; set; } = null!;
    public DateTime FechaModificacion { get; set; }
    public string UsuarioModificador { get; set; } = null!;

    public virtual Ciudade? IdCiudadDestinoNavigation { get; set; }
    public virtual Ciudade? IdCiudadOrigenNavigation { get; set; }
    public int? IdCiudadPlanta { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.ForeignKey("IdCiudadPlanta")]
    public virtual Ciudade? IdCiudadPlantaNavigation { get; set; }
    public virtual Proveedore? IdProveedorNavigation { get; set; }

    // --- NUEVAS COLUMNAS (REUNIÓN CRISTIAN) ---
    public int? HorasLibresPlanta { get; set; } = 7;
    public int? HorasLibresPuerto { get; set; } = 3;
    public decimal? ValorFalsoFlete { get; set; }
    public decimal? FalsoFletePlanta { get; set; }
    public decimal? FalsoFleteRutaMayor50 { get; set; }
    public decimal? FalsoFleteRutaMenor50 { get; set; }
    public decimal? FalsoFleteDeposito { get; set; }
    public decimal? ServicioPorteoPuerto { get; set; }
    public decimal? Interplanta { get; set; }
    public string? ContratoNegociado { get; set; }
}