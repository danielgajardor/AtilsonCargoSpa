using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AtilsonCargoSpa.Models
{
    [Table("OperacionesAlmacenamiento")]
    public partial class OperacionesAlmacenamiento
    {
        [Key]
        public int Id { get; set; }

        public int IdOperacion { get; set; }

        public int? IdProveedor { get; set; }

        // --- BLOQUE 1: INGRESO ---
        [Column(TypeName = "datetime")]
        public DateTime? FechaIngreso { get; set; }

        [StringLength(150)]
        [Unicode(false)]
        public string? ConductorIngresoNombre { get; set; }

        [StringLength(20)]
        [Unicode(false)]
        public string? ConductorIngresoRut { get; set; }

        [StringLength(50)]
        [Unicode(false)]
        public string? ConductorIngresoTelefono { get; set; }

        [StringLength(20)]
        [Unicode(false)]
        public string? CamionIngresoPatente { get; set; }

        public bool NotificacionIngresoEnviada { get; set; }

        // --- BLOQUE 2: SALIDA ---
        [Column(TypeName = "datetime")]
        public DateTime? FechaSalida { get; set; }

        [StringLength(150)]
        [Unicode(false)]
        public string? ConductorSalidaNombre { get; set; }

        [StringLength(20)]
        [Unicode(false)]
        public string? ConductorSalidaRut { get; set; }

        [StringLength(50)]
        [Unicode(false)]
        public string? ConductorSalidaTelefono { get; set; }

        [StringLength(20)]
        [Unicode(false)]
        public string? CamionSalidaPatente { get; set; }

        public bool NotificacionSalidaEnviada { get; set; }

        // --- RESPALDOS FINANCIEROS ---
        [StringLength(500)]
        [Unicode(false)]
        public string? RutaArchivoTransferencia { get; set; }

        public string? Comentarios { get; set; }

        public bool Activo { get; set; } = true;

        [Column(TypeName = "datetime")]
        public DateTime? FechaCreacion { get; set; }

        [StringLength(100)]
        [Unicode(false)]
        public string? UsuarioCreador { get; set; }

        // Conexiones de Base de Datos
        [ForeignKey("IdOperacion")]
        public virtual Operacione? IdOperacionNavigation { get; set; }

        [ForeignKey("IdProveedor")]
        public virtual Proveedore? IdProveedorNavigation { get; set; }
    }
}