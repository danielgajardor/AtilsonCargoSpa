using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models
{
    public partial class ArchivosOperaciones
    {
        public int Id { get; set; }
        public int? IdOperacion { get; set; }

        // Agrega aquí las otras propiedades si tu tabla en SQL tiene más (ej: NombreArchivo, Ruta, etc.)
        // public string? RutaRespaldo { get; set; }
        public string? NombreArchivo { get; set; }
        public string? RutaArchivo { get; set; }
        public DateTime? FechaSubida { get; set; }

        public string? TipoDocumento { get; set; }
        public string? UsuarioSubida { get; set; }

        // REPARACIÓN DEL BINARYREADER: Si guardas el archivo en la BD, debe ser byte[] y no string
        public byte[]? ArchivoFisico { get; set; }
        public virtual Operacione? IdOperacionNavigation { get; set; }
    }
}