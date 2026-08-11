using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;
using System.Text.RegularExpressions;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class CreateModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(AtilsonContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // --- MAGIA ATILSON: LECTOR DE PDF GRATUITO V3 (EXTRACCIÓN COMPLETA) ---
        public async Task<IActionResult> OnPostProcesarPdfAsync(IFormFile archivoPdf)
        {
            if (archivoPdf == null || archivoPdf.Length == 0)
                return new JsonResult(new { success = false, message = "No se recibió ningún archivo." });

            try
            {
                string textoCompleto = "";

                using (var stream = archivoPdf.OpenReadStream())
                using (var pdf = PdfDocument.Open(stream))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        textoCompleto += page.Text + " ";
                    }
                }

                // Limpiar espacios en blanco dobles para facilitar la lectura
                textoCompleto = Regex.Replace(textoCompleto, @"\s+", " ");

                // Variables a extraer
                string booking = ""; string nave = ""; string naviera = "";
                string pol = ""; string pod = ""; string etd = ""; string eta = "";
                string tipoCarga = "1"; string tipoContenedor = ""; string cantidadContenedores = "1";
                string cliente = ""; string commodity = "";

                // 1. Identificar la Naviera
                if (textoCompleto.Contains("Hapag-Lloyd", StringComparison.OrdinalIgnoreCase)) naviera = "Hapag-Lloyd";
                else if (textoCompleto.Contains("CMA CGM", StringComparison.OrdinalIgnoreCase)) naviera = "CMA CGM";
                else if (textoCompleto.Contains("MSC") || textoCompleto.Contains("MEDITERRANEAN SHIPPING", StringComparison.OrdinalIgnoreCase)) naviera = "MSC";
                else if (textoCompleto.Contains("MAERSK", StringComparison.OrdinalIgnoreCase)) naviera = "Maersk";
                else if (textoCompleto.Contains("EVERGREEN", StringComparison.OrdinalIgnoreCase)) naviera = "Evergreen";
                else if (textoCompleto.Contains("OCEAN NETWORK EXPRESS", StringComparison.OrdinalIgnoreCase) || textoCompleto.Contains(" ONE ")) naviera = "ONE";
                else if (textoCompleto.Contains("OOCL", StringComparison.OrdinalIgnoreCase)) naviera = "OOCL";
                else if (textoCompleto.Contains("AGUNSA", StringComparison.OrdinalIgnoreCase)) naviera = "AGUNSA";

                // 2. Número de Booking
                Match matchBooking = Regex.Match(textoCompleto, @"(?:Booking No|Booking Ref|Booking #|Our Reference|NUMERO DE RESERVA|Número de Booking|Booking Number|Turn-In Reference)[\s\.:#]+([A-Za-z0-9]{6,15})", RegexOptions.IgnoreCase);
                if (matchBooking.Success) booking = matchBooking.Groups[1].Value.Trim();

                // 3. Nombre de Nave
                Match matchNave = Regex.Match(textoCompleto, @"(?:Vessel\/Voyage|Nave-Viaje|Vessel|Trunk Vessel|VESSEL AND VOYAGE NUMBER)[\s\.:]+([A-Za-z0-9\s\-]+?)(?=\s+(?:POL|Port|From|Date|Origin|Voyage|Pto|PORT|202|POD|Transhipment))", RegexOptions.IgnoreCase);
                if (matchNave.Success) nave = matchNave.Groups[1].Value.Trim();

                // 4. Extraer Cliente (Shipper / Booked by)
                Match matchCliente = Regex.Match(textoCompleto, @"(?:SHIPPER|Booked by Party:|Booking Party|Attn:|Customer|FROM:)[\s\*:]+([A-Z\s\.\,]{5,40})(?=\s|$|RUT|Contact|Telephone)", RegexOptions.IgnoreCase);
                if (matchCliente.Success) cliente = matchCliente.Groups[1].Value.Trim();

                // Refuerzo para clientes si la naviera lo pone en un formato extraño
                if (string.IsNullOrEmpty(cliente) || cliente.Length < 4)
                {
                    if (textoCompleto.Contains("PROMAR", StringComparison.OrdinalIgnoreCase)) cliente = "PROMAR INVERSIONES";
                    else if (textoCompleto.Contains("ATILSON", StringComparison.OrdinalIgnoreCase)) cliente = "ATILSON";
                }

                // 5. Extraer Mercancía (Commodity)
                Match matchCommodity = Regex.Match(textoCompleto, @"(?:Commodity Description:|Commodity|Description)[\s\*:]+([A-Za-z0-9\s\,\-]{4,40})(?=\s|$|Temp|App|OOG)", RegexOptions.IgnoreCase);
                if (matchCommodity.Success) commodity = matchCommodity.Groups[1].Value.Trim();

                // Refuerzo de commodity basado en tus PDFs de muestra
                if (string.IsNullOrEmpty(commodity))
                {
                    if (textoCompleto.Contains("Kiwifruit", StringComparison.OrdinalIgnoreCase)) commodity = "Kiwifruit";
                    else if (textoCompleto.Contains("Molluscs", StringComparison.OrdinalIgnoreCase)) commodity = "Molluscs";
                    else if (textoCompleto.Contains("Nueces", StringComparison.OrdinalIgnoreCase) || textoCompleto.Contains("Walnuts", StringComparison.OrdinalIgnoreCase)) commodity = "Nueces";
                }

                // 6. Fechas (ETD y ETA) - Parseo a formato HTML5 (YYYY-MM-DD)
                Match matchEtd = Regex.Match(textoCompleto, @"(?:ETD|ETS|Departure|Sailing Date|Cargo Cut Off)[\s\.:]*(\d{1,2}[\-\/\.][A-Za-z]{3,}[\-\/\.]\d{2,4}|\d{1,2}[\-\/\.]\d{1,2}[\-\/\.]\d{2,4})", RegexOptions.IgnoreCase);
                if (matchEtd.Success)
                {
                    string dateStr = matchEtd.Groups[1].Value.Replace(".", "/").Replace("-", "/");
                    if (DateTime.TryParse(dateStr, out DateTime parsedEtd)) etd = parsedEtd.ToString("yyyy-MM-dd");
                }

                Match matchEta = Regex.Match(textoCompleto, @"(?:ETA|Arrival Date|Discharge Date ETA)[\s\.:]*(\d{1,2}[\-\/\.][A-Za-z]{3,}[\-\/\.]\d{2,4}|\d{1,2}[\-\/\.]\d{1,2}[\-\/\.]\d{2,4})", RegexOptions.IgnoreCase);
                if (matchEta.Success)
                {
                    string dateStr = matchEta.Groups[1].Value.Replace(".", "/").Replace("-", "/");
                    if (DateTime.TryParse(dateStr, out DateTime parsedEta)) eta = parsedEta.ToString("yyyy-MM-dd");
                }

                // 7. Puertos (POL y POD)
                Match matchPol = Regex.Match(textoCompleto, @"(?:Port of Loading|POL|Origin|From|Sailing|Pto\. Embarque)[\s\*:]+([A-Za-z\s]{4,20})(?=\s|,|-|CHILE|PERU)", RegexOptions.IgnoreCase);
                if (matchPol.Success) pol = matchPol.Groups[1].Value.Replace("CHILE", "").Trim();

                Match matchPod = Regex.Match(textoCompleto, @"(?:Port of Discharge|POD|Destination|To|Discharge)[\s\*:]+([A-Za-z\s]{4,20})(?=\s|,|-|SPAIN|MOROCCO|HONG KONG|INDIA)", RegexOptions.IgnoreCase);
                if (matchPod.Success) pod = matchPod.Groups[1].Value.Trim();

                // 8. Contenedores y Carga (Reefer vs Dry)
                if (textoCompleto.Contains("REEFER", StringComparison.OrdinalIgnoreCase) || textoCompleto.Contains(" RQ") || textoCompleto.Contains(" RF") || textoCompleto.Contains("Refrigerated"))
                {
                    tipoCarga = "2"; // REEFER
                    if (textoCompleto.Contains("40' HIGH CUBE", StringComparison.OrdinalIgnoreCase) || textoCompleto.Contains("40' Reefer", StringComparison.OrdinalIgnoreCase) || textoCompleto.Contains("40RQ") || textoCompleto.Contains("40 RH") || textoCompleto.Contains("40' Hi-Cube")) tipoContenedor = "40' REEFER";
                    else tipoContenedor = "20' REEFER";
                }
                else
                {
                    tipoCarga = "1"; // DRY
                    if (textoCompleto.Contains("40' HIGH CUBE", StringComparison.OrdinalIgnoreCase) || textoCompleto.Contains("40HC") || textoCompleto.Contains("40' HC")) tipoContenedor = "40' DRY HC";
                    else if (textoCompleto.Contains("40' DRY") || textoCompleto.Contains("40DV") || textoCompleto.Contains("40' DV")) tipoContenedor = "40' DRY STD";
                    else tipoContenedor = "20' DRY STD";
                }

                // Cantidad (Ej: "1 x 40", "2x20")
                Match matchCant = Regex.Match(textoCompleto, @"(\d+)\s*[xX]\s*(?:20|40|45)");
                if (matchCant.Success) cantidadContenedores = matchCant.Groups[1].Value;

                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        booking = booking,
                        naviera = naviera,
                        nave = nave,
                        cliente = cliente,
                        commodity = commodity,
                        etd = etd,
                        eta = eta,
                        pol = pol,
                        pod = pod,
                        tipoCarga = tipoCarga,
                        tipoContenedor = tipoContenedor,
                        cantidadContenedores = cantidadContenedores
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error leyendo el PDF: " + ex.Message });
            }
        }


        // --- MAGIA ATILSON: Carga asíncrona de datos maestros ---
        private async Task CargarSelectListsAsync()
        {
            ViewData["IdCliente"] = new SelectList(await _context.Clientes.Where(c => c.Activo == 1).OrderBy(c => c.RazonSocial).ToListAsync(), "Id", "RazonSocial");
            ViewData["IdNaviera"] = new SelectList(await _context.Navieras.Where(n => n.Activo == 1).OrderBy(n => n.NombreNaviera).ToListAsync(), "Id", "NombreNaviera");

            var tipoMovimientos = new List<object>
            {
                new { Id = 1, Valor = "CY/CY (Contenedor Completo / FCL)" },
                new { Id = 2, Valor = "CY/CFS (Consolidado)" },
                new { Id = 3, Valor = "CFS/CY (Desconsolidado)" },
                new { Id = 4, Valor = "CFS/CFS (LCL)" },
                new { Id = 5, Valor = "FO/FO (Free Out)" },
                new { Id = 6, Valor = "FI/FO (Free In / Free Out)" },
                new { Id = 7, Valor = "SD/SD (Store Door)" }
            };
            ViewData["IdTipoMovimiento"] = new SelectList(tipoMovimientos, "Id", "Valor");

            // Listas completas para autocompletado en el frontend
            ViewData["PlantasList"] = await _context.Plantas.Include(p => p.Ciudad).Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            ViewData["PuertosList"] = await _context.Puertos.Where(p => p.Activo == 1).OrderBy(p => p.NombrePuerto).ToListAsync();
            ViewData["DepositosList"] = await _context.Depositos.Where(d => d.Activo == 1).OrderBy(d => d.NombreDeposito).ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await CargarSelectListsAsync();
            return Page();
        }

        [BindProperty]
        public Operacione Operacione { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync(
            IFormFile? ArchivoInstructivoTerrestre,
            IFormFile? DocInstructivo,
            IFormFile? DocMatriz,
            IFormFile? DocBookingCliente,
            IFormFile? EvidenciaInstructivo)
        {
            ModelState.Clear();

            // --- VALIDAR BOOKING DUPLICADO ---
            bool bookingExiste = await _context.Operaciones
                .AnyAsync(o => o.NumeroBooking == Operacione.NumeroBooking);

            if (bookingExiste)
            {
                TempData["ErrorBooking"] = $"El número de Booking/Referencia '{Operacione.NumeroBooking}' ya se encuentra registrado. Verifique el dato.";
                await CargarSelectListsAsync();
                return Page();
            }

            string nombreUsuario = User.Identity?.Name ?? "Sistema";

            var serv = Operacione.IdTipoServicio ?? 0;
            bool esMaritimo = new[] { 1, 2, 3, 5, 8, 9, 10, 12 }.Contains(serv);
            bool esTerrestre = new[] { 1, 2, 4, 6, 8, 9, 11, 13 }.Contains(serv);
            bool esDocumental = new[] { 1, 3, 4, 7, 8, 10, 11, 14 }.Contains(serv);
            bool esSoloTerrestre = esTerrestre && !esMaritimo;

            // --- NORMALIZAR CAMPOS OBLIGATORIOS ---
            if (!esMaritimo)
            {
                Operacione.IdNaviera = _context.Navieras.FirstOrDefault()?.Id ?? 1;
                Operacione.IdTipoCarga = 1;
                Operacione.IdPuertoOrigen = null;
                Operacione.IdPuertoDestino = null;
            }
            else
            {
                if (Operacione.IdNaviera == 0)
                    Operacione.IdNaviera = _context.Navieras.FirstOrDefault()?.Id ?? 1;
                if (Operacione.IdTipoCarga == 0)
                    Operacione.IdTipoCarga = 1;
            }

            if (Operacione.IdCliente == 0)
                Operacione.IdCliente = _context.Clientes.FirstOrDefault()?.Id ?? 1;

            // --- CAMPOS AUDITORÍA Y CONTROL ---
            Operacione.FechaCreacion = DateTime.Now;
            Operacione.UsuarioCreador = nombreUsuario;
            Operacione.Activo = 1;
            Operacione.IsDeleted = false;
            Operacione.ContenedorIngresado = false;
            Operacione.EstadoLar = "No Solicitado";
            Operacione.EvidenciaLar = null;

            // Terminal portuario dinámico (Marítimo o Terrestre)
            string? termMaritimo = Request.Form["TerminalMaritimoDDL"];
            if (string.IsNullOrWhiteSpace(termMaritimo)) termMaritimo = Request.Form["TerminalMaritimoStr"];

            string? termTerrestre = Request.Form["TerminalTerrestreDDL"];
            if (string.IsNullOrWhiteSpace(termTerrestre)) termTerrestre = Request.Form["TerminalTerrestreStr"];

            Operacione.TerminalPortuario = esMaritimo ? termMaritimo : termTerrestre;

            // --- CORRELATIVO ATL-YYYYMM-XXX ---
            int anioActual = DateTime.Now.Year;
            int mesActual = DateTime.Now.Month;
            int conteoMes = await _context.Operaciones.CountAsync(o =>
                o.FechaCreacion != null &&
                o.FechaCreacion.Value.Year == anioActual &&
                o.FechaCreacion.Value.Month == mesActual) + 1;
            Operacione.CodigoAtilson = $"ATL-{anioActual}{mesActual:D2}-{conteoMes:D3}";
            Operacione.EstadoWorkflow = "EN PROCESO";

            if (string.IsNullOrWhiteSpace(Operacione.NumeroContenedor))
                Operacione.NumeroContenedor = null;

            // --- PARÁMETROS REEFER ---
            if (!esSoloTerrestre)
            {
                Operacione.CondicionReefer = Request.Form["CondicionReefer"];
                Operacione.MarcaAc = Request.Form["MarcaAc"];
                Operacione.Atmosfera = Request.Form["TipoAtmosfera"];
                if (double.TryParse(Request.Form["TempSeteada"], out double temp)) Operacione.Temperatura = temp;
                if (double.TryParse(Request.Form["Ventilacion"], out double vent)) Operacione.Ventilacion = vent;
                if (double.TryParse(Request.Form["Humedad"], out double hum)) Operacione.Humedad = hum;
                if (double.TryParse(Request.Form["NivelO2"], out double o2)) Operacione.O2 = o2;
                if (double.TryParse(Request.Form["NivelCO2"], out double co2)) Operacione.Co2 = co2;
            }
            else
            {
                Operacione.CondicionReefer = null;
                Operacione.MarcaAc = null;
                Operacione.Atmosfera = null;
                Operacione.Temperatura = null;
                Operacione.Ventilacion = null;
                Operacione.Humedad = null;
                Operacione.O2 = null;
                Operacione.Co2 = null;
            }

            Operacione.OperacionesTerrestres ??= new List<OperacionesTerrestre>();
            Operacione.OperacionesDocumentales ??= new List<OperacionesDocumentale>();
            Operacione.Unidadestecnicas ??= new List<Unidadestecnica>();

            // --- SERVICIO TERRESTRE ---
            if (esTerrestre)
            {
                string? rutaInstructivoTerrestre = null;
                if (EvidenciaInstructivo != null && EvidenciaInstructivo.Length > 0)
                {
                    string carpeta = Path.Combine(_env.WebRootPath, "uploads", "instructivos");
                    if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
                    string uniqueName = $"TerrestreInstructivo_{DateTime.Now.Ticks}_{Path.GetFileName(EvidenciaInstructivo.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(carpeta, uniqueName), FileMode.Create))
                        await EvidenciaInstructivo.CopyToAsync(stream);
                    rutaInstructivoTerrestre = $"/uploads/instructivos/{uniqueName}";
                }

                DateTime? fechaCarga = DateTime.TryParse(Request.Form["FechaCarga"], out DateTime dtCarga) ? dtCarga : null;

                Operacione.OperacionesTerrestres.Add(new OperacionesTerrestre
                {
                    EmpresaTransporte = Request.Form["EmpresaTransporte"],
                    RutTransporte = Request.Form["RutTransporte"],
                    PuertoEntrega = Request.Form["PuertoEntrega"],
                    TerminalTerrestreStr = termTerrestre,
                    DepositoRetiro = Request.Form["DepositoRetiro"],
                    PlantaCarga = Request.Form["PlantaCarga"],
                    ZonaCarga = Request.Form["ZonaCarga"],
                    FechaCarga = fechaCarga,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreador = nombreUsuario,
                    Activo = true,
                    SorteoEscaner = false
                });
            }

            // --- SERVICIO DOCUMENTAL ---
            if (esDocumental)
            {
                string carpeta = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

                string? rInstructivo = null;
                if (DocInstructivo != null && DocInstructivo.Length > 0)
                {
                    string u = $"DocInst_{DateTime.Now.Ticks}_{Path.GetFileName(DocInstructivo.FileName).Replace(" ", "_")}";
                    using (var s = new FileStream(Path.Combine(carpeta, u), FileMode.Create)) { await DocInstructivo.CopyToAsync(s); }
                    rInstructivo = $"/uploads/evidencias/{u}";
                }

                string? rMatriz = null;
                if (DocMatriz != null && DocMatriz.Length > 0)
                {
                    string u = $"DocMatriz_{DateTime.Now.Ticks}_{Path.GetFileName(DocMatriz.FileName).Replace(" ", "_")}";
                    using (var s = new FileStream(Path.Combine(carpeta, u), FileMode.Create)) { await DocMatriz.CopyToAsync(s); }
                    rMatriz = $"/uploads/evidencias/{u}";
                }

                string? rBooking = null;
                if (DocBookingCliente != null && DocBookingCliente.Length > 0)
                {
                    string u = $"DocBkgCli_{DateTime.Now.Ticks}_{Path.GetFileName(DocBookingCliente.FileName).Replace(" ", "_")}";
                    using (var s = new FileStream(Path.Combine(carpeta, u), FileMode.Create)) { await DocBookingCliente.CopyToAsync(s); }
                    rBooking = $"/uploads/evidencias/{u}";
                }

                Operacione.OperacionesDocumentales.Add(new OperacionesDocumentale
                {
                    AgenciaAduana = Request.Form["DocAgencia"],
                    DusDin = Request.Form["DocDus"],
                    GuiaVisado = Request.Form.TryGetValue("GuiaVisado", out var gv) && gv == "true",
                    EvidenciaMatriz = rMatriz,
                    MatrizPresentada = rMatriz != null,
                    Mandato = rInstructivo,
                    DocBkgCli = rBooking,
                    EstadoDocumental = "PENDIENTE",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreador = nombreUsuario,
                    Activo = true,
                    ExtensionDocumental = false
                });
            }

            // --- UNIDADES TÉCNICAS ADICIONALES ---
            for (int i = 2; i <= 20; i++)
            {
                var tipoCargaKey = $"ContenedoresExtra[{i}].IdTipoCarga";
                if (!Request.Form.ContainsKey(tipoCargaKey) || string.IsNullOrWhiteSpace(Request.Form[tipoCargaKey]))
                    continue;

                var nuevaUnidad = new Unidadestecnica
                {
                    IdTipoCarga = int.TryParse(Request.Form[tipoCargaKey], out int tc) ? tc : 1,
                    TipoContenedor = Request.Form[$"ContenedoresExtra[{i}].TipoContenedor"],
                    Commodity = Request.Form[$"ContenedoresExtra[{i}].Commodity"],
                    FechaCreacion = DateTime.Now,
                    UsuarioCreador = nombreUsuario
                };

                if (!esSoloTerrestre)
                {
                    nuevaUnidad.CondicionReefer = Request.Form[$"ContenedoresExtra[{i}].CondicionReefer"];
                    nuevaUnidad.TipoAtmosfera = Request.Form[$"ContenedoresExtra[{i}].TipoAtmosfera"];
                    nuevaUnidad.MarcaAc = Request.Form[$"ContenedoresExtra[{i}].MarcaAc"];
                    if (decimal.TryParse(Request.Form[$"ContenedoresExtra[{i}].TempSeteada"], out decimal tempExt)) nuevaUnidad.Temperatura = tempExt;
                    if (int.TryParse(Request.Form[$"ContenedoresExtra[{i}].Humedad"], out int humExt)) nuevaUnidad.Humedad = humExt;
                    if (int.TryParse(Request.Form[$"ContenedoresExtra[{i}].Ventilacion"], out int ventExt)) nuevaUnidad.Ventilacion = ventExt;
                    if (decimal.TryParse(Request.Form[$"ContenedoresExtra[{i}].NivelO2"], out decimal o2Ext)) nuevaUnidad.NivelO2 = o2Ext;
                    if (decimal.TryParse(Request.Form[$"ContenedoresExtra[{i}].NivelCO2"], out decimal co2Ext)) nuevaUnidad.NivelCo2 = co2Ext;
                }

                Operacione.Unidadestecnicas.Add(nuevaUnidad);
            }

            _context.Operaciones.Add(Operacione);
            await _context.SaveChangesAsync();

            TempData["SuccessMsg"] = $"La operación {Operacione.NumeroBooking} fue creada exitosamente.";

            bool esAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            bool generarPdf = Request.Form["GenerarPdfDirecto"] == "true";

            if (esAjax && generarPdf)
            {
                string pdfUrl = Url.Page("/Operaciones/ReservaPdf", new { id = Operacione.Id });
                return new JsonResult(new { success = true, id = Operacione.Id, pdfUrl });
            }

            if (generarPdf)
            {
                TempData["OpenPdfId"] = Operacione.Id;
            }

            return RedirectToPage("./Index");
        }
    }
}