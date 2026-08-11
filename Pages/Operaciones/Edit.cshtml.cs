using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class EditModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(AtilsonContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Operacione Operacione { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Operacione = await _context.Operaciones
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.Unidadestecnicas)
                .Include(o => o.ExtracostosOperacions) // Incluimos extra costos
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Operacione == null) return NotFound();

            ViewData["IdCliente"] = new SelectList(_context.Clientes.Where(c => c.Activo == 1), "Id", "RazonSocial");
            ViewData["IdNaviera"] = new SelectList(_context.Navieras.Where(n => n.Activo == 1), "Id", "NombreNaviera");
            ViewData["IdPuertoOrigen"] = new SelectList(_context.Puertos.Where(p => p.Activo == 1), "Id", "NombrePuerto");
            ViewData["IdPuertoDestino"] = new SelectList(_context.Puertos.Where(p => p.Activo == 1), "Id", "NombrePuerto");
            ViewData["IdTipoMovimiento"] = new SelectList(_context.Subparametros.Where(p => p.Parametro.Categoria == "TipoMovimiento"), "Id", "Valor");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();
            string nombreUsuario = User.Identity?.Name ?? "Sistema";

            var serv = Operacione.IdTipoServicio ?? 0;
            bool esMaritimo = serv == 1 || serv == 2 || serv == 3 || serv == 5 || serv == 8 || serv == 9 || serv == 10 || serv == 12;

            if (!esMaritimo)
            {
                Operacione.IdNaviera = _context.Navieras.FirstOrDefault()?.Id ?? 1;
                Operacione.IdTipoCarga = 1;
                Operacione.IdPuertoOrigen = null;
                Operacione.IdPuertoDestino = null;
            }
            else
            {
                if (Operacione.IdNaviera == 0) Operacione.IdNaviera = _context.Navieras.FirstOrDefault()?.Id ?? 1;
                if (Operacione.IdTipoCarga == 0) Operacione.IdTipoCarga = 1;
            }

            if (Operacione.IdCliente == 0) Operacione.IdCliente = _context.Clientes.FirstOrDefault()?.Id ?? 1;

            var opDb = await _context.Operaciones
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.Unidadestecnicas)
                .Include(o => o.ExtracostosOperacions)
                .FirstOrDefaultAsync(m => m.Id == Operacione.Id);

            if (opDb != null)
            {
                // ========================================================================
                // 1. CAPTURAR ESTADOS ANTERIORES (Para detectar las señales de Finanzas)
                // ========================================================================
                string estadoLarAnterior = opDb.EstadoLar;
                bool correctorAnterior = opDb.OperacionesDocumentales.FirstOrDefault()?.ExtensionDocumental == true;


                // ========================================================================
                // 2. ACTUALIZACIÓN REGULAR DE CAMPOS
                // ========================================================================
                opDb.NumeroBooking = Operacione.NumeroBooking;
                opDb.IdCliente = Operacione.IdCliente;
                opDb.IdTipoServicio = Operacione.IdTipoServicio;
                opDb.CondicionPago = Operacione.CondicionPago;
                opDb.IdTipoMovimiento = Operacione.IdTipoMovimiento;
                opDb.EstadoWorkflow = Operacione.EstadoWorkflow;
                opDb.IdNaviera = Operacione.IdNaviera;
                opDb.Nave = Operacione.Nave;
                opDb.Transbordo = Operacione.Transbordo;
                opDb.TerminalPortuario = Operacione.TerminalPortuario;
                opDb.IdPuertoOrigen = Operacione.IdPuertoOrigen;
                opDb.IdPuertoDestino = Operacione.IdPuertoDestino;
                opDb.EtdPol = Operacione.EtdPol;
                opDb.EtaPod = Operacione.EtaPod;
                opDb.FechaStacking = Operacione.FechaStacking;
                opDb.CutOffMatriz = Operacione.CutOffMatriz;

                opDb.EstadoLar = Request.Form.TryGetValue("EstadoLar", out var elar) && !string.IsNullOrWhiteSpace(elar) ? elar.ToString() : null;
                if (opDb.EstadoLar != null && (opDb.EstadoLar.Contains("AUTORIZADO") || opDb.EstadoLar.Contains("INGRESADO")))
                {
                    if (Request.Form.TryGetValue("LateArrival", out var lat) && DateTime.TryParse(lat, out DateTime ldt)) opDb.LateArrival = ldt;
                }
                else
                {
                    opDb.LateArrival = null;
                }
                opDb.ContenedorIngresado = Request.Form.TryGetValue("ContenedorIngresado", out var cIng) && cIng == "true";

                opDb.NumeroContenedor = string.IsNullOrWhiteSpace(Operacione.NumeroContenedor) ? null : Operacione.NumeroContenedor;
                opDb.SelloNaviera = string.IsNullOrWhiteSpace(Operacione.SelloNaviera) ? null : Operacione.SelloNaviera;
                opDb.IdTipoCarga = Operacione.IdTipoCarga;
                opDb.TipoContenedor = Operacione.TipoContenedor;
                opDb.Commodity = Operacione.Commodity;

                opDb.CondicionReefer = Request.Form.TryGetValue("CondicionReefer", out var cr) ? cr.ToString() : null;
                opDb.MarcaAc = Request.Form.TryGetValue("MarcaAc", out var ma) ? ma.ToString() : null;
                opDb.Atmosfera = Request.Form.TryGetValue("TipoAtmosfera", out var ta) ? ta.ToString() : null;

                if (Request.Form.TryGetValue("TempSeteada", out var ts) && double.TryParse(ts, out double t)) opDb.Temperatura = t; else opDb.Temperatura = null;
                if (Request.Form.TryGetValue("Ventilacion", out var v) && double.TryParse(v, out double ve)) opDb.Ventilacion = ve; else opDb.Ventilacion = null;
                if (Request.Form.TryGetValue("Humedad", out var h) && double.TryParse(h, out double hu)) opDb.Humedad = hu; else opDb.Humedad = null;
                if (Request.Form.TryGetValue("NivelO2", out var o2) && double.TryParse(o2, out double o2v)) opDb.O2 = o2v; else opDb.O2 = null;
                if (Request.Form.TryGetValue("NivelCO2", out var co2) && double.TryParse(co2, out double co2v)) opDb.Co2 = co2v; else opDb.Co2 = null;

                opDb.FechaModificacion = DateTime.Now;
                opDb.UsuarioModificador = nombreUsuario;

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePrincipal = Request.Form.Files.GetFile("EvidenciaPrincipal");
                if (filePrincipal != null && filePrincipal.Length > 0)
                {
                    string uniqueName = $"{opDb.Id}_Main_{DateTime.Now.Ticks}_{Path.GetFileName(filePrincipal.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uniqueName), FileMode.Create)) { await filePrincipal.CopyToAsync(stream); }
                    opDb.EvidenciaContenedor = $"/uploads/evidencias/{uniqueName}";
                }

                var fileLar = Request.Form.Files.GetFile("EvidenciaLar");
                if (fileLar != null && fileLar.Length > 0)
                {
                    string uniqueLar = $"{opDb.Id}_LAR_{DateTime.Now.Ticks}_{Path.GetFileName(fileLar.FileName).Replace(" ", "_")}";
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, uniqueLar), FileMode.Create)) { await fileLar.CopyToAsync(stream); }
                    opDb.EvidenciaLar = $"/uploads/evidencias/{uniqueLar}";
                }

                int[] serviciosTerrestres = { 1, 2, 4, 6, 8, 9, 11, 13 };
                var terrDb = opDb.OperacionesTerrestres.FirstOrDefault();
                if (serviciosTerrestres.Contains(serv))
                {
                    if (terrDb == null) { terrDb = new OperacionesTerrestre { FechaCreacion = DateTime.Now, UsuarioCreador = nombreUsuario, Activo = true }; opDb.OperacionesTerrestres.Add(terrDb); }

                    terrDb.EmpresaTransporte = Request.Form.TryGetValue("TransEmpresa", out var te) ? te.ToString() : null;
                    terrDb.RutTransporte = Request.Form.TryGetValue("TransRut", out var tr) ? tr.ToString() : null;
                    terrDb.CorreoTransporte = Request.Form.TryGetValue("TransCorreo", out var tc) ? tc.ToString() : null;
                    terrDb.NombreConductor = Request.Form.TryGetValue("TransConductor", out var nc) ? nc.ToString() : null;
                    terrDb.TelefonoConductor = Request.Form.TryGetValue("TransTelefono", out var tt) ? tt.ToString() : null;
                    terrDb.Patente = Request.Form.TryGetValue("TransPatente", out var tp) ? tp.ToString() : null;
                    terrDb.TipoUnidadTransporte = Request.Form.TryGetValue("TransTipoUnidad", out var tu) ? tu.ToString() : null;
                    terrDb.DepositoRetiro = Request.Form.TryGetValue("TransDeposito", out var dr) ? dr.ToString() : null;
                    terrDb.PlantaCarga = Request.Form.TryGetValue("TransPlanta", out var pc) ? pc.ToString() : null;
                    terrDb.ZonaEmbarque = Request.Form.TryGetValue("TransZonaEmbarque", out var ze) ? ze.ToString() : null;
                    terrDb.LinkTracking = Request.Form.TryGetValue("TransTracking", out var tk) ? tk.ToString() : null;

                    if (Request.Form.TryGetValue("TransFechaCarga", out var tfc) && DateTime.TryParse(tfc, out DateTime dt)) terrDb.FechaCarga = dt; else terrDb.FechaCarga = null;

                    if (Request.Form.TryGetValue("TransLlegadaPlanta", out var llP) && DateTime.TryParse(llP, out DateTime dllP)) terrDb.LlegadaPlanta = dllP; else terrDb.LlegadaPlanta = null;
                    if (Request.Form.TryGetValue("TransSalidaPlanta", out var sP) && DateTime.TryParse(sP, out DateTime dsP)) terrDb.SalidaPlanta = dsP; else terrDb.SalidaPlanta = null;
                    if (Request.Form.TryGetValue("TransLlegadaPuerto", out var llPu) && DateTime.TryParse(llPu, out DateTime dllPu)) terrDb.LlegadaPuerto = dllPu; else terrDb.LlegadaPuerto = null;
                    if (Request.Form.TryGetValue("TransSalidaPuerto", out var sPu) && DateTime.TryParse(sPu, out DateTime dsPu)) terrDb.SalidaPuerto = dsPu; else terrDb.SalidaPuerto = null;

                    terrDb.SorteoEscaner = Request.Form.TryGetValue("SorteoEscaner", out var scan) && scan == "true";
                }
                else if (terrDb != null) { _context.OperacionesTerrestres.Remove(terrDb); }

                int[] serviciosDocumentales = { 1, 3, 4, 7, 8, 10, 11, 14 };
                var docDb = opDb.OperacionesDocumentales.FirstOrDefault();
                if (serviciosDocumentales.Contains(serv))
                {
                    if (docDb == null) { docDb = new OperacionesDocumentale { FechaCreacion = DateTime.Now, UsuarioCreador = nombreUsuario, Activo = true }; opDb.OperacionesDocumentales.Add(docDb); }

                    docDb.AgenciaAduana = Request.Form.TryGetValue("DocAgencia", out var da) ? da.ToString() : null;
                    docDb.DusDin = Request.Form.TryGetValue("DocDus", out var dd) ? dd.ToString() : null;
                    docDb.EstadoDocumental = Request.Form.TryGetValue("DocEstado", out var de) ? de.ToString() : null;

                    docDb.MatrizPresentada = Request.Form.TryGetValue("MatrizPresentada", out var mp) && mp == "true";
                    docDb.ExtensionDocumental = Request.Form.TryGetValue("ExtensionDocumental", out var extd) && extd == "true";
                    docDb.GuiaVisado = Request.Form.TryGetValue("GuiaVisado", out var gv) && gv == "true";

                    var fileMatriz = Request.Form.Files.GetFile("EvidenciaMatriz");
                    if (fileMatriz != null && fileMatriz.Length > 0)
                    {
                        string uNameMatriz = $"{opDb.Id}_Matriz_{DateTime.Now.Ticks}_{Path.GetFileName(fileMatriz.FileName).Replace(" ", "_")}";
                        using (var stream = new FileStream(Path.Combine(uploadsFolder, uNameMatriz), FileMode.Create)) { await fileMatriz.CopyToAsync(stream); }
                        docDb.EvidenciaMatriz = $"/uploads/evidencias/{uNameMatriz}";
                    }
                }
                else if (docDb != null) { _context.OperacionesDocumentales.Remove(docDb); }

                if (opDb.Unidadestecnicas.Any())
                {
                    _context.Unidadestecnicas.RemoveRange(opDb.Unidadestecnicas);
                    await _context.SaveChangesAsync();
                }

                for (int i = 2; i <= 20; i++)
                {
                    if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].IdTipoCarga", out var tcVal) && !string.IsNullOrWhiteSpace(tcVal))
                    {
                        var nuevaU = new Unidadestecnica
                        {
                            IdTipoCarga = int.TryParse(tcVal, out int tci) ? tci : 1,
                            TipoContenedor = Request.Form.TryGetValue($"ContenedoresExtra[{i}].TipoContenedor", out var tcon) ? tcon.ToString() : null,
                            Commodity = Request.Form.TryGetValue($"ContenedoresExtra[{i}].Commodity", out var comm) ? comm.ToString() : null,
                            CondicionReefer = Request.Form.TryGetValue($"ContenedoresExtra[{i}].CondicionReefer", out var crExt) ? crExt.ToString() : null,
                            TipoAtmosfera = Request.Form.TryGetValue($"ContenedoresExtra[{i}].TipoAtmosfera", out var taExt) ? taExt.ToString() : null,
                            MarcaAc = Request.Form.TryGetValue($"ContenedoresExtra[{i}].MarcaAc", out var maExt) ? maExt.ToString() : null,
                            NroContenedor = Request.Form.TryGetValue($"ContenedoresExtra[{i}].NumeroContenedor", out var numExt) && !string.IsNullOrWhiteSpace(numExt) ? numExt.ToString() : null,
                            SelloNaviera = Request.Form.TryGetValue($"ContenedoresExtra[{i}].SelloNaviera", out var selExt) && !string.IsNullOrWhiteSpace(selExt) ? selExt.ToString() : null,
                            FechaCreacion = DateTime.Now,
                            UsuarioCreador = nombreUsuario
                        };

                        if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].TempSeteada", out var tExt) && decimal.TryParse(tExt, out decimal tempExt)) nuevaU.Temperatura = tempExt;
                        if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].Humedad", out var hExt) && int.TryParse(hExt, out int humExt)) nuevaU.Humedad = humExt;
                        if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].Ventilacion", out var vExt) && int.TryParse(vExt, out int ventExt)) nuevaU.Ventilacion = ventExt;
                        if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].NivelO2", out var o2ExtVal) && decimal.TryParse(o2ExtVal, out decimal o2Ext)) nuevaU.NivelO2 = o2Ext;
                        if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].NivelCO2", out var co2ExtVal) && decimal.TryParse(co2ExtVal, out decimal co2Ext)) nuevaU.NivelCo2 = co2Ext;

                        var fileExtra = Request.Form.Files.GetFile($"EvidenciaExtra_{i}");
                        if (fileExtra != null && fileExtra.Length > 0)
                        {
                            string uNameEx = $"{opDb.Id}_Ex{i}_{DateTime.Now.Ticks}_{Path.GetFileName(fileExtra.FileName).Replace(" ", "_")}";
                            using (var s = new FileStream(Path.Combine(uploadsFolder, uNameEx), FileMode.Create)) { await fileExtra.CopyToAsync(s); }
                            nuevaU.EvidenciaContenedor = $"/uploads/evidencias/{uNameEx}";
                        }
                        else
                        {
                            if (Request.Form.TryGetValue($"ContenedoresExtra[{i}].EvidenciaAntigua", out var evAntigua) && !string.IsNullOrWhiteSpace(evAntigua))
                            {
                                nuevaU.EvidenciaContenedor = evAntigua.ToString();
                            }
                        }
                        opDb.Unidadestecnicas.Add(nuevaU);
                    }
                }

                // ========================================================================
                // 3. GATILLADORES AUTOMÁTICOS PARA FINANZAS (REGLA DE CRISTIAN)
                // ========================================================================

                // SEÑAL A: Detección de Ingreso con LAR / ELAR
                if (opDb.EstadoLar != estadoLarAnterior && opDb.EstadoLar != null && opDb.EstadoLar.Contains("INGRESADO"))
                {
                    var extraCostoLar = new ExtracostosOperacion
                    {
                        TipoCosto = opDb.EstadoLar,
                        Motivo = "Gatillado automáticamente por confirmación de Ingreso en Panel Operativo.",
                        Monto = 0, // Finanzas debe valorizarlo
                        Moneda = "USD",
                        Evidencia = opDb.EvidenciaLar ?? "Evidencia pendiente",
                        FechaCreacion = DateTime.Now,
                        UsuarioCreador = nombreUsuario
                    };
                    opDb.ExtracostosOperacions.Add(extraCostoLar);
                }

                // SEÑAL B: Detección de Corrector de BL (Extensión Documental)
                bool correctorActual = opDb.OperacionesDocumentales.FirstOrDefault()?.ExtensionDocumental == true;
                if (correctorActual == true && correctorAnterior == false)
                {
                    var extraCostoCorrector = new ExtracostosOperacion
                    {
                        TipoCosto = "Corrección de BL / Extensión Documental",
                        Motivo = "Gatillado automáticamente por solicitud de Corrector/Extensión Documental.",
                        Monto = 0, // Finanzas debe valorizarlo
                        Moneda = "USD",
                        Evidencia = "Evidencia pendiente por parte de Operaciones",
                        FechaCreacion = DateTime.Now,
                        UsuarioCreador = nombreUsuario
                    };
                    opDb.ExtracostosOperacions.Add(extraCostoCorrector);
                }

                // ========================================================================
                // 4. DISPARO DEL MOTOR COMERCIAL ATILSON (INYECCIÓN DE PROVISIONES)
                // ========================================================================
                await InyectarProvisionesComercialesAsync(opDb, nombreUsuario);

                // Finalmente guardamos todo
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }

        // ====================================================================
        // FASE 4: REPORTAR EXTRA COSTO MANUAL
        // ====================================================================
        public async Task<IActionResult> OnPostAddCostoAsync(int id)
        {
            string nombreUsuario = User.Identity?.Name ?? "Sistema";

            var tipo = Request.Form["NuevoCosto.TipoCosto"];
            var motivo = Request.Form["NuevoCosto.Motivo"];
            var file = Request.Form.Files.GetFile("NuevoCosto.Evidencia");

            if (!string.IsNullOrEmpty(tipo) && file != null && file.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidencias");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uName = $"{id}_Costo_{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName).Replace(" ", "_")}";
                using (var s = new FileStream(Path.Combine(uploadsFolder, uName), FileMode.Create)) { await file.CopyToAsync(s); }

                var costo = new ExtracostosOperacion
                {
                    IdOperacion = id,
                    TipoCosto = tipo,
                    Motivo = motivo,
                    Moneda = "USD", // Valor temporal
                    Monto = 0,      // Valor $0 hasta que Finanzas evalúe
                    Evidencia = $"/uploads/evidencias/{uName}",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreador = nombreUsuario
                };
                _context.ExtracostosOperacions.Add(costo);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteCostoAsync(int idCosto, int idOp)
        {
            var costo = await _context.ExtracostosOperacions.FindAsync(idCosto);
            if (costo != null)
            {
                _context.ExtracostosOperacions.Remove(costo);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = idOp });
        }

        // ========================================================================
        // FASE 1 - MOTOR ATILSON: EL CAZADOR AUTOMÁTICO DE TARIFAS (PUENTE SILENCIOSO)
        // ========================================================================
        private async Task InyectarProvisionesComercialesAsync(Operacione op, string usuario)
        {
            // CORRECCIÓN ATILSON: Creamos ambas variables para complacer a las tablas DateTime y DateOnly
            DateTime hoyDt = DateTime.Now.Date;
            DateOnly hoyDate = DateOnly.FromDateTime(DateTime.Now);
            DateTime hoyRegistro = DateTime.Now;

            // 1. INYECCIÓN MARÍTIMA (Venta y Costo Naviera)
            var serv = op.IdTipoServicio ?? 0;
            bool esMaritimo = serv == 1 || serv == 2 || serv == 3 || serv == 5 || serv == 8 || serv == 9 || serv == 10 || serv == 12;

            if (esMaritimo && op.IdNaviera != null)
            {
                // A) Buscar Tarifa de Venta (Cliente Mandante - Usa DateTime)
                var tarifaVentaMar = await _context.TarifasClientes 
                    .Where(t => t.IdCliente == op.IdCliente && t.EsActiva && t.GrupoCobro == "Marítimo"
                             && t.FechaInicioVigencia <= hoyDt && t.FechaFinVigencia >= hoyDt)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                if (tarifaVentaMar != null && !await _context.TransaccionesFinancieras.AnyAsync(t => t.IdOperacion == op.Id && t.GrupoCobro == "Marítimo" && t.TipoMovimiento == "INGRESO"))
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                    {
                        IdOperacion = op.Id,
                        TipoMovimiento = "INGRESO",
                        GrupoCobro = "Marítimo",
                        Concepto = tarifaVentaMar.Concepto,
                        Moneda = tarifaVentaMar.Moneda,
                        MontoNeto = tarifaVentaMar.EsServicioGratuito ? 0m : tarifaVentaMar.PrecioPactado,
                        EstadoFila = "PROVISIÓN",
                        ResponsablePago = "CLIENTE",
                        FechaCreacion = hoyRegistro,
                        UsuarioCreador = usuario
                    });
                }

                // B) Buscar Tarifa de Costo (Proveedor Naviera - Usa DateOnly)
                var tarifaCostoMar = await _context.TarifasMaritimas
                    .Where(t => t.IdNaviera == op.IdNaviera && t.FechaInicioVigencia <= hoyDate)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                if (tarifaCostoMar != null && !await _context.TransaccionesFinancieras.AnyAsync(t => t.IdOperacion == op.Id && t.GrupoCobro == "Marítimo" && t.TipoMovimiento == "EGRESO"))
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                    {
                        IdOperacion = op.Id,
                        IdProveedor = op.IdNaviera,
                        TipoMovimiento = "EGRESO",
                        GrupoCobro = "Marítimo",
                        Concepto = $"Flete Marítimo ({tarifaCostoMar.Equipamiento})",
                        Moneda = "USD",
                        MontoNeto = tarifaCostoMar.TarifaUsd,
                        EstadoFila = "PROVISIÓN",
                        ResponsablePago = "CLIENTE",
                        FechaCreacion = hoyRegistro,
                        UsuarioCreador = usuario
                    });
                }
            }

            // 2. INYECCIÓN TERRESTRE (Inland / Transporte - Usa DateTime)
            var terrDb = op.OperacionesTerrestres.FirstOrDefault();
            if (terrDb != null && !string.IsNullOrWhiteSpace(terrDb.PlantaCarga))
            {
                var tarifaVentaTerr = await _context.TarifasClientes
                    .Where(t => t.IdCliente == op.IdCliente && t.EsActiva && t.GrupoCobro == "Terrestre"
                             && (t.ZonaPlanta == terrDb.PlantaCarga || t.ZonaPlanta == terrDb.ZonaEmbarque || string.IsNullOrEmpty(t.ZonaPlanta))
                             && t.FechaInicioVigencia <= hoyDt && t.FechaFinVigencia >= hoyDt)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                if (tarifaVentaTerr != null && !await _context.TransaccionesFinancieras.AnyAsync(t => t.IdOperacion == op.Id && t.GrupoCobro == "Terrestre" && t.TipoMovimiento == "INGRESO"))
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                    {
                        IdOperacion = op.Id,
                        TipoMovimiento = "INGRESO",
                        GrupoCobro = "Terrestre",
                        Concepto = tarifaVentaTerr.Concepto,
                        Moneda = tarifaVentaTerr.Moneda,
                        MontoNeto = tarifaVentaTerr.EsServicioGratuito ? 0m : tarifaVentaTerr.PrecioPactado,
                        EstadoFila = "PROVISIÓN",
                        ResponsablePago = "CLIENTE",
                        FechaCreacion = hoyRegistro,
                        UsuarioCreador = usuario
                    });
                }
            }

            // 3. INYECCIÓN DOCUMENTAL (Agencia Aduana / Trámites - Usa DateTime)
            var docDb = op.OperacionesDocumentales.FirstOrDefault();
            if (docDb != null && !string.IsNullOrWhiteSpace(docDb.AgenciaAduana))
            {
                var tarifaVentaDoc = await _context.TarifasClientes
                    .Where(t => t.IdCliente == op.IdCliente && t.EsActiva && t.GrupoCobro == "Documental"
                             && t.FechaInicioVigencia <= hoyDt && t.FechaFinVigencia >= hoyDt)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                if (tarifaVentaDoc != null && !await _context.TransaccionesFinancieras.AnyAsync(t => t.IdOperacion == op.Id && t.GrupoCobro == "Documental" && t.TipoMovimiento == "INGRESO"))
                {
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                    {
                        IdOperacion = op.Id,
                        TipoMovimiento = "INGRESO",
                        GrupoCobro = "Documental",
                        Concepto = tarifaVentaDoc.Concepto,
                        Moneda = tarifaVentaDoc.Moneda,
                        MontoNeto = tarifaVentaDoc.EsServicioGratuito ? 0m : tarifaVentaDoc.PrecioPactado,
                        EstadoFila = "PROVISIÓN",
                        ResponsablePago = "CLIENTE",
                        FechaCreacion = hoyRegistro,
                        UsuarioCreador = usuario
                    });
                }
            }
        }
    }
}