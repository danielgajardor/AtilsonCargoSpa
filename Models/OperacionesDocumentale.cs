using System;
using System.Collections.Generic;

namespace AtilsonCargoSpa.Models
{
    public partial class OperacionesDocumentale
    {
        public int Id { get; set; }

        public int IdOperacion { get; set; }

        // --- Mantenemos tu campo antiguo por seguridad ---
        public string? AgenciaAduana { get; set; }

        // ==========================================================
        // 🚀 NUEVOS CAMPOS AÑADIDOS PARA EL WORKFLOW INTELIGENTE
        // ==========================================================
        public int? IdAgenciaAduana { get; set; }
        public bool AplicaSernapesca { get; set; }
        public bool AplicaSag { get; set; }

        public int? IdUnidadTecnica { get; set; }

        // Trazabilidad Cliente (V°B°)
        public DateTime? FechaEnvioDocsCliente { get; set; }
        public DateTime? FechaVbcliente { get; set; }
        public string? UsuarioVbcliente { get; set; }

        // Trazabilidad Trámites (SAG/Sernapesca)
        public DateTime? FechaEnvioAgencia { get; set; }
        public DateTime? FechaEnvioEntidad { get; set; }
        public DateTime? FechaAprobacionEntidad { get; set; }
        public DateTime? FechaPagoRemision { get; set; }
        // ==========================================================

        public string? DusDin { get; set; }

        public string? EstadoDocumental { get; set; }

        public bool? Activo { get; set; }

        public DateTime? FechaCreacion { get; set; }

        public string? UsuarioCreador { get; set; }

        public DateTime? FechaModificacion { get; set; }

        public string? UsuarioModificador { get; set; }

        public bool MatrizPresentada { get; set; }

        public string? EvidenciaMatriz { get; set; }

        public bool ExtensionDocumental { get; set; }

        public bool GuiaVisado { get; set; }

        public bool? CertificadoOrigen { get; set; }

        public bool? CertificadoSeguro { get; set; }

        public bool? CertificadoFito { get; set; }

        public string? Mandato { get; set; }

        public string? Consignatario { get; set; }

        public string? NotificarA { get; set; }

        public decimal? ValorAduana { get; set; }

        public string? DocBkgCli { get; set; }

        public string? Neppex { get; set; }
        public string? FacturaExportacion { get; set; }
        public string? Aol { get; set; }
        public string? GuiaDespacho { get; set; }
        public string? OpcionDus { get; set; }
        public string? InstructivoCliente { get; set; }
        public string? RemisionOrigen1 { get; set; }
        public string? RemisionOrigen2 { get; set; }
        public string? CertFitosanitario { get; set; }
        public string? RemisionFito1 { get; set; }
        public string? RemisionFito2 { get; set; }
        public string? CertSanitario { get; set; }
        public string? RemisionSanitario1 { get; set; }
        public string? RemisionSanitario2 { get; set; }
        public string? RemisionSanitario3 { get; set; }
        public string? CertCaptura { get; set; }
        public string? RemisionCaptura1 { get; set; }
        public string? RemisionCaptura2 { get; set; }
        public string? RemisionCaptura3 { get; set; }
        public string? RemisionCaptura4 { get; set; }
        public string? CertHaccp { get; set; }
        public string? CertLibreVenta { get; set; }
        public string? RutaCertificadoOrigen { get; set; }
        public DateTime? FechaEnvioFito { get; set; }
        public DateTime? FechaVBFito { get; set; }
        public string? UsuarioVBFito { get; set; }

        public DateTime? FechaEnvioSanitario { get; set; }
        public DateTime? FechaVBSanitario { get; set; }
        public string? UsuarioVBSanitario { get; set; }

        public DateTime? FechaEnvioCaptura { get; set; }
        public DateTime? FechaVBCaptura { get; set; }
        public string? UsuarioVBCaptura { get; set; }

        // Navegación Virtual
        public virtual Operacione IdOperacionNavigation { get; set; } = null!;

        // --- RELACIÓN CON LA NUEVA TABLA DE AGENCIAS ---
        public virtual AgenciasAduana? IdAgenciaAduanaNavigation { get; set; }

        // --- NUEVOS CAMPOS ADUANAS Y COSTOS ---
        public string? Din { get; set; }
        public decimal? ValorDus { get; set; }
        public decimal? ValorDin { get; set; }
        public decimal? ValorLegalizacion { get; set; }
        public decimal? ValorAclaracion { get; set; }
        public decimal? ValorCancelacion { get; set; }

        // --- NUEVOS CAMPOS DOCUMENTOS BASE ---
        public string? CertExtra { get; set; }
        public string? DhlTracking { get; set; }
        public string? RutaFactura { get; set; }
        public string? RutaGuia { get; set; }
        public string? RutaInstructivo { get; set; }
        public string? RutaPacking { get; set; }
        public string? RutaCertExtra { get; set; }
        public string? RutaDhl { get; set; }

        // --- SLOTS ORIGEN ---
        public string? AcuerdoOrigen { get; set; }
        public decimal? ValOri1 { get; set; }
        public decimal? ValOri2 { get; set; }
        public string? NumOri3 { get; set; }
        public decimal? ValOri3 { get; set; }
        public string? NumOri4 { get; set; }
        public decimal? ValOri4 { get; set; }
        public string? EvidenciaOrigen1 { get; set; }
        public string? EvidenciaOrigen2 { get; set; }
        public string? EvidenciaOrigen3 { get; set; }
        public string? EvidenciaOrigen4 { get; set; }

        // --- SLOTS FITOSANITARIO ---
        public string? NumFit1 { get; set; }
        public decimal? ValFit1 { get; set; }
        public string? NumFit2 { get; set; }
        public decimal? ValFit2 { get; set; }
        public string? NumFit3 { get; set; }
        public decimal? ValFit3 { get; set; }
        public string? NumFit4 { get; set; }
        public decimal? ValFit4 { get; set; }
        public string? EvidenciaFito1 { get; set; }
        public string? EvidenciaFito2 { get; set; }
        public string? EvidenciaFito3 { get; set; }
        public string? EvidenciaFito4 { get; set; }

        // --- SLOTS SANITARIO ---
        public string? NumSan1 { get; set; }
        public decimal? ValSan1 { get; set; }
        public string? NumSan2 { get; set; }
        public decimal? ValSan2 { get; set; }
        public string? NumSan3 { get; set; }
        public decimal? ValSan3 { get; set; }
        public string? NumSan4 { get; set; }
        public decimal? ValSan4 { get; set; }
        public string? EvidenciaSanitario1 { get; set; }
        public string? EvidenciaSanitario2 { get; set; }
        public string? EvidenciaSanitario3 { get; set; }
        public string? EvidenciaSanitario4 { get; set; }

        // --- SLOTS CODAUT ---
        public string? NumCod1 { get; set; }
        public decimal? ValCod1 { get; set; }
        public string? NumCod2 { get; set; }
        public decimal? ValCod2 { get; set; }
        public string? NumCod3 { get; set; }
        public decimal? ValCod3 { get; set; }
        public string? NumCod4 { get; set; }
        public decimal? ValCod4 { get; set; }
        public string? EvidenciaCod1 { get; set; }
        public string? EvidenciaCod2 { get; set; }
        public string? EvidenciaCod3 { get; set; }
        public string? EvidenciaCod4 { get; set; }

        // --- SLOTS CLAVE ---
        public string? NumCla1 { get; set; }
        public decimal? ValCla1 { get; set; }
        public string? NumCla2 { get; set; }
        public decimal? ValCla2 { get; set; }
        public string? NumCla3 { get; set; }
        public decimal? ValCla3 { get; set; }
        public string? NumCla4 { get; set; }
        public decimal? ValCla4 { get; set; }
        public string? EvidenciaCla1 { get; set; }
        public string? EvidenciaCla2 { get; set; }
        public string? EvidenciaCla3 { get; set; }
        public string? EvidenciaCla4 { get; set; }

        // --- SLOTS NEPPEX ---
        public string? NumNep1 { get; set; }
        public decimal? ValNep1 { get; set; }
        public string? NumNep2 { get; set; }
        public decimal? ValNep2 { get; set; }
        public string? NumNep3 { get; set; }
        public decimal? ValNep3 { get; set; }
        public string? NumNep4 { get; set; }
        public decimal? ValNep4 { get; set; }
        public string? EvidenciaNep1 { get; set; }
        public string? EvidenciaNep2 { get; set; }
        public string? EvidenciaNep3 { get; set; }
        public string? EvidenciaNep4 { get; set; }

        // -- CAMPOS PARA CERTIFICADO DE CAPTURA --
        public string? NumCap1 { get; set; }
        public decimal? ValCap1 { get; set; }
        public string? EvidenciaCap1 { get; set; }

        public string? NumCap2 { get; set; }
        public decimal? ValCap2 { get; set; }
        public string? EvidenciaCap2 { get; set; }

        public string? NumCap3 { get; set; }
        public decimal? ValCap3 { get; set; }
        public string? EvidenciaCap3 { get; set; }

        public string? NumCap4 { get; set; }
        public decimal? ValCap4 { get; set; }
        public string? EvidenciaCap4 { get; set; }

        // -- CAMPOS PARA COA --
        public string? NumCoa1 { get; set; }
        public decimal? ValCoa1 { get; set; }
        public string? EvidenciaCoa1 { get; set; }

        // -- CAMPOS PARA DT --
        public string? NumDt1 { get; set; }
        public decimal? ValDt1 { get; set; }
        public string? EvidenciaDt1 { get; set; }

        public string? LogOrigen { get; set; }
        public string? LogFitosanitario { get; set; }
        public string? LogSanitario { get; set; }
        public string? LogCaptura { get; set; }
        public string? LogCoa { get; set; }
        public string? LogDt { get; set; }
        public string? LogCodaut { get; set; }
        public string? LogClave { get; set; }
        public string? LogNeppex { get; set; }

        public string? NumGuiaDespacho { get; set; }
        public string? TrackingDhl { get; set; }
        public string? LogGuia { get; set; }
        public string? LogDhl { get; set; }
        public bool? DhlEnviadoCliente { get; set; }

        public string? LogFactura { get; set; }
        public string? LogInstructivo { get; set; }
        public string? LogPacking { get; set; }
        public string? LogExtra { get; set; }

        public string? BookingAtilson { get; set; }
        public string? RutaBookingAtilson { get; set; }
        public string? LogBookingAtilson { get; set; }

        public string? NumFullSet { get; set; }
        public string? RutaFullSet { get; set; }
        public string? LogFullSet { get; set; }

        public string? NumCapturaAga { get; set; }
        public string? RutaCapturaAga { get; set; }
        public string? LogCapturaAga { get; set; }

        public string? LogMatriz { get; set; }
        public string? ObsOrigen { get; set; }
        public string? ObsCaptura { get; set; }
        // Agrégalo al final de las propiedades existentes en tu clase OperacionesDocumentale
        public string? RutaLibreVenta { get; set; }
        public string? LogLibreVenta { get; set; }
        public string? CertsBloqueados { get; set; }   // CSV: "ori,fit,san,cap,coa,dt,cod,cla,nep"
        public string? CertsSinNumero { get; set; }   // CSV con los mismos keys

        public bool? Roleo { get; set; }
        public decimal? ValorRoleo { get; set; }
        public bool? GeneracionIvv { get; set; }

        public string? SagModalidad { get; set; }
    }
}