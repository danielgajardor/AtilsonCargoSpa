using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtilsonCargoSpa.Models
{
    [Table("TarifasMaestras")]
    public partial class TarifasMaestra
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string Categoria { get; set; } = null!;

        [StringLength(100)]
        public string Concepto { get; set; } = null!;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorNeto { get; set; }

        [StringLength(10)]
        public string Moneda { get; set; } = null!;

        public bool AplicaIva { get; set; }

        public bool EsActiva { get; set; }

        // Nuevas columnas de "Inteligencia de Reglas de Negocio"
        public int? HorasLibres { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? PorcentajeCobro { get; set; }
    }
}