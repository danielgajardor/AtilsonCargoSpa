using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtilsonCargoSpa.Models;

[Table("TarifasDocumentales")]
public partial class TarifasDocumentale
{
    [Key]
    public int Id { get; set; }

    // 👈 Conexión directa a AgenciasAduana
    public int? IdAgenciaAduana { get; set; }

    [Required]
    [StringLength(255)]
    [Unicode(false)]
    public string Concepto { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ValorNeto { get; set; }

    [Required]
    [StringLength(10)]
    [Unicode(false)]
    public string Moneda { get; set; } = null!;

    public bool AplicaIva { get; set; } = true;

    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificador { get; set; }
    public bool EsActiva { get; set; } = true;

    [Column(TypeName = "date")]
    public DateTime? FechaInicioVigencia { get; set; }

    [Column(TypeName = "date")]
    public DateTime? FechaFinVigencia { get; set; }

    [Unicode(false)]
    public string? Comentarios { get; set; }
    public string? Clasificacion { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? FechaCreacion { get; set; }

    // Propiedad de navegación
    [ForeignKey("IdAgenciaAduana")]
    [InverseProperty("TarifasDocumentales")]
    public virtual AgenciasAduana? IdAgenciaAduanaNavigation { get; set; }
}