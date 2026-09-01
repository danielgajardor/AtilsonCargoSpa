using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AtilsonCargoSpa.Models
{
    [Table("TarifasAlmacenamiento")]
    public partial class TarifasAlmacenamiento
    {
        [Key]
        public int Id { get; set; }

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificador { get; set; }
        public int IdProveedor { get; set; }

        public int DiasLibres { get; set; } = 5;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TarifaBase { get; set; } = 30000.00m;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TarifaConexionReefer { get; set; } = 3500.00m;

        [StringLength(10)]
        [Unicode(false)]
        public string Moneda { get; set; } = "CLP";

        public bool AplicaIva { get; set; } = true;

        public bool EsActiva { get; set; } = true;

        [Column(TypeName = "date")]
        public DateTime? FechaInicioVigencia { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaFinVigencia { get; set; }

        public string? Comentarios { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? FechaCreacion { get; set; }

        [StringLength(100)]
        [Unicode(false)]
        public string? UsuarioCreador { get; set; }

        // Conexión con la tabla Proveedores (Las Cadenas)
        [ForeignKey("IdProveedor")]
        public virtual Proveedore? IdProveedorNavigation { get; set; }
    }
}