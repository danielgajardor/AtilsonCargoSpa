using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtilsonCargoSpa.Models;

[Table("OperacionesTerrestres")]
public partial class OperacionesTerrestre
{
    [Key]
    public int Id { get; set; }

    public int IdOperacion { get; set; }

    [StringLength(100)]
    public string? EmpresaTransporte { get; set; }

    [StringLength(20)]
    public string? RutTransporte { get; set; }

    [StringLength(100)]
    public string? CorreoTransporte { get; set; }

    [StringLength(100)]
    public string? NombreConductor { get; set; }

    [StringLength(20)]
    public string? TelefonoConductor { get; set; }

    [StringLength(20)]
    public string? Patente { get; set; }

    [StringLength(50)]
    public string? TipoUnidadTransporte { get; set; }

    [StringLength(100)]
    public string? DepositoRetiro { get; set; }

    [StringLength(100)]
    public string? PlantaCarga { get; set; }

    [StringLength(100)]
    public string? ZonaEmbarque { get; set; }

    public DateTime? FechaCarga { get; set; }

    public string? LinkTracking { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaCreacion { get; set; }

    [StringLength(50)]
    public string? UsuarioCreador { get; set; }

    public DateTime? FechaModificacion { get; set; }

    [StringLength(50)]
    public string? UsuarioModificador { get; set; }

    public DateTime? LlegadaPlanta { get; set; }

    public DateTime? SalidaPlanta { get; set; }

    public DateTime? LlegadaPuerto { get; set; }

    public DateTime? SalidaPuerto { get; set; }

    public bool SorteoEscaner { get; set; }

    [StringLength(20)]
    public string? RutConductor { get; set; }

    public bool SolicitudEnviada { get; set; }

    public bool AsignacionEnviada { get; set; }

    [StringLength(50)]
    public string? Rampla { get; set; }

    [StringLength(100)]
    public string? ReferenciaCliente { get; set; }

    [StringLength(100)]
    public string? DepositoDevolucion { get; set; }

    [StringLength(100)]
    public string? ZonaCarga { get; set; }

    [StringLength(100)]
    public string? PuertoEntrega { get; set; }

    [StringLength(100)]
    public string? TerminalTerrestreStr { get; set; }

    [StringLength(20)]
    public string? PatenteRampla { get; set; }

    public string? RutaBookingTransporte { get; set; }

    // ==========================================
    // 🚀 MAGIA ATILSON: COLUMNAS INTERPLANTA
    // ==========================================
    public bool? AplicaInterplanta { get; set; }

    [StringLength(100)]
    public string? PlantaCarga2 { get; set; }

    public string? LinkMaps2 { get; set; }

    [StringLength(100)]
    public string? PlantaCarga3 { get; set; }

    public string? LinkMaps3 { get; set; }

    [ForeignKey("IdOperacion")]
    [InverseProperty("OperacionesTerrestres")]
    public virtual Operacione IdOperacionNavigation { get; set; } = null!;

    public string? FolioRetiro { get; set; }
    public DateTime? FechaRetiroVacio { get; set; }
    public string? RutaFolioRetiro { get; set; }
}