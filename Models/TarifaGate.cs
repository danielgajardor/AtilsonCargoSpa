using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtilsonCargoSpa.Models
{
    public partial class TarifaGate
    {
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificador { get; set; }
        public int Id { get; set; }
        public int? IdNaviera { get; set; }
        public int? IdDeposito { get; set; }
        public int? IdTipoCarga { get; set; }
        public string? TipoContenedor { get; set; }
        public string TipoMovimiento { get; set; } = "AMBOS";
        public decimal? ValorNeto { get; set; }
        public string Moneda { get; set; } = "CLP";

        // --- NUEVO HISTORIAL COMERCIAL ---
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public bool EsActiva { get; set; } = true;

        public DateTime? FechaCreacion { get; set; }
        public string? UsuarioCreador { get; set; }

        [ForeignKey("IdNaviera")]
        public virtual Naviera? IdNavieraNavigation { get; set; }
        [ForeignKey("IdDeposito")]
        public virtual Deposito? IdDepositoNavigation { get; set; }
    }
}