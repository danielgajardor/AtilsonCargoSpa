using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models
{
    public partial class AgenciasAduana
    {
        public AgenciasAduana()
        {
            OperacionesDocumentales = new HashSet<OperacionesDocumentale>();
        }

        public int Id { get; set; }
        public string NombreAgencia { get; set; } = null!;
        public string? Rut { get; set; }
        public string? Contacto { get; set; }
        public string? CorreoContacto { get; set; }
        public int Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string UsuarioCreador { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificador { get; set; }
        
        public virtual ICollection<TarifasDocumentale> TarifasDocumentales { get; set; } = new List<TarifasDocumentale>();

        public virtual ICollection<OperacionesDocumentale> OperacionesDocumentales { get; set; }



    }
}