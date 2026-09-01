using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <-- Obligatorio para leer el ForeignKey

namespace AtilsonCargoSpa.Models
{
    public partial class TarifasCliente
    {
        [Key]
        public int Id { get; set; }
        public int? IdCliente { get; set; }
        public string? Concepto { get; set; }
        public decimal PrecioPactado { get; set; }
        public string? Moneda { get; set; }
        public DateTime FechaInicioVigencia { get; set; }
        public DateTime FechaFinVigencia { get; set; }
        public bool EsActiva { get; set; }
        public string? UsuarioCreador { get; set; }
        public string? GrupoCobro { get; set; }
        public string? ZonaPlanta { get; set; }
        public string? TipoContenedor { get; set; }
        public bool EsServicioGratuito { get; set; }
        public string? Comentarios { get; set; }
        // ... otras propiedades ...
        public bool AplicaIva { get; set; } 

        // ================= NUEVOS CAMPOS MARÍTIMOS =================
        public int? IdNaviera { get; set; }
        public string? Pol { get; set; }
        public string? Pod { get; set; }
        public int? DiasLibresOrigen { get; set; }
        public int? DiasLibresDestino { get; set; }
        public decimal? ValorBL { get; set; }
        public decimal? ProfitHandling { get; set; }
        public string? Clasificacion { get; set; }
        // ===========================================================

        // Le indicamos explícitamente a C# cuáles son las columnas reales de SQL
        [ForeignKey("IdCliente")]
        public virtual Cliente? IdClienteNavigation { get; set; }

        [ForeignKey("IdNaviera")]
        public virtual Naviera? IdNavieraNavigation { get; set; }
    }
}