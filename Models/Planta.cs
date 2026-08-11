using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtilsonCargoSpa.Models
{
    public partial class Planta
    {
        [Key]
        public int Id { get; set; }

        public int? IdCliente { get; set; }

        public int IdCiudad { get; set; }

        [StringLength(200)]
        public string Nombre { get; set; } = null!;

        [StringLength(500)]
        public string? Direccion { get; set; }

        public string? UrlGoogleMaps { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        [StringLength(100)]
        public string UsuarioCreador { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }

        [StringLength(100)]
        public string? UsuarioModificador { get; set; }

        // --- MAGIA ATILSON: Propiedades de Navegación ---
        [ForeignKey("IdCiudad")]
        public virtual Ciudade Ciudad { get; set; } = null!;

        [ForeignKey("IdCliente")]
        public virtual Cliente? Cliente { get; set; }
    }
}