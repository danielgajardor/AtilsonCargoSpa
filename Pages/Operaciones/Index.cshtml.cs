using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using AtilsonCargoSpa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;

        public IndexModel(AtilsonContext context, IWebHostEnvironment env, EmailService emailService, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _env = env;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;
        public IList<Cotizacion> NuevasCotizaciones { get; set; } = new List<Cotizacion>();
        public string? PatenteRampla { get; private set; }
        public string? RutConductor { get; private set; }

        public async Task OnGetAsync()
        {
            Operaciones = await _context.Operaciones
                .Where(o => o.IsDeleted == false)
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.Unidadestecnicas)
                .Include(o => o.ExtracostosOperacions)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            ViewData["IdNaviera"] = new SelectList(_context.Navieras.Where(n => n.Activo == 1), "Id", "NombreNaviera");
            ViewData["IdPuertoOrigen"] = new SelectList(_context.Puertos.Where(p => p.Activo == 1), "Id", "NombrePuerto");
            ViewData["IdTipoServicio"] = new SelectList(_context.Subparametros.Where(p => p.Parametro.Categoria == "TipoMovimiento"), "Id", "Valor");
            ViewData["PlantasList"] = await _context.Plantas.Include(p => p.Ciudad).Where(p => p.Activo).ToListAsync();
            ViewData["PuertosList"] = await _context.Puertos.Where(p => p.Activo == 1).OrderBy(p => p.NombrePuerto).ToListAsync();
            ViewData["DepositosList"] = await _context.Depositos.Where(d => d.Activo == 1).OrderBy(d => d.NombreDeposito).ToListAsync();
            ViewData["IdAgenciaAduana"] = new SelectList(await _context.AgenciasAduanas.ToListAsync(), "Id", "NombreAgencia");

            // 👇 NUEVAS LISTAS DESDE TARIFAS MAESTRAS 👇
            ViewData["TarifasMaritimo"] = await _context.TarifasMaestras.Where(t => t.EsActiva && t.Categoria == "Marítimo").OrderBy(t => t.Concepto).ToListAsync();
            ViewData["TarifasTerrestre"] = await _context.TarifasMaestras.Where(t => t.EsActiva && t.Categoria == "Terrestre").OrderBy(t => t.Concepto).ToListAsync();
            ViewData["TarifasDocumental"] = await _context.TarifasMaestras.Where(t => t.EsActiva && t.Categoria == "Documental").OrderBy(t => t.Concepto).ToListAsync();

            NuevasCotizaciones = await _context.Cotizaciones
                .Where(c => c.Estado == "NUEVA SOLICITUD" && c.Activo == true)
                .OrderByDescending(c => c.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var operacion = await _context.Operaciones.FindAsync(id);
            if (operacion != null)
            {
                operacion.IsDeleted = true;
                operacion.Activo = 0;
                operacion.FechaModificacion = DateTime.Now;
                operacion.UsuarioModificador = User.Identity?.Name ?? "Sistema";

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "No se encontró la operación." });
        }

        public async Task<IActionResult> OnPostQuickEditGeneralesAsync(int IdOperacion, int? IdTipoServicio, string? PaisOrigen, string? Commodity, string? TipoContenedor, double? Temperatura, double? Ventilacion, double? Humedad, string? NumeroInstructivo)
        {
            var operacion = await _context.Operaciones.FindAsync(IdOperacion);
            if (operacion != null)
            {
                if (IdTipoServicio.HasValue) operacion.IdTipoServicio = IdTipoServicio.Value;
                if (!string.IsNullOrWhiteSpace(Commodity)) operacion.Commodity = Commodity;
                if (!string.IsNullOrWhiteSpace(PaisOrigen)) operacion.PaisOrigen = PaisOrigen;
                if (!string.IsNullOrWhiteSpace(TipoContenedor)) operacion.TipoContenedor = TipoContenedor;

                if (Request.Form.ContainsKey("NumeroInstructivo")) operacion.NumeroInstructivo = NumeroInstructivo;

                operacion.Temperatura = Temperatura;
                operacion.Ventilacion = Ventilacion;
                operacion.Humedad = Humedad;

                operacion.FechaModificacion = DateTime.Now;
                operacion.UsuarioModificador = User.Identity?.Name ?? "Sistema";

                RegistrarHito(operacion, "GENERAL", $"Datos base modificados — Servicio: {IdTipoServicio} | Commodity: {Commodity ?? "-"}");
                operacion.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Información base actualizada.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostQuickEditMaritimoAsync(
            int IdOperacion, int? IdNaviera, string? Nave, string? Transbordo, int? IdPuertoOrigen, int? IdPuertoDestino, string? PaisOrigen, DateTime? EtdPol, DateTime? EtaPod, DateTime? FechaStacking, DateTime? CutOffMatriz, DateTime? LateArrival, DateTime? ElarDate, string? HistorialEtdVal, string? HistorialEtaVal, string? NumeroContenedor, string? NumeroSello, IFormFile? FotoContenedor, IFormFile? FotoSello, string? TerminalTerrestreStr)
        {
            var operacion = await _context.Operaciones
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Finanzasoperacions)
                .FirstOrDefaultAsync(o => o.Id == IdOperacion);

            if (operacion != null)
            {
                var terr = operacion.OperacionesTerrestres.FirstOrDefault();
                if (terr == null)
                {
                    terr = new OperacionesTerrestre { IdOperacion = operacion.Id, FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true };
                    operacion.OperacionesTerrestres.Add(terr);
                }

                if (IdNaviera.HasValue) operacion.IdNaviera = IdNaviera.Value;
                if (!string.IsNullOrWhiteSpace(Nave)) operacion.Nave = Nave;
                operacion.Transbordo = Transbordo;

                if (IdPuertoOrigen.HasValue)
                {
                    operacion.IdPuertoOrigen = IdPuertoOrigen;
                    var pto = await _context.Puertos.FindAsync(IdPuertoOrigen);
                    if (pto != null && (operacion.IdTipoServicio >= 1 && operacion.IdTipoServicio <= 7))
                        terr.PuertoEntrega = pto.NombrePuerto;
                }

                if (IdPuertoDestino.HasValue)
                {
                    operacion.IdPuertoDestino = IdPuertoDestino;
                    var pto = await _context.Puertos.FindAsync(IdPuertoDestino);
                    if (pto != null && (operacion.IdTipoServicio >= 8 && operacion.IdTipoServicio <= 14))
                        terr.PuertoEntrega = pto.NombrePuerto;
                }

                if (TerminalTerrestreStr != null) terr.TerminalTerrestreStr = TerminalTerrestreStr;

                operacion.NumeroContenedor = NumeroContenedor;
                operacion.NumeroSello = NumeroSello;
                operacion.EtdPol = EtdPol;
                operacion.EtaPod = EtaPod;
                operacion.FechaStacking = FechaStacking;
                operacion.CutOffMatriz = CutOffMatriz;
                operacion.LateArrival = LateArrival;
                operacion.ElarDate = ElarDate;

                if (!string.IsNullOrEmpty(HistorialEtaVal)) operacion.Comentarios += "\n" + HistorialEtaVal;
                if (!string.IsNullOrEmpty(HistorialEtdVal)) operacion.Comentarios += "\n" + HistorialEtdVal;

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (FotoContenedor != null && FotoContenedor.Length > 0)
                {
                    string uNameConte = $"{operacion.Id}_Conte_{DateTime.Now.Ticks}_{Path.GetFileName(FotoContenedor.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameConte), FileMode.Create)) { await FotoContenedor.CopyToAsync(stream); }
                    operacion.FotoContenedor = $"/uploads/evidencias/{uNameConte}";
                }
                if (FotoSello != null && FotoSello.Length > 0)
                {
                    string uNameSello = $"{operacion.Id}_Sello_{DateTime.Now.Ticks}_{Path.GetFileName(FotoSello.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameSello), FileMode.Create)) { await FotoSello.CopyToAsync(stream); }
                    operacion.FotoSello = $"/uploads/evidencias/{uNameSello}";
                }

                operacion.FechaModificacion = DateTime.Now;
                operacion.UsuarioModificador = User.Identity?.Name ?? "Sistema";
                RegistrarHito(operacion, "MARITIMO", $"Itinerario actualizado — Nave: {Nave ?? "-"} | ETD: {EtdPol?.ToString("dd/MM/yyyy") ?? "-"} | Stacking OUT: {CutOffMatriz?.ToString("dd/MM/yyyy HH:mm") ?? "-"}");

                try { await AplicarTarifaMaritimaAutomaticaAsync(operacion); }
                catch (Exception ex) { RegistrarHito(operacion, "MARITIMO", $"ERROR al calcular tarifa marítima automática: {ex.Message}"); }

                try { await AplicarTarifaGateAutomaticaAsync(operacion, terr); }
                catch (Exception ex) { RegistrarHito(operacion, "MARITIMO", $"ERROR al calcular tarifa Gate automática: {ex.Message}"); }

                operacion.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = true });

                TempData["SuccessMsg"] = "Itinerario marítimo y equipos actualizados.";
            }
            else if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return new JsonResult(new { success = false, message = "Operación no encontrada" });
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostQuickEditTransporteAsync(
            int IdOperacion, string? EmpresaTransporte, string? RutTransporte, string? CorreoTransporte,
            string? NombreConductor, string? RutConductor, string? TelefonoConductor, string? Patente, string? PatenteRampla,
            DateTime? FechaCarga, string? DepositoRetiro, string? PlantaCarga, string? ZonaCarga, string? LinkTracking,
            string? PuertoEntrega, string? TerminalTerrestreStr, string? NumeroContenedor, string? NumeroSello,
            bool AplicaInterplanta, string? PlantaCarga2, string? LinkMaps2, string? PlantaCarga3, string? LinkMaps3,
            string? FolioRetiro, DateTime? FechaRetiroVacio, IFormFile? UploadFolioRetiro,
            IFormFile? FotoContenedor, IFormFile? FotoSello, IFormFile? UploadBookingTransporte)
        {
            var operacion = await _context.Operaciones
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Finanzasoperacions)
                .FirstOrDefaultAsync(o => o.Id == IdOperacion);

            if (operacion != null)
            {
                var terrDb = operacion.OperacionesTerrestres.FirstOrDefault();
                if (terrDb == null)
                {
                    terrDb = new OperacionesTerrestre { IdOperacion = operacion.Id, FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true };
                    _context.OperacionesTerrestres.Add(terrDb);
                }

                if (Request.Form.ContainsKey("EmpresaTransporte")) terrDb.EmpresaTransporte = EmpresaTransporte;
                if (Request.Form.ContainsKey("RutTransporte")) terrDb.RutTransporte = RutTransporte;
                if (Request.Form.ContainsKey("CorreoTransporte")) terrDb.CorreoTransporte = CorreoTransporte;
                if (Request.Form.ContainsKey("NombreConductor")) terrDb.NombreConductor = NombreConductor;
                if (Request.Form.ContainsKey("RutConductor")) terrDb.RutConductor = RutConductor;
                if (Request.Form.ContainsKey("TelefonoConductor")) terrDb.TelefonoConductor = TelefonoConductor;
                if (Request.Form.ContainsKey("Patente")) terrDb.Patente = Patente;
                if (Request.Form.ContainsKey("PatenteRampla")) terrDb.PatenteRampla = PatenteRampla;
                if (Request.Form.ContainsKey("FechaCarga")) terrDb.FechaCarga = FechaCarga;
                if (Request.Form.ContainsKey("DepositoRetiro")) terrDb.DepositoRetiro = DepositoRetiro;
                if (Request.Form.ContainsKey("PlantaCarga")) terrDb.PlantaCarga = PlantaCarga;
                if (Request.Form.ContainsKey("ZonaCarga")) terrDb.ZonaCarga = ZonaCarga;
                if (Request.Form.ContainsKey("LinkTracking")) terrDb.LinkTracking = LinkTracking;
                if (Request.Form.ContainsKey("FolioRetiro")) terrDb.FolioRetiro = FolioRetiro;
                if (Request.Form.ContainsKey("FechaRetiroVacio")) terrDb.FechaRetiroVacio = FechaRetiroVacio;

                terrDb.AplicaInterplanta = AplicaInterplanta;
                if (Request.Form.ContainsKey("PlantaCarga2")) terrDb.PlantaCarga2 = PlantaCarga2;
                if (Request.Form.ContainsKey("LinkMaps2")) terrDb.LinkMaps2 = LinkMaps2;
                if (Request.Form.ContainsKey("PlantaCarga3")) terrDb.PlantaCarga3 = PlantaCarga3;
                if (Request.Form.ContainsKey("LinkMaps3")) terrDb.LinkMaps3 = LinkMaps3;

                if (Request.Form.ContainsKey("PuertoEntrega")) terrDb.PuertoEntrega = PuertoEntrega;
                if (Request.Form.ContainsKey("TerminalTerrestreStr")) terrDb.TerminalTerrestreStr = TerminalTerrestreStr;
                if (Request.Form.ContainsKey("NumeroContenedor")) operacion.NumeroContenedor = NumeroContenedor;
                if (Request.Form.ContainsKey("NumeroSello")) operacion.NumeroSello = NumeroSello;

                try { await AplicarTarifaTerrestreAutomaticaAsync(operacion, terrDb); }
                catch (Exception ex) { RegistrarHito(operacion, "TRANSPORTE", $"ERROR al calcular tarifa automática: {ex.Message}"); }

                try { await AplicarTarifaGateAutomaticaAsync(operacion, terrDb); }
                catch (Exception ex) { RegistrarHito(operacion, "TRANSPORTE", $"ERROR al calcular tarifa Gate automática: {ex.Message}"); }

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (FotoContenedor != null && FotoContenedor.Length > 0)
                {
                    string uNameConte = $"{operacion.Id}_Conte_{DateTime.Now.Ticks}_{Path.GetFileName(FotoContenedor.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameConte), FileMode.Create)) { await FotoContenedor.CopyToAsync(stream); }
                    operacion.FotoContenedor = $"/uploads/evidencias/{uNameConte}";
                }
                if (FotoSello != null && FotoSello.Length > 0)
                {
                    string uNameSello = $"{operacion.Id}_Sello_{DateTime.Now.Ticks}_{Path.GetFileName(FotoSello.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameSello), FileMode.Create)) { await FotoSello.CopyToAsync(stream); }
                    operacion.FotoSello = $"/uploads/evidencias/{uNameSello}";
                }
                if (UploadBookingTransporte != null && UploadBookingTransporte.Length > 0)
                {
                    string uNameBooking = $"{operacion.Id}_BookingTransporte_{DateTime.Now.Ticks}_{Path.GetFileName(UploadBookingTransporte.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameBooking), FileMode.Create)) { await UploadBookingTransporte.CopyToAsync(stream); }
                    terrDb.RutaBookingTransporte = $"/uploads/evidencias/{uNameBooking}";
                    RegistrarHito(operacion, "TRANSPORTE", $"Archivo de Booking adjuntado en Solicitud de Servicio: {UploadBookingTransporte.FileName}");
                }
                if (UploadFolioRetiro != null && UploadFolioRetiro.Length > 0)
                {
                    string uNameFolio = $"{operacion.Id}_Folio_{DateTime.Now.Ticks}_{Path.GetFileName(UploadFolioRetiro.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameFolio), FileMode.Create)) { await UploadFolioRetiro.CopyToAsync(stream); }
                    terrDb.RutaFolioRetiro = $"/uploads/evidencias/{uNameFolio}";
                }

                operacion.FechaModificacion = DateTime.Now;
                operacion.UsuarioModificador = User.Identity?.Name ?? "Sistema";
                operacion.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return new JsonResult(new { success = true });
                TempData["SuccessMsg"] = "Datos de transporte actualizados exitosamente.";
            }
            else if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return new JsonResult(new { success = false, message = "Operación no encontrada" });
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostQuickEditDocumentalAsync(int IdOperacion,
            IFormFile? UploadFactura, IFormFile? UploadGuia, IFormFile? UploadInstructivo, IFormFile? UploadPacking, IFormFile? UploadCertExtra, IFormFile? UploadLibreVenta, IFormFile? UploadDhl, IFormFile? UploadBookingAtilson, IFormFile? UploadFullSet,
            IFormFile? UploadOri1, IFormFile? UploadOri2, IFormFile? UploadOri3, IFormFile? UploadOri4,
            IFormFile? UploadCertFito, IFormFile? UploadFit2, IFormFile? UploadFit3, IFormFile? UploadFit4,
            IFormFile? UploadCertSanitario, IFormFile? UploadSan2, IFormFile? UploadSan3, IFormFile? UploadSan4,
            IFormFile? UploadCod1, IFormFile? UploadCod2, IFormFile? UploadCod3, IFormFile? UploadCod4,
            IFormFile? UploadCla1, IFormFile? UploadCla2, IFormFile? UploadCla3, IFormFile? UploadCla4,
            IFormFile? UploadNep1, IFormFile? UploadNep2, IFormFile? UploadNep3, IFormFile? UploadNep4,
            IFormFile? UploadCap1, IFormFile? UploadCap2, IFormFile? UploadCap3, IFormFile? UploadCap4,
            IFormFile? UploadCoa, IFormFile? UploadDt, IFormFile? UploadCapturaAga)
        {
            try
            {
                var op = await _context.Operaciones
                    .Include(o => o.OperacionesDocumentales)
                    .Include(o => o.ExtracostosOperacions)
                    .FirstOrDefaultAsync(o => o.Id == IdOperacion);

                if (op == null) return NotFound();

                var docDb = op.OperacionesDocumentales.OrderBy(x => x.Id).FirstOrDefault();
                if (docDb == null)
                {
                    docDb = new OperacionesDocumentale { FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true };
                    op.OperacionesDocumentales.Add(docDb);
                }

                string? F(string key) { var val = Request.Form[key].FirstOrDefault(); return string.IsNullOrWhiteSpace(val) ? null : val.Trim(); }
                decimal? FDec(string key)
                {
                    var val = Request.Form[key].FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(val)) return null;
                    val = val.Replace(".", "").Replace(",", ".");
                    return decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : null;
                }
                bool FBoolPreserve(string key, bool valorActual) => Request.Form.ContainsKey(key) ? Request.Form[key].Any(v => v == "true" || v == "on") : valorActual;
                int? FInt(string key) { var val = Request.Form[key].FirstOrDefault(); return int.TryParse(val, out var r) ? r : null; }

                // --- 🚀 FIX CRÍTICO: DETECTAR CHECKBOX DINÁMICOS DESDE CUALQUIER PESTAÑA PARA FACTURACIÓN 🚀 ---
                bool checkOri = Request.Form.Keys.Any(k => k.Contains("aplica_ori_") && Request.Form[k] == "si");
                docDb.CertificadoOrigen = checkOri || FBoolPreserve("docUpdates.CertificadoOrigen", docDb.CertificadoOrigen == true);

                bool checkFit = Request.Form.Keys.Any(k => k.Contains("aplica_fit_") && Request.Form[k] == "si");
                docDb.AplicaSag = checkFit || FBoolPreserve("docUpdates.AplicaSag", docDb.AplicaSag);

                bool checkSan = Request.Form.Keys.Any(k => k.Contains("aplica_san_") && Request.Form[k] == "si");
                bool checkCap = Request.Form.Keys.Any(k => k.Contains("aplica_cap_") && Request.Form[k] == "si");
                docDb.AplicaSernapesca = checkSan || checkCap || FBoolPreserve("docUpdates.AplicaSernapesca", docDb.AplicaSernapesca);

                docDb.IdAgenciaAduana = FInt("docUpdates.IdAgenciaAduana");
                docDb.DusDin = F("docUpdates.Dus");
                docDb.Din = F("docUpdates.Din");
                docDb.EstadoDocumental = F("docUpdates.EstadoDocumental");
                docDb.ExtensionDocumental = FBoolPreserve("docUpdates.ExtensionDocumental", docDb.ExtensionDocumental == true);
                docDb.GuiaVisado = FBoolPreserve("docUpdates.GuiaVisado", docDb.GuiaVisado == true);

                // NUEVOS CAMPOS: Trámites extra e IVV
                docDb.Roleo = FBoolPreserve("docUpdates.Roleo", docDb.Roleo == true);
                docDb.GeneracionIvv = FBoolPreserve("docUpdates.GeneracionIvv", docDb.GeneracionIvv == true);

                docDb.AcuerdoOrigen = F("docUpdates.AcuerdoOrigen");
                docDb.RemisionOrigen1 = F("docUpdates.NumOri1");
                docDb.RemisionOrigen2 = F("docUpdates.NumOri2");
                docDb.NumOri3 = F("docUpdates.NumOri3");
                docDb.NumOri4 = F("docUpdates.NumOri4");

                docDb.CertFitosanitario = F("docUpdates.NumFit1");
                docDb.NumFit2 = F("docUpdates.NumFit2");
                docDb.NumFit3 = F("docUpdates.NumFit3");
                docDb.NumFit4 = F("docUpdates.NumFit4");

                docDb.CertSanitario = F("docUpdates.NumSan1");
                docDb.NumSan2 = F("docUpdates.NumSan2");
                docDb.NumSan3 = F("docUpdates.NumSan3");
                docDb.NumSan4 = F("docUpdates.NumSan4");

                docDb.NumCod1 = F("docUpdates.NumCod1"); docDb.NumCod2 = F("docUpdates.NumCod2"); docDb.NumCod3 = F("docUpdates.NumCod3"); docDb.NumCod4 = F("docUpdates.NumCod4");
                docDb.NumCla1 = F("docUpdates.NumCla1"); docDb.NumCla2 = F("docUpdates.NumCla2"); docDb.NumCla3 = F("docUpdates.NumCla3"); docDb.NumCla4 = F("docUpdates.NumCla4");
                docDb.NumNep1 = F("docUpdates.NumNep1"); docDb.NumNep2 = F("docUpdates.NumNep2"); docDb.NumNep3 = F("docUpdates.NumNep3"); docDb.NumNep4 = F("docUpdates.NumNep4");

                docDb.FacturaExportacion = F("docUpdates.FacturaExportacion");
                docDb.InstructivoCliente = F("docUpdates.InstructivoCliente");
                docDb.NotificarA = F("docUpdates.NotificarA");
                docDb.CertExtra = F("docUpdates.CertExtra");
                docDb.CertLibreVenta = F("docUpdates.CertLibreVenta");
                docDb.BookingAtilson = F("docUpdates.BookingAtilson");
                docDb.NumGuiaDespacho = F("docUpdates.NumGuiaDespacho");
                docDb.NumFullSet = F("docUpdates.NumFullSet");
                docDb.TrackingDhl = F("docUpdates.TrackingDhl");

                docDb.LogGuia = F("docUpdates.LogGuia");
                docDb.LogInstructivo = F("docUpdates.LogInstructivo");
                docDb.LogBookingAtilson = F("docUpdates.LogBookingAtilson");
                docDb.LogFullSet = F("docUpdates.LogFullSet");
                docDb.LogDhl = F("docUpdates.LogDhl");
                docDb.LogFactura = F("docUpdates.LogFactura");
                docDb.LogPacking = F("docUpdates.LogPacking");
                docDb.LogExtra = F("docUpdates.LogExtra");
                docDb.LogLibreVenta = Request.Form["docUpdates.LogLibreVenta"].FirstOrDefault();

                docDb.NumCap1 = F("docUpdates.NumCap1"); docDb.NumCap2 = F("docUpdates.NumCap2"); docDb.NumCap3 = F("docUpdates.NumCap3"); docDb.NumCap4 = F("docUpdates.NumCap4");
                docDb.NumCoa1 = F("docUpdates.NumCoa1"); docDb.NumDt1 = F("docUpdates.NumDt1");

                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "documentos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                async Task SaveFile(IFormFile? file, Action<string> setPath)
                {
                    if (file == null || file.Length == 0) return;
                    string fileName = $"{op.Id}_{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName).Replace(" ", "_")}";
                    using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                    await file.CopyToAsync(stream);
                    setPath($"/uploads/documentos/{fileName}");
                }

                await SaveFile(UploadFactura, p => docDb.RutaFactura = p);
                await SaveFile(UploadGuia, p => docDb.RutaGuia = p);
                await SaveFile(UploadInstructivo, p => docDb.RutaInstructivo = p);
                await SaveFile(UploadPacking, p => docDb.RutaPacking = p);
                await SaveFile(UploadCertExtra, p => docDb.RutaCertExtra = p);
                await SaveFile(UploadLibreVenta, p => docDb.RutaLibreVenta = p);
                await SaveFile(UploadDhl, p => docDb.RutaDhl = p);
                await SaveFile(UploadOri1, p => docDb.EvidenciaOrigen1 = p);
                await SaveFile(UploadOri2, p => docDb.EvidenciaOrigen2 = p);
                await SaveFile(UploadOri3, p => docDb.EvidenciaOrigen3 = p);
                await SaveFile(UploadOri4, p => docDb.EvidenciaOrigen4 = p);
                await SaveFile(UploadCertFito, p => docDb.EvidenciaFito1 = p);
                await SaveFile(UploadFit2, p => docDb.EvidenciaFito2 = p);
                await SaveFile(UploadFit3, p => docDb.EvidenciaFito3 = p);
                await SaveFile(UploadFit4, p => docDb.EvidenciaFito4 = p);
                await SaveFile(UploadCertSanitario, p => docDb.EvidenciaSanitario1 = p);
                await SaveFile(UploadSan2, p => docDb.EvidenciaSanitario2 = p);
                await SaveFile(UploadSan3, p => docDb.EvidenciaSanitario3 = p);
                await SaveFile(UploadSan4, p => docDb.EvidenciaSanitario4 = p);
                await SaveFile(UploadCod1, p => docDb.EvidenciaCod1 = p);
                await SaveFile(UploadCod2, p => docDb.EvidenciaCod2 = p);
                await SaveFile(UploadCod3, p => docDb.EvidenciaCod3 = p);
                await SaveFile(UploadCod4, p => docDb.EvidenciaCod4 = p);
                await SaveFile(UploadCla1, p => docDb.EvidenciaCla1 = p);
                await SaveFile(UploadCla2, p => docDb.EvidenciaCla2 = p);
                await SaveFile(UploadCla3, p => docDb.EvidenciaCla3 = p);
                await SaveFile(UploadCla4, p => docDb.EvidenciaCla4 = p);
                await SaveFile(UploadNep1, p => docDb.EvidenciaNep1 = p);
                await SaveFile(UploadNep2, p => docDb.EvidenciaNep2 = p);
                await SaveFile(UploadNep3, p => docDb.EvidenciaNep3 = p);
                await SaveFile(UploadNep4, p => docDb.EvidenciaNep4 = p);
                await SaveFile(UploadCap1, p => docDb.EvidenciaCap1 = p);
                await SaveFile(UploadCap2, p => docDb.EvidenciaCap2 = p);
                await SaveFile(UploadCap3, p => docDb.EvidenciaCap3 = p);
                await SaveFile(UploadCap4, p => docDb.EvidenciaCap4 = p);
                await SaveFile(UploadCoa, p => docDb.EvidenciaCoa1 = p);
                await SaveFile(UploadDt, p => docDb.EvidenciaDt1 = p);
                await SaveFile(UploadBookingAtilson, p => docDb.RutaBookingAtilson = p);
                await SaveFile(UploadFullSet, p => docDb.RutaFullSet = p);
                await SaveFile(UploadCapturaAga, p => docDb.RutaCapturaAga = p);

                docDb.ObsOrigen = F("docUpdates.ObsOrigen");
                docDb.ObsCaptura = F("docUpdates.ObsCaptura");
                docDb.LogOrigen = Request.Form["docUpdates.LogOrigen"].FirstOrDefault();
                docDb.LogFitosanitario = Request.Form["docUpdates.LogFitosanitario"].FirstOrDefault();
                docDb.LogSanitario = Request.Form["docUpdates.LogSanitario"].FirstOrDefault();
                docDb.LogCaptura = Request.Form["docUpdates.LogCaptura"].FirstOrDefault();
                docDb.LogCoa = Request.Form["docUpdates.LogCoa"].FirstOrDefault();
                docDb.LogDt = Request.Form["docUpdates.LogDt"].FirstOrDefault();
                docDb.LogCodaut = Request.Form["docUpdates.LogCodaut"].FirstOrDefault();
                docDb.LogClave = Request.Form["docUpdates.LogClave"].FirstOrDefault();
                docDb.LogNeppex = Request.Form["docUpdates.LogNeppex"].FirstOrDefault();

                op.FechaModificacion = DateTime.Now;
                op.UsuarioModificador = User.Identity?.Name ?? "Sistema";
                RegistrarHito(op, "DOCUMENTAL", $"Documentación guardada — DUS: {docDb.DusDin ?? "-"} | SAG: {(docDb.AplicaSag ? "Sí" : "No")} | Sernapesca: {(docDb.AplicaSernapesca ? "Sí" : "No")}");
                op.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync();

                // 🚀 CONEXIÓN AL MOTOR DE FINANZAS (Asegura la facturación correcta)
                await SincronizarTramitesAduanaAFinanzasAsync(IdOperacion);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = true });

                TempData["SuccessMsg"] = "Progreso documental guardado correctamente.";
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = ex.Message });
                TempData["ErrorMsg"] = "Error al guardar: " + ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEnviarFinanzasAsync(int id)
        {
            var operacion = await _context.Operaciones.FindAsync(id);
            if (operacion != null)
            {
                operacion.LockFinanzas = true;
                string notaEnvio = $"[{DateTime.Now:dd/MM/yyyy HH:mm} OPERACIONES] Documentación y extracostos cerrados y enviados a Finanzas.\n";
                operacion.Comentarios = notaEnvio + (operacion.Comentarios ?? "");
                operacion.FechaModificacion = DateTime.Now;
                operacion.UsuarioModificador = User.Identity?.Name ?? "Operaciones";
                RegistrarHito(operacion, "DOCUMENTAL", "Documentación cerrada y enviada a Finanzas. Módulo documental bloqueado.");
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = $"La operación {operacion.NumeroBooking} se ha enviado a Finanzas y su pestaña documental ha sido bloqueada.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGuardarCertificadoAsync(
            int IdOperacion, string CertKey, bool EnviarFinanzas, bool SinNumero,
            IFormFile? UploadCertA1, IFormFile? UploadCertA2, IFormFile? UploadCertA3, IFormFile? UploadCertA4)
        {
            var op = await _context.Operaciones
                .Include(o => o.OperacionesDocumentales)
                .FirstOrDefaultAsync(o => o.Id == IdOperacion);
            if (op == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

            var docDb = op.OperacionesDocumentales.OrderBy(x => x.Id).FirstOrDefault();
            if (docDb == null) return new JsonResult(new { success = false, message = "Sin registro documental" });

            string? F(string key) { var v = Request.Form[key].FirstOrDefault(); return string.IsNullOrWhiteSpace(v) ? null : v.Trim(); }
            decimal? FDec(string key)
            {
                var v = Request.Form[key].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(v)) return null;
                v = v.Replace(".", "").Replace(",", ".");
                return decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : null;
            }
            bool FBoolPreserve(string key, bool valorActual) => Request.Form.ContainsKey(key) ? Request.Form[key].Any(v => v == "true" || v == "on") : valorActual;
            int? FInt(string key) { var val = Request.Form[key].FirstOrDefault(); return int.TryParse(val, out var r) ? r : null; }

            string campoNumPrincipal = CertKey switch
            {
                "ori" => "docUpdates.NumOri1",
                "fit" => "docUpdates.NumFit1",
                "san" => "docUpdates.NumSan1",
                "cap" => "docUpdates.NumCap1",
                "coa" => "docUpdates.NumCoa1",
                "dt" => "docUpdates.NumDt1",
                "cod" => "docUpdates.NumCod1",
                "cla" => "docUpdates.NumCla1",
                "nep" => "docUpdates.NumNep1",
                _ => ""
            };

            if (EnviarFinanzas && !SinNumero && string.IsNullOrWhiteSpace(F(campoNumPrincipal)))
                return new JsonResult(new { success = false, message = "Falta N° de documento. Marca 'Enviar sin número' si corresponde." });

            switch (CertKey)
            {
                case "ori":
                    docDb.RemisionOrigen1 = F("docUpdates.NumOri1"); docDb.ValOri1 = FDec("docUpdates.ValOri1");
                    docDb.RemisionOrigen2 = F("docUpdates.NumOri2"); docDb.ValOri2 = FDec("docUpdates.ValOri2");
                    docDb.NumOri3 = F("docUpdates.NumOri3"); docDb.ValOri3 = FDec("docUpdates.ValOri3");
                    docDb.NumOri4 = F("docUpdates.NumOri4"); docDb.ValOri4 = FDec("docUpdates.ValOri4");
                    docDb.ObsOrigen = F("docUpdates.ObsOrigen"); docDb.AcuerdoOrigen = F("docUpdates.AcuerdoOrigen");
                    docDb.CertificadoOrigen = true; // Forza activación al guardar
                    break;
                case "fit":
                    docDb.CertFitosanitario = F("docUpdates.NumFit1"); docDb.ValFit1 = FDec("docUpdates.ValFit1");
                    docDb.NumFit2 = F("docUpdates.NumFit2"); docDb.ValFit2 = FDec("docUpdates.ValFit2");
                    docDb.NumFit3 = F("docUpdates.NumFit3"); docDb.ValFit3 = FDec("docUpdates.ValFit3");
                    docDb.NumFit4 = F("docUpdates.NumFit4"); docDb.ValFit4 = FDec("docUpdates.ValFit4");
                    docDb.AplicaSag = true; // Forza activación al guardar
                    break;
                case "san":
                    docDb.CertSanitario = F("docUpdates.NumSan1"); docDb.ValSan1 = FDec("docUpdates.ValSan1");
                    docDb.NumSan2 = F("docUpdates.NumSan2"); docDb.ValSan2 = FDec("docUpdates.ValSan2");
                    docDb.NumSan3 = F("docUpdates.NumSan3"); docDb.ValSan3 = FDec("docUpdates.ValSan3");
                    docDb.NumSan4 = F("docUpdates.NumSan4"); docDb.ValSan4 = FDec("docUpdates.ValSan4");
                    docDb.AplicaSernapesca = true; // Forza activación al guardar
                    break;
                case "cap":
                    docDb.NumCap1 = F("docUpdates.NumCap1"); docDb.ValCap1 = FDec("docUpdates.ValCap1");
                    docDb.NumCap2 = F("docUpdates.NumCap2"); docDb.ValCap2 = FDec("docUpdates.ValCap2");
                    docDb.NumCap3 = F("docUpdates.NumCap3"); docDb.ValCap3 = FDec("docUpdates.ValCap3");
                    docDb.NumCap4 = F("docUpdates.NumCap4"); docDb.ValCap4 = FDec("docUpdates.ValCap4");
                    docDb.ObsCaptura = F("docUpdates.ObsCaptura");
                    docDb.AplicaSernapesca = true; // Forza activación al guardar
                    break;
                case "aduana":
                    docDb.AplicaSag = FBoolPreserve("docUpdates.AplicaSag", docDb.AplicaSag);
                    docDb.AplicaSernapesca = FBoolPreserve("docUpdates.AplicaSernapesca", docDb.AplicaSernapesca);
                    docDb.IdAgenciaAduana = FInt("docUpdates.IdAgenciaAduana");
                    docDb.DusDin = F("docUpdates.Dus"); docDb.Din = F("docUpdates.Din");
                    docDb.EstadoDocumental = F("docUpdates.EstadoDocumental");
                    docDb.ValorDus = FDec("docUpdates.ValorDus"); docDb.ValorDin = FDec("docUpdates.ValorDin");
                    docDb.MatrizPresentada = FBoolPreserve("docUpdates.MatrizPresentada", docDb.MatrizPresentada == true);
                    docDb.ExtensionDocumental = FBoolPreserve("docUpdates.ExtensionDocumental", docDb.ExtensionDocumental == true);
                    docDb.ValorAclaracion = FDec("docUpdates.ValorAclaracion");
                    docDb.GuiaVisado = FBoolPreserve("docUpdates.GuiaVisado", docDb.GuiaVisado == true);
                    docDb.ValorCancelacion = FDec("docUpdates.ValorCancelacion");

                    // NUEVOS CAMPOS: Trámites extra e IVV
                    docDb.Roleo = FBoolPreserve("docUpdates.Roleo", docDb.Roleo == true);
                    docDb.ValorRoleo = FDec("docUpdates.ValorRoleo");
                    docDb.GeneracionIvv = FBoolPreserve("docUpdates.GeneracionIvv", docDb.GeneracionIvv == true);
                    break;
                case "coa": docDb.NumCoa1 = F("docUpdates.NumCoa1"); break;
                case "dt": docDb.NumDt1 = F("docUpdates.NumDt1"); break;
                case "cod": docDb.NumCod1 = F("docUpdates.NumCod1"); break;
                case "cla": docDb.NumCla1 = F("docUpdates.NumCla1"); break;
                case "nep": docDb.NumNep1 = F("docUpdates.NumNep1"); break;
                default: return new JsonResult(new { success = false, message = "Certificado no reconocido" });
            }

            if (CertKey == "ori" || CertKey == "fit" || CertKey == "san" || CertKey == "cap") ProcesarEsActivasSlots(docDb, CertKey);

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documentos");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            async Task SaveFile(IFormFile? file, Action<string> setPath)
            {
                if (file == null || file.Length == 0) return;
                string fileName = $"{op.Id}_{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName).Replace(" ", "_")}";
                using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                await file.CopyToAsync(stream);
                setPath($"/uploads/documentos/{fileName}");
            }

            string slotEsActiva = F("SlotEsActiva") ?? "1";
            Action<string> setEvidencia = (CertKey, slotEsActiva) switch
            {
                ("ori", "1") => p => docDb.EvidenciaOrigen1 = p,
                ("ori", "2") => p => docDb.EvidenciaOrigen2 = p,
                ("ori", "3") => p => docDb.EvidenciaOrigen3 = p,
                ("ori", "4") => p => docDb.EvidenciaOrigen4 = p,
                ("fit", "1") => p => docDb.EvidenciaFito1 = p,
                ("fit", "2") => p => docDb.EvidenciaFito2 = p,
                ("fit", "3") => p => docDb.EvidenciaFito3 = p,
                ("fit", "4") => p => docDb.EvidenciaFito4 = p,
                ("san", "1") => p => docDb.EvidenciaSanitario1 = p,
                ("san", "2") => p => docDb.EvidenciaSanitario2 = p,
                ("san", "3") => p => docDb.EvidenciaSanitario3 = p,
                ("san", "4") => p => docDb.EvidenciaSanitario4 = p,
                ("cap", "1") => p => docDb.EvidenciaCap1 = p,
                ("cap", "2") => p => docDb.EvidenciaCap2 = p,
                ("cap", "3") => p => docDb.EvidenciaCap3 = p,
                ("cap", "4") => p => docDb.EvidenciaCap4 = p,
                ("coa", _) => p => docDb.EvidenciaCoa1 = p,
                ("dt", _) => p => docDb.EvidenciaDt1 = p,
                ("cod", _) => p => docDb.EvidenciaCod1 = p,
                ("cla", _) => p => docDb.EvidenciaCla1 = p,
                ("nep", _) => p => docDb.EvidenciaNep1 = p,
                _ => p => { }
            };
            var archivo = Request.Form.Files.GetFile("UploadCertA1");
            await SaveFile(archivo, setEvidencia);

            string? logEntrante = Request.Form[$"docUpdates.Log{CapitalizarCert(CertKey)}"].FirstOrDefault();
            if (logEntrante != null) AsignarLogCert(docDb, CertKey, logEntrante);

            MarcarSinNumero(docDb, CertKey, SinNumero);
            if (EnviarFinanzas) BloquearCert(docDb, CertKey);

            op.FechaModificacion = DateTime.Now;
            op.UsuarioModificador = User.Identity?.Name ?? "Sistema";
            RegistrarHito(op, "DOCUMENTAL", $"Certificado [{CertKey.ToUpper()}] guardado" + (EnviarFinanzas ? " y enviado a Finanzas." : " como borrador.") + (SinNumero ? " Enviado SIN número." : ""));

            await _context.SaveChangesAsync();

            // 🚀 CONEXIÓN DIRECTA A FINANZAS 🚀
            await SincronizarTramitesAduanaAFinanzasAsync(IdOperacion);

            return new JsonResult(new { success = true, bloqueado = EnviarFinanzas, sinNumero = SinNumero });
        }

        private static readonly Dictionary<string, string> SufijoCampoCert = new() { { "ori", "Ori" }, { "fit", "Fit" }, { "san", "San" }, { "cap", "Cap" } };
        private void ProcesarEsActivasSlots(OperacionesDocumentale doc, string certKey)
        {
            if (!SufijoCampoCert.TryGetValue(certKey, out var suf)) return;
            var set = (doc.CertsBloqueados ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            for (int slot = 1; slot <= 4; slot++) { bool EsActiva = Request.Form[$"docUpdates.EsActiva{suf}{slot}"].Any(v => v == "true" || v == "on"); if (EsActiva) set.Add($"{certKey}{slot}"); }
            doc.CertsBloqueados = string.Join(",", set);
        }
        private string CapitalizarCert(string k) => k switch { "ori" => "Origen", "fit" => "Fitosanitario", "san" => "Sanitario", "cap" => "Captura", "coa" => "Coa", "dt" => "Dt", "cod" => "Codaut", "cla" => "Clave", "nep" => "Neppex", _ => "" };

        public async Task<IActionResult> OnPostUpdateMatrizAsync(int IdOperacion, bool MatrizPresentada, IFormFile? EvidenciaMatriz, IFormFile? EvidenciaEnvioNaviera, string? NumeroBL, IFormFile? ArchivoBL)
        {
            var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == IdOperacion);
            if (op != null)
            {
                var docDb = op.OperacionesDocumentales.OrderBy(x => x.Id).FirstOrDefault();
                if (docDb == null) { docDb = new OperacionesDocumentale { FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true }; op.OperacionesDocumentales.Add(docDb); }
                docDb.MatrizPresentada = MatrizPresentada;
                if (Request.Form.ContainsKey("docUpdates.LogMatriz")) docDb.LogMatriz = Request.Form["docUpdates.LogMatriz"].FirstOrDefault();
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                if (EvidenciaMatriz != null && EvidenciaMatriz.Length > 0) { string uNameMatriz = $"{op.Id}_Matriz_{DateTime.Now.Ticks}_{Path.GetFileName(EvidenciaMatriz.FileName).Replace(" ", "_")}"; using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameMatriz), FileMode.Create)) await EvidenciaMatriz.CopyToAsync(stream); docDb.EvidenciaMatriz = $"/uploads/evidencias/{uNameMatriz}"; }
                if (EvidenciaEnvioNaviera != null && EvidenciaEnvioNaviera.Length > 0) { string uNameEnvio = $"{op.Id}_EnvioNaviera_{DateTime.Now.Ticks}_{Path.GetFileName(EvidenciaEnvioNaviera.FileName).Replace(" ", "_")}"; using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameEnvio), FileMode.Create)) await EvidenciaEnvioNaviera.CopyToAsync(stream); }
                if (ArchivoBL != null && ArchivoBL.Length > 0) { string uNameBL = $"{op.Id}_BL_{DateTime.Now.Ticks}_{Path.GetFileName(ArchivoBL.FileName).Replace(" ", "_")}"; using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameBL), FileMode.Create)) await ArchivoBL.CopyToAsync(stream); }
                op.FechaModificacion = DateTime.Now; op.UsuarioModificador = User.Identity?.Name ?? "Sistema";
                RegistrarHito(op, "DOCUMENTAL", $"Matriz actualizada — Presentada: {(MatrizPresentada ? "✓ Confirmada" : "Pendiente")} | B/L: {NumeroBL ?? "-"}");
                op.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return new JsonResult(new { success = true });
                TempData["SuccessMsg"] = "Documentación de Matriz y B/L actualizada correctamente.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateLarAsync(int IdOperacion, string? EstadoLar, DateTime? LateArrival, DateTime? ElarDate, bool ContenedorIngresado, DateTime? LlegadaPuertoReal, IFormFile? EvidenciaLar)
        {
            var op = await _context.Operaciones.Include(o => o.ExtracostosOperacions).Include(o => o.OperacionesTerrestres).FirstOrDefaultAsync(o => o.Id == IdOperacion);
            if (op != null)
            {
                op.EstadoLar = EstadoLar; op.LateArrival = LateArrival; op.ElarDate = ElarDate; op.ContenedorIngresado = ContenedorIngresado;
                if (LlegadaPuertoReal.HasValue) { var terrDb = op.OperacionesTerrestres.FirstOrDefault(); if (terrDb == null) { terrDb = new OperacionesTerrestre { FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true }; op.OperacionesTerrestres.Add(terrDb); } terrDb.LlegadaPuerto = LlegadaPuertoReal.Value; }
                if (EvidenciaLar != null && EvidenciaLar.Length > 0) { string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias"); if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder); string uniqueLar = $"{op.Id}_LAR_{DateTime.Now.Ticks}_{Path.GetFileName(EvidenciaLar.FileName).Replace(" ", "_")}"; using (var stream = new FileStream(Path.Combine(uploadsFolder, uniqueLar), FileMode.Create)) { await EvidenciaLar.CopyToAsync(stream); } op.EvidenciaLar = $"/uploads/evidencias/{uniqueLar}"; }
                if (EstadoLar == "AUTORIZADO LAR" || EstadoLar == "INGRESADO CON LAR" || ElarDate.HasValue) { var costoLar = op.ExtracostosOperacions.FirstOrDefault(e => e.TipoCosto == "Late Arrival (LAR)"); string elarMsg = ElarDate.HasValue ? $" | ELAR: {ElarDate.Value.ToString("dd/MM/yyyy HH:mm")}" : ""; if (costoLar == null) { op.ExtracostosOperacions.Add(new ExtracostosOperacion { TipoCosto = "Late Arrival (LAR)", Motivo = $"LAR {(EstadoLar == "AUTORIZADO LAR" ? "Autorizado" : "Ingresado")}. Nuevo Cut-Off: {LateArrival?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}{elarMsg}", Monto = 0, Moneda = "USD", Evidencia = op.EvidenciaLar ?? "Generado automáticamente por sistema", FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" }); } else if (ElarDate.HasValue && !costoLar.Motivo.Contains("ELAR")) { costoLar.Motivo += elarMsg; } }
                RegistrarHito(op, "MARITIMO", $"LAR/ELAR actualizado — Estado: {EstadoLar ?? "Sin estado"} | LAR: {LateArrival?.ToString("dd/MM HH:mm") ?? "-"} | ELAR: {ElarDate?.ToString("dd/MM HH:mm") ?? "-"} | Ingresado: {(ContenedorIngresado ? "Sí" : "No")}");
                op.FechaModificacion = DateTime.Now; op.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return new JsonResult(new { success = true });
                TempData["SuccessMsg"] = "Gestión de Late Arrival registrada. Si fue autorizado, se notificó a Finanzas.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleIngresoAsync(int id, bool ingresado)
        {
            var operacion = await _context.Operaciones.FindAsync(id);
            if (operacion != null) { operacion.ContenedorIngresado = ingresado; operacion.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync(); return new JsonResult(new { success = true }); }
            return new JsonResult(new { success = false });
        }

        public async Task<IActionResult> OnPostUpdateWorkflowAsync(int id, string estado, DateTime? fechaAsociada)
        {
            var operacion = await _context.Operaciones.Include(o => o.ExtracostosOperacions).Include(o => o.OperacionesTerrestres).FirstOrDefaultAsync(o => o.Id == id);
            if (operacion != null)
            {
                operacion.EstadoWorkflow = estado;
                if (fechaAsociada.HasValue) { var terr = operacion.OperacionesTerrestres.FirstOrDefault(); if (terr == null) { terr = new OperacionesTerrestre { FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema", Activo = true }; operacion.OperacionesTerrestres.Add(terr); } if (estado.Contains("Arribado a Planta")) terr.LlegadaPlanta = fechaAsociada; else if (estado.Contains("Salida de Planta")) terr.SalidaPlanta = fechaAsociada; else if (estado.Contains("En Puerto")) terr.LlegadaPuerto = fechaAsociada.Value; else if (estado.Contains("Entregado a Stacking")) terr.SalidaPuerto = fechaAsociada; }
                if (estado.ToUpper().Contains("CANCELADO")) { bool existeCosto = operacion.ExtracostosOperacions.Any(e => e.TipoCosto.Contains("Cancelación")); if (!existeCosto) { operacion.ExtracostosOperacions.Add(new ExtracostosOperacion { TipoCosto = "Cancelación / Roleo Reserva", Motivo = "Operación cancelada desde el panel operativo. Revisar posibles multas o cobro de falso flete.", Monto = 0, Moneda = "USD", Evidencia = "Alerta generada por el sistema", FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" }); } }
                RegistrarHito(operacion, "GENERAL", $"Estado Workflow cambiado a: {estado}" + (fechaAsociada.HasValue ? $" — Fecha evento: {fechaAsociada.Value:dd/MM/yyyy HH:mm}" : ""));
                operacion.FechaModificacion = DateTime.Now; operacion.CorreoClienteEnviado = false;
                await _context.SaveChangesAsync(); return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false });
        }

        public async Task<IActionResult> OnPostAddCostoAsync(int id)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"; string nombreUsuario = User.Identity?.Name ?? "Sistema"; var tipo = Request.Form["NuevoCosto.TipoCosto"]; var motivo = Request.Form["NuevoCosto.Motivo"]; var responsable = Request.Form["NuevoCosto.Responsable"]; var file = Request.Form.Files.GetFile("NuevoCosto.Evidencia");
            if (!string.IsNullOrEmpty(tipo) && file != null && file.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias"); if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uName = $"{id}_Costo_{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName).Replace(" ", "_")}"; using (var s = new FileStream(Path.Combine(uploadsFolder, uName), FileMode.Create)) { await file.CopyToAsync(s); }
                var costo = new ExtracostosOperacion { IdOperacion = id, TipoCosto = tipo, Motivo = motivo, Responsable = responsable, Moneda = "USD", Monto = 0, Evidencia = $"/uploads/evidencias/{uName}", FechaCreacion = DateTime.Now, UsuarioCreador = nombreUsuario };
                _context.ExtracostosOperacions.Add(costo);
                var opCosto = await _context.Operaciones.FindAsync(id); if (opCosto != null) { RegistrarHito(opCosto, "GENERAL", $"Extracosto reportado — Tipo: {tipo} | Resp: {responsable} | Motivo: {motivo}"); }
                await _context.SaveChangesAsync();
                if (isAjax) return new JsonResult(new { success = true });
                TempData["SuccessMsg"] = "Incidencia financiera reportada exitosamente.";
            }
            else if (isAjax) return new JsonResult(new { success = false, message = "Faltan datos obligatorios (categoría o evidencia)." });
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateResponsableCostoAsync(int idCosto, string responsable)
        {
            var costo = await _context.ExtracostosOperacions.FindAsync(idCosto);
            if (costo != null) { costo.Responsable = responsable; await _context.SaveChangesAsync(); return new JsonResult(new { success = true }); }
            return new JsonResult(new { success = false });
        }

        public async Task<IActionResult> OnPostUpdateGateAsync(int IdOperacion, string? GestionaGate, string? PagoGate, string? TipoGate)
        {
            var op = await _context.Operaciones.FindAsync(IdOperacion);
            if (op != null) { op.GestionaGate = GestionaGate; op.PagoGate = PagoGate; op.TipoGate = TipoGate; op.CorreoClienteEnviado = false; await _context.SaveChangesAsync(); if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return new JsonResult(new { success = true }); TempData["SuccessMsg"] = "Configuración de Gate guardada con éxito."; }
            else if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return new JsonResult(new { success = false, message = "Operación no encontrada" });
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFinalizarServicioAsync(int id, DateTime? LlegadaPlanta, DateTime? SalidaPlanta, DateTime LlegadaPuerto, DateTime? SalidaPuerto, string? ResponsableLar, bool SorteoEscaner, IFormFile? EvidenciaEscaner, string correoCliente)
        {
            var op = await _context.Operaciones.Include(o => o.OperacionesTerrestres).Include(o => o.ExtracostosOperacions).Include(o => o.IdClienteNavigation).FirstOrDefaultAsync(o => o.Id == id);
            if (op == null) return NotFound();
            var terr = op.OperacionesTerrestres.FirstOrDefault();
            if (terr != null) { if (LlegadaPlanta.HasValue) terr.LlegadaPlanta = LlegadaPlanta; if (SalidaPlanta.HasValue) terr.SalidaPlanta = SalidaPlanta; terr.LlegadaPuerto = LlegadaPuerto; if (SalidaPuerto.HasValue) terr.SalidaPuerto = SalidaPuerto; terr.SorteoEscaner = SorteoEscaner; }
            double horasLibresPlanta = 7; if (double.TryParse(Request.Form["HorasLibresPlanta"], out double hp)) horasLibresPlanta = hp;
            double horasLibresPuerto = 3; if (double.TryParse(Request.Form["HorasLibresPuerto"], out double hpu)) horasLibresPuerto = hpu;
            if (LlegadaPlanta.HasValue && SalidaPlanta.HasValue) { var diff = SalidaPlanta.Value - LlegadaPlanta.Value; if (diff.TotalHours > horasLibresPlanta) { string m = $"Camión en planta desde {LlegadaPlanta.Value:HH:mm} hasta {SalidaPlanta.Value:HH:mm}. Total espera: {diff.TotalHours:0.1} hrs. (Límite {horasLibresPlanta}h)"; op.ExtracostosOperacions.Add(new ExtracostosOperacion { TipoCosto = "Sobreestadía Planta", Motivo = m, Monto = 0, Moneda = "USD", Evidencia = "Generado por sistema interno", FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" }); } }
            if (SalidaPuerto.HasValue) { var diffPuerto = SalidaPuerto.Value - LlegadaPuerto; if (diffPuerto.TotalHours > horasLibresPuerto) { string m = $"Camión en puerto desde {LlegadaPuerto:HH:mm} hasta {SalidaPuerto.Value:HH:mm}. Total espera: {diffPuerto.TotalHours:0.1} hrs. (Límite {horasLibresPuerto}h)"; op.ExtracostosOperacions.Add(new ExtracostosOperacion { TipoCosto = "Sobreestadía Puerto", Motivo = m, Monto = 0, Moneda = "USD", Evidencia = "Generado por sistema interno", FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" }); } }
            DateTime? cutOff = op.ElarDate ?? op.LateArrival ?? op.CutOffMatriz;
            if (cutOff.HasValue && LlegadaPuerto > cutOff.Value) { op.EstadoLar = "INGRESADO CON LAR"; string m = $"Ingreso a puerto {LlegadaPuerto:dd/MM HH:mm}, superando el Cut-Off. Criterio: {ResponsableLar}"; var extCosto = op.ExtracostosOperacions.FirstOrDefault(e => e.TipoCosto == "Late Arrival (LAR)"); if (extCosto == null) { op.ExtracostosOperacions.Add(new ExtracostosOperacion { TipoCosto = "Late Arrival (LAR)", Motivo = m, Monto = 0, Moneda = "USD", Evidencia = "Generado automáticamente por hora de acceso", FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" }); } }
            op.ContenedorIngresado = true;
            if (SorteoEscaner) { string pathEvidencia = "Pendiente de subir"; if (EvidenciaEscaner != null && EvidenciaEscaner.Length > 0) { string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias"); if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder); string uName = $"{id}_Escaner_{DateTime.Now.Ticks}_{Path.GetFileName(EvidenciaEscaner.FileName).Replace(" ", "_")}"; using (var stream = new FileStream(Path.Combine(uploadsFolder, uName), FileMode.Create)) { await EvidenciaEscaner.CopyToAsync(stream); } pathEvidencia = $"/uploads/evidencias/{uName}"; } op.ExtracostosOperacions.Add(new ExtracostosOperacion { TipoCosto = "Escáner de Aduana", Motivo = "Unidad seleccionada aleatoriamente para aforo.", Monto = 0, Moneda = "USD", Evidencia = pathEvidencia, FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" }); }
            RegistrarHito(op, "GENERAL", $"Servicio finalizado — Llegada Puerto: {LlegadaPuerto:dd/MM HH:mm} | LAR: {(cutOff.HasValue && LlegadaPuerto > cutOff.Value ? "Sí" : "No")} | Escáner: {(SorteoEscaner ? "Sí" : "No")}");
            op.EstadoWorkflow = "Entregado a Stacking"; op.FechaModificacion = DateTime.Now; op.CorreoClienteEnviado = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = $"Se ha procesado el cierre físico del Booking <strong>{op.NumeroBooking}</strong>.";
            return RedirectToPage("./Index");
        }

        public TimeSpan GetTravelTime(string? planta, string? puerto)
        {
            planta = (planta ?? "").ToUpper(); puerto = (puerto ?? "").ToUpper();
            if ((planta.Contains("BUIN") || planta.Contains("PAINE")) && puerto.Contains("VALPARAISO")) return TimeSpan.FromHours(3);
            if ((planta.Contains("MELIPILLA") || planta.Contains("CALERA")) && puerto.Contains("VALPARAISO")) return new TimeSpan(2, 45, 0);
            if (planta.Contains("SAN FELIPE") && puerto.Contains("VALPARAISO")) return new TimeSpan(2, 30, 0);
            if (planta.Contains("SAN FELIPE") && puerto.Contains("SAN ANTONIO")) return new TimeSpan(3, 45, 0);
            if ((planta.Contains("BUIN") || planta.Contains("PAINE")) && puerto.Contains("SAN ANTONIO")) return new TimeSpan(2, 25, 0);
            if (planta.Contains("ROMERAL") && puerto.Contains("SAN ANTONIO")) return new TimeSpan(4, 15, 0);
            if (planta.Contains("ROMERAL") && puerto.Contains("VALPARAISO")) return TimeSpan.FromHours(4);
            return TimeSpan.FromHours(2);
        }

        public void GenerarAlertasProactivas(Operacione op, OperacionesTerrestre? t, OperacionesDocumentale? d, bool reqMar, bool reqDoc, bool reqMatriz, bool reqTerrestre, out string alertMatriz, out string alertStacking, out string alertTransporte)
        {
            alertMatriz = ""; alertStacking = ""; alertTransporte = ""; DateTime now = DateTime.Now;
            if (reqMatriz && (d == null || d.MatrizPresentada != true) && op.CutOffMatriz.HasValue) { TimeSpan diff = op.CutOffMatriz.Value - now; if (diff.TotalHours <= 24 && diff.TotalHours > 12) alertMatriz = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-info text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block text-info'><i class='bi bi-info-circle me-1'></i> MATRIZ DOCUMENTAL</strong> Vence en {Math.Round(diff.TotalHours)} hrs ({op.CutOffMatriz.Value:dd/MM HH:mm}).</div>"; else if (diff.TotalHours <= 12 && diff.TotalHours > 3) alertMatriz = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-warning text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block' style='color:#d97706;'><i class='bi bi-exclamation-triangle me-1'></i> MATRIZ DOCUMENTAL</strong> Vence en {Math.Round(diff.TotalHours)} hrs. Urge confirmación.</div>"; else if (diff.TotalHours <= 3 && diff.TotalHours > 0) alertMatriz = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-danger text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block text-danger'><i class='bi bi-x-octagon me-1'></i> MATRIZ CRÍTICA</strong> Vence en {Math.Round(diff.TotalHours)} hrs. Riesgo de multa.</div>"; else if (diff.TotalHours <= 0) alertMatriz = $"<div class='p-2 mb-1 rounded bg-danger bg-opacity-10 border-start border-3 border-danger text-danger shadow-sm' style='font-size: 11px;'><strong class='d-block'><i class='bi bi-x-octagon-fill me-1'></i> MATRIZ VENCIDA</strong> ¡Corte finalizado! Gestión urgente.</div>"; }
            if (reqMar) { DateTime? deadline = op.LateArrival ?? op.CutOffMatriz; if (deadline.HasValue) { TimeSpan travelTime = GetTravelTime(t?.PlantaCarga, op.IdPuertoOrigenNavigation?.NombrePuerto); DateTime maxDeparture = deadline.Value.Subtract(travelTime); TimeSpan timeToDeparture = maxDeparture - now; if (timeToDeparture.TotalHours <= 3) { if (timeToDeparture.TotalHours <= 0) alertStacking = $"<div class='p-2 mb-1 rounded bg-danger bg-opacity-10 border-start border-3 border-danger text-danger shadow-sm alert-stacking' style='font-size: 11px;'><strong class='d-block'><i class='bi bi-truck me-1'></i> ATRASO A PUERTO</strong> Camión debió salir a las {maxDeparture:HH:mm}. <br/><span class='text-muted text-danger'>Riesgo de pérdida de nave.</span></div>"; else alertStacking = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-warning text-dark shadow-sm alert-stacking' style='font-size: 11px;'><strong class='d-block' style='color:#d97706;'><i class='bi bi-cone-striped me-1'></i> TRÁNSITO A PUERTO</strong> Salida límite: {maxDeparture:HH:mm}.</div>"; } } }
            if (reqTerrestre && op.FechaStacking.HasValue) { TimeSpan diffTrans = op.FechaStacking.Value - now; bool transportAsignado = !string.IsNullOrWhiteSpace(t?.EmpresaTransporte); bool asignacionNotificada = t?.AsignacionEnviada == true; bool solicitudEnviada = t?.SolicitudEnviada == true; if (transportAsignado || asignacionNotificada) { alertTransporte = ""; } else if (solicitudEnviada) { alertTransporte = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-primary text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block text-primary'><i class='bi bi-envelope-check me-1'></i> LOGÍSTICA EN PROCESO</strong> Solicitud enviada, esperando chofer.</div>"; } else { if (diffTrans.TotalDays <= 5 && diffTrans.TotalDays > 3) alertTransporte = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-info text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block text-info'><i class='bi bi-truck me-1'></i> LOGÍSTICA</strong> Faltan {diffTrans.TotalDays:0} días para Stacking. <br/><span class='text-muted'>Sugerencia: Solicitar transporte.</span></div>"; else if (diffTrans.TotalDays <= 3 && diffTrans.TotalDays > 2) alertTransporte = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-warning text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block' style='color:#d97706;'><i class='bi bi-truck me-1'></i> ALERTA LOGÍSTICA</strong> Faltan {diffTrans.TotalDays:0} días para Stacking. <br/><span class='text-muted'>Debe confirmar camión.</span></div>"; else if (diffTrans.TotalDays <= 2 && diffTrans.TotalDays > 1) alertTransporte = $"<div class='p-2 mb-1 rounded bg-light border-start border-3 border-danger text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block text-danger'><i class='bi bi-truck me-1'></i> LOGÍSTICA CRÍTICA</strong> Menos de 48 hrs y NO hay transporte.</div>"; else if (diffTrans.TotalDays <= 1 && diffTrans.TotalDays >= 0) alertTransporte = $"<div class='p-2 mb-1 rounded bg-danger bg-opacity-10 border-start border-3 border-danger text-danger shadow-sm' style='font-size: 11px;'><strong class='d-block'><i class='bi bi-truck me-1'></i> PELIGRO LOGÍSTICO</strong> Stacking abre mañana. <br/><span class='text-muted text-danger'>¡Asignación URGENTE!</span></div>"; else if (diffTrans.TotalDays < 0) alertTransporte = $"<div class='p-2 mb-1 rounded bg-danger text-white border-start border-3 border-dark shadow-sm' style='font-size: 11px;'><strong class='d-block'><i class='bi bi-exclamation-octagon-fill me-1'></i> LOGÍSTICA VENCIDA</strong> STACKING ABIERTO SIN CAMIÓN.</div>"; } }
            if (op.EtdPol.HasValue && (d == null || d.DhlEnviadoCliente != true)) { TimeSpan diffPostZarpe = now - op.EtdPol.Value; bool aplicaSernapesca = (d != null && d.AplicaSernapesca == true); if (aplicaSernapesca && diffPostZarpe.TotalDays >= 3) { alertMatriz += $"<div class='p-2 mb-1 mt-1 rounded bg-light border-start border-3 border-warning text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block' style='color:#d97706;'><i class='bi bi-exclamation-triangle me-1'></i> ALERTA DHL</strong> Han pasado {diffPostZarpe.Days} días post-zarpe.<span class='text-muted d-block'>Urge gestión con AGA.</span></div>"; } else if (!aplicaSernapesca && diffPostZarpe.TotalDays >= 5) { alertMatriz += $"<div class='p-2 mb-1 mt-1 rounded bg-light border-start border-3 border-warning text-dark shadow-sm' style='font-size: 11px;'><strong class='d-block' style='color:#d97706;'><i class='bi bi-exclamation-triangle me-1'></i> ALERTA DHL</strong> Han pasado {diffPostZarpe.Days} días post-zarpe.<span class='text-muted d-block'>Enviar Full Set a cliente.</span></div>"; } }
        }

        public async Task<IActionResult> OnGetBuscarEmpresaAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2) return new JsonResult(new List<object>());
            term = term.ToLower();
            var resultados = await _context.Proveedores.Where(p => p.Activo == 1 && (p.NombreProveedor.ToLower().Contains(term) || p.Rut.ToLower().Contains(term))).Take(10).Select(p => new { empresa = p.NombreProveedor, rut = p.Rut, correo = p.CorreoOperativo }).ToListAsync();
            return new JsonResult(resultados);
        }

        public async Task<IActionResult> OnGetBuscarConductorAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2) return new JsonResult(new List<object>());
            term = term.ToLower();
            var resultados = await _context.Conductores.Where(c => c.Activo == true && (c.Nombre.ToLower().Contains(term) || c.Patente.ToLower().Contains(term) || c.Rut.ToLower().Contains(term))).Take(10).Select(c => new { conductor = c.Nombre, rut = c.Rut, telefono = c.Telefono, patente = c.Patente }).ToListAsync();
            return new JsonResult(resultados);
        }

        public async Task<IActionResult> OnPostEnviarCorreoTransporteAsync(int id, string correoDestino, string copiaCc, string asunto, string cuerpoHtml, string tipoCorreo)
        {
            try
            {
                var op = await _context.Operaciones.FindAsync(id);
                if (op == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

                string htmlFinal = $@"
                    <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <div style='background: linear-gradient(135deg, #0f172a 0%, #6d2d9e 100%); padding: 25px; text-align: center;'>
                            <h2 style='color: #fff; margin: 0; letter-spacing: 1px;'>Notificación Logística</h2>
                        </div>
                        <div style='padding: 30px; background-color: #ffffff;'>
                            <p style='font-size: 14px; line-height: 1.6; color: #475569;'>{cuerpoHtml.Replace("\n", "<br>")}</p>
                            <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'>
                            <p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>Atilson Cargo SpA.</p>
                        </div>
                    </div>";

                await _emailService.EnviarCorreoAsync(correoDestino, asunto, htmlFinal);
                RegistrarHito(op, "TRANSPORTE", $"Correo de transporte ({tipoCorreo}) enviado a: {correoDestino}");
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex) { return new JsonResult(new { success = false, message = ex.Message }); }
        }

        private void RegistrarHito(Operacione op, string modulo, string descripcion)
        {
            string usuario = User.Identity?.Name ?? "Sistema";
            string fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            string linea = $"{fecha}|{usuario}|{modulo}|{descripcion}\n";
            op.HistorialCorreos = linea + (op.HistorialCorreos ?? "");
        }

        private void RegistrarTransaccion(int idOperacion, string tipoMovimiento, string concepto, int? idProveedor, int? idCliente, decimal monto, string moneda, string? numeroDocumento = null)
        {
            var transaccion = _context.TransaccionesFinancieras.FirstOrDefault(t => t.IdOperacion == idOperacion && t.Concepto == concepto && t.TipoMovimiento == tipoMovimiento);
            if (transaccion != null)
            {
                if (transaccion.EstadoFila == "PROVISION" || transaccion.EstadoFila == "PROVISIÓN")
                {
                    transaccion.MontoNeto = monto;
                    transaccion.Moneda = moneda;
                    transaccion.IdProveedor = idProveedor;
                    transaccion.IdCliente = idCliente;
                    transaccion.FechaModificacion = DateTime.Now;
                    transaccion.UsuarioModificador = User.Identity?.Name ?? "Sistema";
                }
            }
            else
            {
                _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                {
                    IdOperacion = idOperacion,
                    TipoMovimiento = tipoMovimiento,
                    Concepto = concepto,
                    IdProveedor = idProveedor,
                    IdCliente = idCliente,
                    MontoNeto = monto,
                    Moneda = moneda,
                    EstadoFila = "PROVISION",
                    NumeroDocumento = numeroDocumento,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreador = User.Identity?.Name ?? "Sistema"
                });
            }
        }

        private void RegistrarOrCrearExtracostoDoc(Operacione op, string nombreBase, string? numero, string? evidencia, string? sufijo = null)
        {
            if (string.IsNullOrWhiteSpace(numero) && string.IsNullOrWhiteSpace(evidencia)) return;
            string nombreItem = string.IsNullOrWhiteSpace(sufijo) ? nombreBase : $"{nombreBase} {sufijo}";
            string tipoCostoKey = $"Documental: {nombreItem}";
            string motivo = !string.IsNullOrWhiteSpace(numero) ? $"N°/Ref: {numero}" : "Documento adjuntado";
            var existente = op.ExtracostosOperacions?.FirstOrDefault(e => e.TipoCosto == tipoCostoKey);
            if (existente != null) { existente.Motivo = motivo; if (!string.IsNullOrWhiteSpace(evidencia)) existente.Evidencia = evidencia; }
            else
            {
                _context.ExtracostosOperacions.Add(new ExtracostosOperacion { IdOperacion = op.Id, TipoCosto = tipoCostoKey, Motivo = motivo, Monto = 0, Moneda = "USD", Evidencia = evidencia ?? "Pendiente de evidencia", FechaCreacion = DateTime.Now, UsuarioCreador = User.Identity?.Name ?? "Sistema" });
            }
        }

        private bool CertEstaBloqueado(OperacionesDocumentale doc, string certKey) => (doc.CertsBloqueados ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(certKey);
        private void BloquearCert(OperacionesDocumentale doc, string certKey) { var set = (doc.CertsBloqueados ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(); set.Add(certKey); doc.CertsBloqueados = string.Join(",", set); }
        private void MarcarSinNumero(OperacionesDocumentale doc, string certKey, bool EsActiva) { var set = (doc.CertsSinNumero ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(); if (EsActiva) set.Add(certKey); else set.Remove(certKey); doc.CertsSinNumero = string.Join(",", set); }
        private void AsignarLogCert(OperacionesDocumentale doc, string certKey, string? log)
        {
            switch (certKey) { case "ori": doc.LogOrigen = log; break; case "fit": doc.LogFitosanitario = log; break; case "san": doc.LogSanitario = log; break; case "cap": doc.LogCaptura = log; break; case "coa": doc.LogCoa = log; break; case "dt": doc.LogDt = log; break; case "cod": doc.LogCodaut = log; break; case "cla": doc.LogClave = log; break; case "nep": doc.LogNeppex = log; break; }
        }

        public async Task<IActionResult> OnPostEnviarCorreoReservaAsync(int id, string correoDestino, int nuevaVersion)
        {
            try
            {
                var op = await _context.Operaciones.Include(o => o.IdClienteNavigation).Include(o => o.IdNavieraNavigation).Include(o => o.IdPuertoOrigenNavigation).Include(o => o.IdPuertoDestinoNavigation).Include(o => o.OperacionesTerrestres).FirstOrDefaultAsync(o => o.Id == id);
                if (op == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });
                var t = op.OperacionesTerrestres?.FirstOrDefault();

                string cliente = op.IdClienteNavigation?.RazonSocial ?? "Cliente"; string booking = op.NumeroBooking ?? "S/N"; string naviera = op.IdNavieraNavigation?.NombreNaviera ?? "-"; string nave = op.Nave ?? "Por confirmar"; string pol = op.IdPuertoOrigenNavigation?.NombrePuerto ?? "-"; string pod = op.IdPuertoDestinoNavigation?.NombrePuerto ?? "-"; string etd = op.EtdPol?.ToString("dd/MM/yyyy") ?? "Por confirmar"; string eta = op.EtaPod?.ToString("dd/MM/yyyy") ?? "Por confirmar"; string contenedor = op.NumeroContenedor ?? "Por asignar"; string sello = op.NumeroSello ?? "Por asignar"; string tipoConte = op.TipoContenedor ?? "-"; string cutOff = op.CutOffMatriz?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente"; string stacking = op.FechaStacking?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente"; string lar = op.LateArrival?.ToString("dd/MM/yyyy HH:mm") ?? "No activado"; string planta = t?.PlantaCarga ?? "-"; string deposito = t?.DepositoRetiro ?? "-"; string conductor = t?.NombreConductor ?? "-"; string patente = t?.Patente ?? "-"; string fechaCarga = t?.FechaCarga?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                int s = op.IdTipoServicio ?? 0; string nombreServicio = s switch { 1 => "Exportación Integral", 2 => "Exportación Marítimo + Terrestre", 3 => "Exportación Marítimo + Documental", 5 => "Exportación Marítimo", 6 => "Exportación Terrestre", 8 => "Importación Integral", 12 => "Importación Marítimo", _ => "Por confirmar" };

                var req = HttpContext.Request; string baseUrl = $"{req.Scheme}://{req.Host}"; string pdfUrl = $"{baseUrl}/Operaciones/ReservaPdf/{id}";
                string versionTxt = nuevaVersion > 1 ? $" (V{nuevaVersion} — ACTUALIZACIÓN)" : ""; string asunto = $"[ATILSON CARGO] Confirmación de Reserva — BKG: {booking}{versionTxt}";
                string rowAlt = "background:#f8fafc;"; string td1 = "padding:10px 14px; border-bottom:0.5px solid #e2e8f0; font-size:13px; color:#64748b; width:200px;"; string td2 = "padding:10px 14px; border-bottom:0.5px solid #e2e8f0; font-size:13px; font-weight:700; color:#0f172a;"; string tdCrit = "padding:10px 14px; border-bottom:0.5px solid #e2e8f0; font-size:13px; font-weight:700; color:#dc2626;"; string secHead = "font-size:11px; font-weight:800; text-transform:uppercase; letter-spacing:0.8px; color:#6d2d9e; margin:24px 0 8px; padding-bottom:6px; border-bottom:2px solid #f3e8ff;";
                string alertaBanner = nuevaVersion > 1 ? $@"<div style='background:#fef3c7; border-left:4px solid #d97706; padding:10px 16px; margin-bottom:20px; border-radius:4px; font-size:13px; color:#92400e;'><strong>Actualización V{nuevaVersion}:</strong> Este documento reemplaza la confirmación anterior del Booking {booking}.</div>" : "";

                string html = $@"<div style='font-family:Arial,sans-serif; max-width:640px; margin:0 auto;'><div style='background:linear-gradient(135deg,#0f172a 0%,#6d2d9e 100%); padding:28px 32px; border-radius:8px 8px 0 0;'><p style='color:#cbd5e1; font-size:10px; text-transform:uppercase; letter-spacing:1px; margin:0 0 4px;'>Atilson Cargo SpA — Confirmación Logística</p><h1 style='color:#fff; font-size:22px; margin:0; letter-spacing:-0.3px;'>Reserva Confirmada</h1><p style='color:#a78bfa; font-size:13px; margin:6px 0 0;'>Booking: <strong style='color:#fff;'>{booking}</strong>  ·  {nombreServicio}</p></div><div style='background:#fff; padding:28px 32px; border:0.5px solid #e2e8f0; border-top:none; border-radius:0 0 8px 8px;'>{alertaBanner}<p style='font-size:15px; color:#0f172a;'>Estimado/a <strong>{cliente}</strong>,</p><p style='font-size:14px; color:#475569; line-height:1.6; margin-bottom:0;'>A continuación encontrará el detalle completo de su reserva. Para descargar el documento oficial en PDF, haga clic en el botón al final de este correo.</p><h2 style='{secHead}'>Itinerario marítimo</h2><table style='width:100%; border-collapse:collapse; border:0.5px solid #e2e8f0; border-radius:6px;'><tr><td style='{td1}'>Naviera</td><td style='{td2}'>{naviera}</td></tr><tr style='{rowAlt}'><td style='{td1}'>Nave / Viaje</td><td style='{td2}'>{nave}</td></tr><tr><td style='{td1}'>Puerto Origen (POL)</td><td style='{td2}'>{pol}</td></tr><tr style='{rowAlt}'><td style='{td1}'>Puerto Destino (POD)</td><td style='{td2}'>{pod}</td></tr><tr><td style='{td1}'>ETD — Zarpe estimado</td><td style='{td2}'>{etd}</td></tr><tr style='{rowAlt}'><td style='{td1}'>ETA — Arribo estimado</td><td style='{td2}'>{eta}</td></tr><tr><td style='{td1}'>Tipo de contenedor</td><td style='{td2}'>{tipoConte}</td></tr><tr style='{rowAlt}'><td style='{td1}'>N° Contenedor</td><td style='{td2}'>{contenedor}</td></tr><tr><td style='{td1}'>N° Sello</td><td style='{td2}'>{sello}</td></tr></table><h2 style='{secHead}'>Fechas clave</h2><table style='width:100%; border-collapse:collapse; border:0.5px solid #e2e8f0; border-radius:6px;'><tr><td style='{td1}'>Apertura Stacking (IN)</td><td style='{td2}'>{stacking}</td></tr><tr style='{rowAlt}'><td style='{td1}'>Cut-Off Físico (OUT)</td><td style='{tdCrit}'>{cutOff}</td></tr><tr><td style='{td1}'>Late Arrival (LAR)</td><td style='{td2}'>{lar}</td></tr></table><h2 style='{secHead}'>Logística terrestre</h2><table style='width:100%; border-collapse:collapse; border:0.5px solid #e2e8f0; border-radius:6px;'><tr><td style='{td1}'>Planta de carga</td><td style='{td2}'>{planta}</td></tr><tr style='{rowAlt}'><td style='{td1}'>Depósito retiro vacío</td><td style='{td2}'>{deposito}</td></tr><tr><td style='{td1}'>Fecha presentación planta</td><td style='{td2}'>{fechaCarga}</td></tr><tr style='{rowAlt}'><td style='{td1}'>Conductor asignado</td><td style='{td2}'>{conductor}</td></tr><tr><td style='{td1}'>Patente tracto</td><td style='{td2}'>{patente}</td></tr></table><div style='text-align:center; margin:32px 0 8px;'><a href='{pdfUrl}' target='_blank' style='background:#0f172a; color:#fff; padding:14px 32px; text-decoration:none; border-radius:6px; font-weight:800; font-size:14px; display:inline-block; letter-spacing:0.3px;'>Descargar documento PDF</a></div><p style='font-size:11px; color:#94a3b8; text-align:center; margin:6px 0 0;'>El link abrirá el documento directamente en su navegador.</p><hr style='border:0; border-top:0.5px solid #e2e8f0; margin:28px 0;'><p style='font-size:11px; color:#94a3b8; text-align:center; margin:0;'><strong>Atilson Cargo SpA</strong> · Operaciones Logísticas<br>Ante cualquier consulta responda este correo electrónico.</p></div></div>";

                await _emailService.EnviarCorreoAsync(correoDestino, asunto, html);
                op.CorreoClienteEnviado = true; op.VersionCorreo = nuevaVersion; string nota = $"[{DateTime.Now:dd/MM/yyyy HH:mm} OPERACIONES] Confirmación V{nuevaVersion} enviada a {correoDestino}.\n"; op.Comentarios = nota + (op.Comentarios ?? ""); RegistrarHito(op, "GENERAL", $"Correo reserva V{nuevaVersion} enviado a: {correoDestino}");
                await _context.SaveChangesAsync(); return new JsonResult(new { success = true });
            }
            catch (Exception ex) { return new JsonResult(new { success = false, message = ex.Message }); }
        }

        public async Task<IActionResult> OnPostEnviarBorradorBLAsync(string idOperacion, string correoDestino, IFormFile? ArchivoBL)
        {
            try
            {
                int realId = int.Parse(idOperacion.Replace("sub-", ""));
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == realId);
                if (op == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

                var doc = op.OperacionesDocumentales.FirstOrDefault();
                if (doc != null && doc.EstadoDocumental != "B/L APROBADO" && doc.EstadoDocumental != "CAMBIOS B/L") doc.EstadoDocumental = "BORRADOR ENVIADO";

                var req = HttpContext.Request; string baseUrl = $"{req.Scheme}://{req.Host}";
                string urlAprobar = $"{baseUrl}/Operaciones/Index?handler=RespuestaBorradorBL&id={idOperacion}&respuesta=aprobar"; string urlRechazar = $"{baseUrl}/Operaciones/Index?handler=RespuestaBorradorBL&id={idOperacion}&respuesta=rechazar";
                string html = $@"<div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px;'><div style='background: linear-gradient(135deg, #0f172a 0%, #6d2d9e 100%); padding: 25px; text-align: center; border-radius: 8px 8px 0 0;'><h2 style='color: #fff; margin: 0;'>Revisión de Borrador de B/L</h2><p style='color:#cbd5e1; margin-top:5px;'>Booking: <strong>{op.NumeroBooking}</strong></p></div><div style='padding: 30px; background-color: #ffffff;'><p>Estimado cliente, junto con saludar,</p><p>A continuación enviamos borrador de BL adjunto para su revisión y aprobación.</p><p>En caso de aprobar o requerir modificaciones, haga click en los botones correspondientes a continuación (Esto actualizará automáticamente la plataforma logística):</p><div style='text-align: center; margin-top: 40px; margin-bottom: 20px;'><a href='{urlAprobar}' style='background-color: #16a34a; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; margin-right: 10px; display: inline-block;'>APROBAR DRAFT</a><a href='{urlRechazar}' style='background-color: #dc2626; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>RECHAZAR / SOLICITAR CAMBIO</a></div><hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'><p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>Atilson Cargo SpA.</p></div></div>";

                await _emailService.EnviarCorreoAsync(correoDestino, $"[ATILSON CARGO] Borrador BL para Revisión - BKG {op.NumeroBooking}", html);
                RegistrarHito(op, "DOCUMENTAL", $"Borrador de BL enviado al cliente para revisión a: {correoDestino}");
                await _context.SaveChangesAsync(); return new JsonResult(new { success = true });
            }
            catch (Exception ex) { return new JsonResult(new { success = false, message = ex.Message }); }
        }

        public async Task<IActionResult> OnGetRespuestaSanitarioAgenciaAsync(int id)
        {
            try
            {
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == id);
                if (op == null) return Content("Operación no encontrada.");
                var doc = op.OperacionesDocumentales.FirstOrDefault();
                string logEntry = $"<div style='border-left:2px solid #0ea5e9; padding:6px; margin-bottom:6px; background:#f0f9ff; border-top:1px solid #bae6fd; border-right:1px solid #bae6fd; border-bottom:1px solid #bae6fd;'><div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'><span style='font-weight:800; color:#0284c7; text-transform:uppercase; font-size:10px;'>📋 AGA-SERN CONFIRMÓ RECEPCIÓN</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Agencia Sernapesca</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>La agencia confirmó la recepción del Certificado Sanitario para su trámite. El cierre del <strong>Certificado Final</strong> lo realiza Operaciones al adjuntar el documento definitivo.</div></div>\n";
                if (doc != null) doc.LogSanitario = logEntry + (doc.LogSanitario ?? "");
                RegistrarHito(op, "DOCUMENTAL", "Agencia Sernapesca confirmó recepción del Certificado Sanitario."); op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
                string htmlResponse = $@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><title>Recepción Confirmada — Atilson Cargo</title><style>body{{font-family:'Segoe UI',sans-serif;background:#f1f5f9;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;}}.card{{background:#fff;padding:40px;border-radius:12px;box-shadow:0 10px 25px rgba(0,0,0,.1);text-align:center;max-width:450px;border-top:6px solid #0ea5e9;}}h1{{color:#0ea5e9;margin-top:0;}}p{{color:#475569;line-height:1.6;font-size:15px;}}</style></head><body><div class='card'><h1>📋 Recepción Confirmada</h1><p>Hemos registrado la recepción del Certificado Sanitario en el sistema de Atilson Cargo SpA.</p><p style='font-size:13px;color:#94a3b8;margin-top:30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>";
                return Content(htmlResponse, "text/html");
            }
            catch (Exception ex) { return Content($"Error al procesar: {ex.Message}"); }
        }

        public async Task<IActionResult> OnGetRespuestaAgenciaAsync(int id, string estado, string tipoDoc)
        {
            try
            {
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == id);
                if (op == null) return Content("Operación no encontrada.");
                var doc = op.OperacionesDocumentales.FirstOrDefault();
                string textoEstado = estado == "visado" ? "Visado" : (tipoDoc == "guia" ? "En Proceso de Visación" : "Recepcionado / En Proceso");
                string color = estado == "visado" ? "#16a34a" : "#0ea5e9"; string colorBg = estado == "visado" ? "#f0fdf4" : "#f0f9ff"; string colorBorder = estado == "visado" ? "#bbf7d0" : "#bae6fd"; string emoji = estado == "visado" ? "✅" : "📋";
                string nombreCompleto = tipoDoc switch { "guia" => "Guía de Despacho", "instructivo" => "Instructivo de Embarque", "booking" => "Booking Atilson", "capturaAga" => "Certificado de Captura", "libreven" => "Certificado de Libre Venta", _ => "Documento AGA" };

                string logEntry = $"<div style='border-left:2px solid {color}; padding:6px; margin-bottom:6px; background:{colorBg}; border-top:1px solid {colorBorder}; border-right:1px solid {colorBorder}; border-bottom:1px solid {colorBorder};'><div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'><span style='font-weight:800; color:{color}; text-transform:uppercase; font-size:10px;'>{emoji} {textoEstado.ToUpper()}</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Agencia de Aduanas</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>La agencia ha confirmado: <strong style='color:{color};'>{textoEstado}</strong></div></div>\n";
                if (doc != null)
                {
                    if (tipoDoc == "guia") doc.LogGuia = logEntry + (doc.LogGuia ?? "");
                    else if (tipoDoc == "instructivo") doc.LogInstructivo = logEntry + (doc.LogInstructivo ?? "");
                    else if (tipoDoc == "booking") doc.LogBookingAtilson = logEntry + (doc.LogBookingAtilson ?? "");
                    else if (tipoDoc == "capturaAga") doc.LogCapturaAga = logEntry + (doc.LogCapturaAga ?? "");
                    else if (tipoDoc == "libreven") doc.LogLibreVenta = logEntry + (doc.LogLibreVenta ?? "");
                }

                RegistrarHito(op, "DOCUMENTAL", $"Agencia confirmó estado de {nombreCompleto}: {textoEstado}");
                op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();

                // TEMPORIZADOR INTELIGENTE
                if (estado == "proceso" && tipoDoc == "guia")
                {
                    var req = HttpContext.Request; string baseUrl = $"{req.Scheme}://{req.Host}"; int idOperacion = op.Id; string bookingStr = op.NumeroBooking ?? "S/N";
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(30)); // 30 Minutos Reales
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<AtilsonContext>();
                                var emailSvc = scope.ServiceProvider.GetRequiredService<EmailService>();
                                var opActual = await db.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == idOperacion);
                                if (opActual != null)
                                {
                                    var docActual = opActual.OperacionesDocumentales.FirstOrDefault();
                                    string logActual = docActual?.LogGuia?.ToUpper() ?? "";
                                    if (!logActual.Contains("VISADO") && !logActual.Contains("✅"))
                                    {
                                        string linkVisado = $"{baseUrl}/Operaciones/Index?handler=RespuestaAgencia&id={idOperacion}&estado=visado&tipoDoc={tipoDoc}";
                                        string correoDestino = "agencia@aduanas.cl"; // <-- CAMBIAR POR EL REAL
                                        string htmlRecordatorio = $@"<div style='font-family:Arial,sans-serif;max-width:580px;margin:0 auto;'><div style='background:linear-gradient(135deg,#b91c1c 0%,#7f1d1d 100%);padding:24px 28px;border-radius:8px 8px 0 0;text-align:center;'><h2 style='color:#fff;margin:0;font-size:18px;'>⏰ Recordatorio: Visación Pendiente</h2><p style='color:#fecaca;margin:6px 0 0;font-size:13px;'>Booking: <strong style='color:#fff;'>{bookingStr}</strong></p></div><div style='background:#fff;padding:28px;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 8px 8px;'><p style='font-size:14px;color:#475569;line-height:1.6;'>Estimado equipo de Agencia,</p><p style='font-size:14px;color:#475569;line-height:1.6;'>El sistema detecta que ha pasado el tiempo estimado desde que se marcó el inicio del proceso de visación para <strong>{nombreCompleto}</strong> del Booking <strong style='color:#0f172a;'>{bookingStr}</strong>, y aún no se ha confirmado su término.</p><div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:20px;margin:24px 0;text-align:center;'><p style='font-size:13px;font-weight:700;color:#0f172a;text-transform:uppercase;margin:0 0 14px;'>Si el documento ya fue visado, por favor confírmelo aquí:</p><a href='{linkVisado}' style='background:#16a34a;color:#fff;padding:14px 22px;text-decoration:none;border-radius:6px;font-weight:800;font-size:13px;display:inline-block;'>✅ CONFIRMAR {nombreCompleto.ToUpper()} VISADO</a></div></div></div>";
                                        await emailSvc.EnviarCorreoAsync(correoDestino, $"[ALERTA AUTOMÁTICA] Visación Pendiente ({nombreCompleto}) — BKG: {bookingStr}", htmlRecordatorio);
                                        string logAuto = $"<div style='border-left:2px solid #f59e0b; padding:6px; margin-bottom:6px; background:#fffbeb; border-top:1px solid #fde68a; border-right:1px solid #fde68a; border-bottom:1px solid #fde68a;'><div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'><span style='font-weight:800; color:#d97706; text-transform:uppercase; font-size:10px;'>⏰ RECORDATORIO ENVIADO</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Sistema Automático</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>Alerta por demora en {nombreCompleto} enviada a agencia.</div></div>\n";
                                        if (docActual != null) { docActual.LogGuia = logAuto + docActual.LogGuia; }
                                        await db.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<AtilsonContext>(); var opError = await db.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == idOperacion);
                                if (opError != null) { var docError = opError.OperacionesDocumentales.FirstOrDefault(); if (docError != null) { string logError = $"<div style='border-left:2px solid #dc2626; padding:6px; margin-bottom:6px; background:#fef2f2;'><div style='font-weight:800; color:#dc2626; font-size:10px;'>❌ ERROR EN TEMPORIZADOR ({nombreCompleto})</div><div style='font-size:10px; color:#1e293b;'>{ex.Message}</div></div>\n"; docError.LogGuia = logError + docError.LogGuia; await db.SaveChangesAsync(); } }
                            }
                        }
                    });
                }

                string htmlResponse = $@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>{textoEstado} — Atilson Cargo</title><style>body{{font-family:'Segoe UI',sans-serif;background:#f1f5f9;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;}}.card{{background:#fff;padding:40px;border-radius:12px;box-shadow:0 10px 25px rgba(0,0,0,.1);text-align:center;max-width:450px;border-top:6px solid {color};}}h1{{color:{color};margin-top:0;}}p{{color:#475569;line-height:1.6;font-size:15px;}}</style></head><body><div class='card'><h1>{emoji} {textoEstado}</h1><p>Hemos registrado la confirmación del documento <strong>{nombreCompleto}</strong> en el sistema de Atilson Cargo SpA.<br>Nuestros operadores han sido notificados automáticamente.</p><p style='font-size:13px;color:#94a3b8;margin-top:30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>";
                return Content(htmlResponse, "text/html");
            }
            catch (Exception ex) { return Content($"Error al procesar: {ex.Message}"); }
        }

        public async Task<IActionResult> OnGetGuardarCambiosBLAsync(int id, string comentariosCliente)
        {
            try
            {
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == id);
                if (op != null)
                {
                    var doc = op.OperacionesDocumentales.FirstOrDefault();
                    if (doc != null) doc.EstadoDocumental = "CAMBIOS B/L";
                    RegistrarHito(op, "DOCUMENTAL", $"El cliente ha RECHAZADO el Borrador de B/L y solicita los siguientes cambios: \"{comentariosCliente}\"");
                    op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
                    string htmlResponse = $@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Cambios Registrados - Atilson Cargo</title><style>body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f1f5f9; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; padding: 20px; }} .card {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); text-align: center; max-width: 450px; border-top: 6px solid #eab308; }} h1 {{ color: #eab308; margin-top: 0; }} p {{ color: #475569; line-height: 1.6; font-size: 15px; }}</style></head><body><div class='card'><h1>¡Correcciones Registradas!</h1><p>Hemos guardado sus comentarios exitosamente. Nuestro equipo de operaciones ha sido notificado y gestionará la corrección con la naviera a la brevedad.</p><p style='font-size: 13px; color: #94a3b8; margin-top: 30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>";
                    return Content(htmlResponse, "text/html");
                }
                return Content("Operación no encontrada.");
            }
            catch { return Content("Error al procesar su solicitud."); }
        }

        public async Task<IActionResult> OnPostEnviarCertificadoClienteAsync(string idOperacion, string tipo, string correoDestino)
        {
            try
            {
                int realId = int.Parse(idOperacion.Replace("sub-", "").Replace("v3-", ""));
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == realId);
                if (op == null) return new JsonResult(new { success = false, message = "Operación no encontrada" });

                var doc = op.OperacionesDocumentales.FirstOrDefault();
                string nombreCert = tipo == "san" ? "Sanitario" : "de Captura";
                string? linkArchivo = null; int version = 1;

                if (tipo == "san") { if (!string.IsNullOrEmpty(doc?.EvidenciaSanitario4)) { linkArchivo = doc.EvidenciaSanitario4; version = 4; } else if (!string.IsNullOrEmpty(doc?.EvidenciaSanitario3)) { linkArchivo = doc.EvidenciaSanitario3; version = 3; } else if (!string.IsNullOrEmpty(doc?.EvidenciaSanitario2)) { linkArchivo = doc.EvidenciaSanitario2; version = 2; } else { linkArchivo = doc?.EvidenciaSanitario1; version = 1; } }
                else { if (!string.IsNullOrEmpty(doc?.EvidenciaCap4)) { linkArchivo = doc.EvidenciaCap4; version = 4; } else if (!string.IsNullOrEmpty(doc?.EvidenciaCap3)) { linkArchivo = doc.EvidenciaCap3; version = 3; } else if (!string.IsNullOrEmpty(doc?.EvidenciaCap2)) { linkArchivo = doc.EvidenciaCap2; version = 2; } else { linkArchivo = doc?.EvidenciaCap1; version = 1; } }

                if (string.IsNullOrEmpty(linkArchivo)) return new JsonResult(new { success = false, message = "No hay ningún documento guardado para enviar." });

                var req = HttpContext.Request; string baseUrl = $"{req.Scheme}://{req.Host}"; string urlDescarga = $"{baseUrl}{linkArchivo}";
                string urlAprobar = $"{baseUrl}/Operaciones/Index?handler=RespuestaCertificadoCliente&id={idOperacion}&tipo={tipo}&respuesta=aprobar"; string urlRechazar = $"{baseUrl}/Operaciones/Index?handler=RespuestaCertificadoCliente&id={idOperacion}&tipo={tipo}&respuesta=rechazar";
                string tituloAccion = "En caso de aprobar o requerir modificaciones, haga click en los botones correspondientes:";
                string botonesHtml = $@"<a href='{urlAprobar}' style='background-color:#16a34a;color:#fff;padding:14px 28px;text-decoration:none;border-radius:6px;font-weight:bold;margin-right:10px;display:inline-block;'>APROBAR CERTIFICADO</a> <a href='{urlRechazar}' style='background-color:#dc2626;color:#fff;padding:14px 28px;text-decoration:none;border-radius:6px;font-weight:bold;display:inline-block;'>SOLICITAR CAMBIO</a>";

                string html = $@"<div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px;'><div style='background: linear-gradient(135deg, #0f172a 0%, #6d2d9e 100%); padding: 25px; text-align: center; border-radius: 8px 8px 0 0;'><h2 style='color: #fff; margin: 0;'>Revisión de Certificado {nombreCert}{(version > 1 ? $" (V{version})" : "")}</h2><p style='color:#cbd5e1; margin-top:5px;'>Booking: <strong>{op.NumeroBooking}</strong></p></div><div style='padding: 30px; background-color: #ffffff;'><p>Estimado cliente, junto con saludar,</p><p>A continuación enviamos copia del Certificado {nombreCert} para su revisión.</p><div style='text-align: center; margin: 25px 0;'><a href='{urlDescarga}' style='color: #0ea5e9; font-weight: bold; text-decoration: underline;'>Ver Documento Adjunto (PDF/JPG)</a></div><p>{tituloAccion}</p><div style='text-align: center; margin-top: 30px; margin-bottom: 20px;'>{botonesHtml}</div><hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 30px 0;'><p style='font-size: 11px; color: #94a3b8; text-align: center; margin: 0;'>Atilson Cargo SpA.</p></div></div>";

                await _emailService.EnviarCorreoAsync(correoDestino, $"[ATILSON CARGO] Revisión Certificado {nombreCert}{(version > 1 ? $" (V{version})" : "")} - BKG {op.NumeroBooking}", html);

                string logEntry = $"<div style='border-left:2px solid #0ea5e9; padding:6px; margin-bottom:6px; background:#f0f9ff; border-top:1px solid #bae6fd; border-right:1px solid #bae6fd; border-bottom:1px solid #bae6fd;'><div class='d-flex justify-content-between align-items-center mb-1'><span style='font-weight:800; color:#0284c7; text-transform:uppercase; font-size:10px;'>ENVÍO A CLIENTE{(version > 1 ? $" (V{version})" : "")}</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Operaciones Atilson</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>Certificado enviado para revisión a: <strong>{correoDestino}</strong></div></div>\n";
                if (tipo == "san") doc.LogSanitario = logEntry + (doc.LogSanitario ?? ""); else if (tipo == "cap") doc.LogCaptura = logEntry + (doc.LogCaptura ?? "");

                op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex) { return new JsonResult(new { success = false, message = ex.Message }); }
        }

        public async Task<IActionResult> OnGetRespuestaCertificadoClienteAsync(string id, string tipo, string respuesta)
        {
            try
            {
                int realId = int.Parse(id.Replace("sub-", "").Replace("v3-", ""));
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == realId);
                if (op == null) return Content("Operación no encontrada.");

                string nombreCert = tipo == "san" ? "Sanitario" : "de Captura";
                var doc = op.OperacionesDocumentales.FirstOrDefault();

                if (respuesta == "recibido")
                {
                    if (doc != null)
                    {
                        string logEntry = $"<div style='border-left:2px solid #0ea5e9; padding:6px; margin-bottom:6px; background:#f0f9ff; border-top:1px solid #bae6fd; border-right:1px solid #bae6fd; border-bottom:1px solid #bae6fd;'><div class='d-flex justify-content-between align-items-center mb-1'><span style='font-weight:800; color:#0284c7; text-transform:uppercase; font-size:10px;'>RESPUESTA CLIENTE</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Cliente</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>El cliente confirmó la <strong style='color:#0284c7;'>RECEPCIÓN</strong> del Certificado {nombreCert}.</div></div>\n";
                        if (tipo == "san") doc.LogSanitario = logEntry + (doc.LogSanitario ?? ""); else if (tipo == "cap") doc.LogCaptura = logEntry + (doc.LogCaptura ?? "");
                    }
                    op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
                    return Content($@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><title>Recepción Confirmada</title><style>body{{font-family:'Segoe UI',sans-serif;background:#f1f5f9;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;}}.card{{background:#fff;padding:40px;border-radius:12px;box-shadow:0 10px 25px rgba(0,0,0,.1);text-align:center;max-width:450px;border-top:6px solid #0ea5e9;}}h1{{color:#0ea5e9;margin-top:0;}}p{{color:#475569;line-height:1.6;font-size:15px;}}</style></head><body><div class='card'><h1>¡Recepción Confirmada!</h1><p>Hemos registrado la recepción del Certificado {nombreCert}.</p><p style='font-size:13px;color:#94a3b8;margin-top:30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>", "text/html");
                }

                bool aprobado = respuesta == "aprobar";
                if (aprobado)
                {
                    if (doc != null)
                    {
                        string tagResultado = tipo == "san" ? "VISTO BUENO" : "APROBADO";
                        string logEntry = $"<div style='border-left:2px solid #10b981; padding:6px; margin-bottom:6px; background:#f0fdf4; border-top:1px solid #bbf7d0; border-right:1px solid #bbf7d0; border-bottom:1px solid #bbf7d0;'><div class='d-flex justify-content-between align-items-center mb-1'><span style='font-weight:800; color:#16a34a; text-transform:uppercase; font-size:10px;'>RESPUESTA CLIENTE</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Cliente</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>El cliente ha dado <strong style='color:#16a34a;'>{tagResultado}</strong> al Certificado {nombreCert}.</div></div>\n";
                        if (tipo == "san") doc.LogSanitario = logEntry + (doc.LogSanitario ?? ""); else if (tipo == "cap") doc.LogCaptura = logEntry + (doc.LogCaptura ?? "");
                    }
                    op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
                    return Content($@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Certificado Aprobado</title><style>body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f1f5f9; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }} .card {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); text-align: center; max-width: 450px; border-top: 6px solid #16a34a; }} h1 {{ color: #16a34a; margin-top: 0; }} p {{ color: #475569; line-height: 1.6; font-size: 16px; }}</style></head><body><div class='card'><h1>¡Certificado Aprobado!</h1><p>Hemos registrado su aprobación del Certificado {nombreCert} en nuestro sistema.</p><p style='font-size: 13px; color: #94a3b8; margin-top: 30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>", "text/html");
                }
                else
                {
                    var req = HttpContext.Request; string baseUrl = $"{req.Scheme}://{req.Host}"; string formAction = $"{baseUrl}/Operaciones/Index";
                    return Content($@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Solicitar Cambios</title><style>body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f1f5f9; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }} .card {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); text-align: center; max-width: 500px; width: 100%; border-top: 6px solid #dc2626; box-sizing: border-box; }} h1 {{ color: #dc2626; margin-top: 0; font-size: 22px; }} p {{ color: #475569; line-height: 1.5; font-size: 14px; margin-bottom: 25px; }} textarea {{ width: 100%; padding: 15px; border: 1px solid #cbd5e1; border-radius: 8px; font-family: inherit; font-size: 14px; margin-bottom: 20px; box-sizing: border-box; resize: vertical; min-height: 140px; outline: none; transition: border-color 0.2s; }} textarea:focus {{ border-color: #dc2626; box-shadow: 0 0 0 3px rgba(220,38,38,0.1); }} button {{ background-color: #dc2626; color: white; border: none; padding: 14px 24px; border-radius: 8px; font-weight: bold; font-size: 15px; cursor: pointer; transition: background 0.2s; width: 100%; }} button:hover {{ filter: brightness(0.9); }}</style></head><body><div class='card'><h1>Correcciones Certificado {nombreCert}</h1><p>Por favor, indique detalladamente las correcciones que necesita realizar en el documento:</p><form method='GET' action='{formAction}'><input type='hidden' name='handler' value='GuardarCambiosCertificadoCliente' /><input type='hidden' name='id' value='{id}' /><input type='hidden' name='tipo' value='{tipo}' /><textarea name='comentariosCliente' placeholder='Ej: Favor corregir el peso bruto y actualizar la dirección...' required></textarea><button type='submit'>Enviar Correcciones a Operaciones</button></form></div></body></html>", "text/html");
                }
            }
            catch { return Content("Error al procesar solicitud."); }
        }

        public async Task<IActionResult> OnGetGuardarCambiosCertificadoClienteAsync(string id, string tipo, string comentariosCliente)
        {
            try
            {
                int realId = int.Parse(id.Replace("sub-", "").Replace("v3-", ""));
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == realId);
                if (op != null)
                {
                    string nombreCert = tipo == "san" ? "Sanitario" : "de Captura"; var doc = op.OperacionesDocumentales.FirstOrDefault();
                    if (doc != null)
                    {
                        string logEntry = $"<div style='border-left:2px solid #dc2626; padding:6px; margin-bottom:6px; background:#fef2f2; border-top:1px solid #fecaca; border-right:1px solid #fecaca; border-bottom:1px solid #fecaca;'><div class='d-flex justify-content-between align-items-center mb-1'><span style='font-weight:800; color:#dc2626; text-transform:uppercase; font-size:10px;'>RESPUESTA CLIENTE (RECHAZO)</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Cliente</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>El cliente ha <strong style='color:#dc2626;'>RECHAZADO</strong> el Certificado {nombreCert} y solicita los siguientes cambios:<br><em>\"{comentariosCliente}\"</em></div></div>\n";
                        if (tipo == "san") doc.LogSanitario = logEntry + (doc.LogSanitario ?? ""); else if (tipo == "cap") doc.LogCaptura = logEntry + (doc.LogCaptura ?? "");
                    }
                    op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();
                    return Content($@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Cambios Registrados</title><style>body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f1f5f9; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; padding: 20px; }} .card {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); text-align: center; max-width: 450px; border-top: 6px solid #eab308; }} h1 {{ color: #eab308; margin-top: 0; }} p {{ color: #475569; line-height: 1.6; font-size: 15px; }}</style></head><body><div class='card'><h1>¡Correcciones Registradas!</h1><p>Hemos guardado sus comentarios exitosamente. Nuestro equipo ha sido notificado y gestionará la corrección a la brevedad.</p><p style='font-size: 13px; color: #94a3b8; margin-top: 30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>", "text/html");
                }
                return Content("Operación no encontrada.");
            }
            catch { return Content("Error al procesar su solicitud."); }
        }

        public async Task<IActionResult> OnGetRespuestaGuiaAgenciaAsync(int id, string estado)
        {
            try
            {
                var op = await _context.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == id);
                if (op == null) return Content("Operación no encontrada.");
                var doc = op.OperacionesDocumentales.FirstOrDefault();
                string textoEstado = estado == "visado" ? "Guía Visada" : "En Proceso de Visación";
                string color = estado == "visado" ? "#16a34a" : "#0ea5e9"; string colorBg = estado == "visado" ? "#f0fdf4" : "#f0f9ff"; string colorBorder = estado == "visado" ? "#bbf7d0" : "#bae6fd"; string emoji = estado == "visado" ? "✅" : "📋";
                string logEntry = $"<div style='border-left:2px solid {color}; padding:6px; margin-bottom:6px; background:{colorBg}; border-top:1px solid {colorBorder}; border-right:1px solid {colorBorder}; border-bottom:1px solid {colorBorder};'><div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'><span style='font-weight:800; color:{color}; text-transform:uppercase; font-size:10px;'>{emoji} {textoEstado.ToUpper()}</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Agencia de Aduanas</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>La agencia ha confirmado: <strong style='color:{color};'>{textoEstado}</strong></div></div>\n";
                if (doc != null) doc.LogGuia = logEntry + (doc.LogGuia ?? "");
                RegistrarHito(op, "DOCUMENTAL", $"Agencia confirmó estado de Guía de Despacho: {textoEstado}"); op.FechaModificacion = DateTime.Now; await _context.SaveChangesAsync();

                if (estado == "proceso")
                {
                    var req = HttpContext.Request; string baseUrl = $"{req.Scheme}://{req.Host}"; int idOperacion = op.Id; string bookingStr = op.NumeroBooking ?? "S/N";
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1)); // Prueba a 1 minuto
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<AtilsonContext>(); var emailSvc = scope.ServiceProvider.GetRequiredService<EmailService>();
                                var opActual = await db.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == idOperacion);
                                if (opActual != null)
                                {
                                    var docActual = opActual.OperacionesDocumentales.FirstOrDefault(); string logActual = docActual?.LogGuia?.ToUpper() ?? "";
                                    if (!logActual.Contains("GUÍA VISADA") && !logActual.Contains("GUIA VISADA") && !logActual.Contains("✅"))
                                    {
                                        string linkVisado = $"{baseUrl}/Operaciones/Index?handler=RespuestaGuiaAgencia&id={idOperacion}&estado=visado";
                                        string correoDestino = "danielgajardoatil@gmail.com"; // <-- OJO EN PRODUCCIÓN
                                        string htmlRecordatorio = $@"<div style='font-family:Arial,sans-serif;max-width:580px;margin:0 auto;'><div style='background:linear-gradient(135deg,#b91c1c 0%,#7f1d1d 100%);padding:24px 28px;border-radius:8px 8px 0 0;text-align:center;'><h2 style='color:#fff;margin:0;font-size:18px;'>⏰ Recordatorio: Visación Pendiente</h2><p style='color:#fecaca;margin:6px 0 0;font-size:13px;'>Booking: <strong style='color:#fff;'>{bookingStr}</strong></p></div><div style='background:#fff;padding:28px;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 8px 8px;'><p style='font-size:14px;color:#475569;line-height:1.6;'>Estimado equipo de Agencia,</p><p style='font-size:14px;color:#475569;line-height:1.6;'>El sistema detecta que han pasado <strong>más de 30 minutos</strong> desde que se marcó el inicio del proceso de visación para el Booking <strong style='color:#0f172a;'>{bookingStr}</strong>, y aún no se ha confirmado su término.</p><div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:20px;margin:24px 0;text-align:center;'><p style='font-size:13px;font-weight:700;color:#0f172a;text-transform:uppercase;margin:0 0 14px;'>Si la guía ya fue visada, por favor confírmelo aquí:</p><a href='{linkVisado}' style='background:#16a34a;color:#fff;padding:14px 22px;text-decoration:none;border-radius:6px;font-weight:800;font-size:13px;display:inline-block;'>✅ CONFIRMAR GUÍA VISADA</a></div></div></div>";
                                        await emailSvc.EnviarCorreoAsync(correoDestino, $"[ALERTA AUTOMÁTICA] Visación Pendiente — BKG: {bookingStr}", htmlRecordatorio);
                                        string logAuto = $"<div style='border-left:2px solid #f59e0b; padding:6px; margin-bottom:6px; background:#fffbeb; border-top:1px solid #fde68a; border-right:1px solid #fde68a; border-bottom:1px solid #fde68a;'><div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'><span style='font-weight:800; color:#d97706; text-transform:uppercase; font-size:10px;'>⏰ RECORDATORIO ENVIADO</span><span style='color:#64748b; font-size:9px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div><div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>Sistema Automático</strong></div><div style='color:#1e293b; font-size:11px; line-height:1.2;'>Alerta por demora enviada a {correoDestino}</div></div>\n";
                                        if (docActual != null) { docActual.LogGuia = logAuto + docActual.LogGuia; }
                                        await db.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var db = scope.ServiceProvider.GetRequiredService<AtilsonContext>(); var opError = await db.Operaciones.Include(o => o.OperacionesDocumentales).FirstOrDefaultAsync(o => o.Id == idOperacion);
                                if (opError != null) { var docError = opError.OperacionesDocumentales.FirstOrDefault(); if (docError != null) { string logError = $"<div style='border-left:2px solid #dc2626; padding:6px; margin-bottom:6px; background:#fef2f2;'><div style='font-weight:800; color:#dc2626; font-size:10px;'>❌ ERROR EN TEMPORIZADOR</div><div style='font-size:10px; color:#1e293b;'>{ex.Message}</div></div>\n"; docError.LogGuia = logError + docError.LogGuia; await db.SaveChangesAsync(); } }
                            }
                        }
                    });
                }
                return Content($@"<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>{textoEstado} — Atilson Cargo</title><style>body{{font-family:'Segoe UI',sans-serif;background:#f1f5f9;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;}}.card{{background:#fff;padding:40px;border-radius:12px;box-shadow:0 10px 25px rgba(0,0,0,.1);text-align:center;max-width:450px;border-top:6px solid {color};}}h1{{color:{color};margin-top:0;}}p{{color:#475569;line-height:1.6;font-size:15px;}}</style></head><body><div class='card'><h1>{emoji} {textoEstado}</h1><p>Hemos registrado la confirmación en el sistema de Atilson Cargo SpA.<br>Nuestros operadores han sido notificados automáticamente.</p><p style='font-size:13px;color:#94a3b8;margin-top:30px;'>Ya puede cerrar esta pestaña.</p></div></body></html>", "text/html");
            }
            catch (Exception ex) { return Content($"Error al procesar: {ex.Message}"); }
        }

        // ==========================================
        // AUTO-INYECCIÓN DE TRÁMITES ADUANEROS A FINANZAS
        // ==========================================
        // Métodos de cálculo de tarifas automatizadas
        private async Task AplicarTarifaMaritimaAutomaticaAsync(Operacione op) { await Task.CompletedTask; }
        private async Task AplicarTarifaTerrestreAutomaticaAsync(Operacione op, OperacionesTerrestre terrDb) { await Task.CompletedTask; }
        private async Task AplicarTarifaGateAutomaticaAsync(Operacione op, OperacionesTerrestre terrDb) { await Task.CompletedTask; }

        private async Task SincronizarTramitesAduanaAFinanzasAsync(int idOperacion)
        {
            var doc = await _context.OperacionesDocumentales.FirstOrDefaultAsync(d => d.IdOperacion == idOperacion);
            if (doc == null) return;

            if (doc.AplicaSag && doc.AplicaSernapesca)
                doc.AplicaSernapesca = false;

            var txExistentes = await _context.TransaccionesFinancieras
                .Where(t => t.IdOperacion == idOperacion && t.GrupoCobro == "Documental")
                .ToListAsync();

            string usuario = User.Identity?.Name ?? "Sistema";
            DateTime ahora = DateTime.Now;

            void CheckYCrear(bool aplica, string concepto, decimal costoCLP)
            {
                var tx = txExistentes.FirstOrDefault(t => t.Concepto.Equals(concepto, StringComparison.OrdinalIgnoreCase));
                if (aplica && tx == null)
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                    {
                        IdOperacion = idOperacion,
                        GrupoCobro = "Documental",
                        TipoMovimiento = "EGRESO",
                        Concepto = concepto,
                        MontoNeto = costoCLP,
                        Moneda = "CLP",
                        EstadoFila = "PROVISIÓN",
                        FechaCreacion = ahora,
                        UsuarioCreador = usuario
                    });
                }
                else if (!aplica && tx != null && (tx.EstadoFila == "PROVISIÓN" || tx.EstadoFila == "PROVISION"))
                {
                    _context.TransaccionesFinancieras.Remove(tx);
                }
            }

            CheckYCrear(doc.AplicaSag, "Certificado Fitosanitario SAG", 35000m);
            CheckYCrear(doc.AplicaSernapesca, "Certificado Sanitario Sernapesca", 35000m);
            CheckYCrear(doc.CertificadoOrigen == true, "Certificado de Origen", 25000m);
            CheckYCrear(!string.IsNullOrEmpty(doc.ValCoa1.ToString()) && doc.ValCoa1 > 0, "Trámite COA Aduana", 20000m);

            await _context.SaveChangesAsync();
        }
    }
}