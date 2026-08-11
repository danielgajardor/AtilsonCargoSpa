using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace AtilsonCargoSpa.Models
{
    public partial class Cotizacion
    {
        public int Id { get; set; }
        public int? IdCliente { get; set; }
        public string TipoServicio { get; set; } = null!;
        public string TipoCarga { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public string Destino { get; set; } = null!;
        public string? Mercancia { get; set; }
        public string? Comentarios { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public bool? Activo { get; set; }

        [ForeignKey("IdCliente")]
        public virtual Cliente? IdClienteNavigation { get; set; }
    }
}