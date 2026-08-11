using AtilsonCargoSpa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class EditModel : PageModel
    {
        private readonly AtilsonContext _context;

        public EditModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Finanzasoperacion Finanzas { get; set; } = new Finanzasoperacion();
        public Operacione OperacionBase { get; set; } = default!;
        public OperacionesDocumentale? Doc { get; set; }   // <-- NUEVO

        // Variables para el Dashboard en Vivo
        public decimal InitialVenta { get; set; }
        public decimal InitialCosto { get; set; }
        public decimal InitialProfit { get; set; }
        public decimal InitialMargen { get; set; }

        // Banderas visuales para saber si el sistema actuó automáticamente
        public bool TarifaMaritimaEncontrada { get; set; } = false;
        public bool TarifaTerrestreEncontrada { get; set; } = false;

        public bool TarifaGateEncontrada { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            OperacionBase = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.ExtracostosOperacions)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales) // <--- ESTA ES LA LÍNEA MÁGICA QUE FALTABA
                .FirstOrDefaultAsync(o => o.Id == id);


            if (OperacionBase == null) return NotFound();

            Doc = OperacionBase.OperacionesDocumentales?.OrderBy(d => d.Id).FirstOrDefault();

            // 🚀 SINCRONIZACIÓN DE CACHÉ PARA LA VISTA EDIT DE FINANZAS
            if (Doc != null)
            {
                bool cambioDoc = false;
                if (Doc.AplicaSag && (Doc.ValFit1 ?? 0) == 0) { Doc.ValFit1 = 35000m; cambioDoc = true; }
                if (Doc.AplicaSernapesca && (Doc.ValSan1 ?? 0) == 0) { Doc.ValSan1 = 35000m; cambioDoc = true; }
                if (Doc.CertificadoOrigen == true && (Doc.ValOri1 ?? 0) == 0) { Doc.ValOri1 = 25000m; cambioDoc = true; }
                if (cambioDoc) await _context.SaveChangesAsync();
            }

            var finanzasDb = await _context.Finanzasoperacions.FirstOrDefaultAsync(f => f.IdOperacion == id);

            // ... (el resto del código hacia abajo queda igual)

            if (finanzasDb != null)
            {
                Finanzas = finanzasDb;
                bool necesitaTerrestre = Finanzas.CostoTerrestreManual != true && (Finanzas.CostoTerrestreNeto ?? 0) == 0;
                bool necesitaGate = Finanzas.CostoGateManual != true && (Finanzas.CostoGateNeto ?? 0) == 0;

                if (necesitaTerrestre || necesitaGate)
                {
                    await IntentarInyectarCostosComercialesAsync(OperacionBase, necesitaTerrestre, necesitaGate);
                }
                CalcularDashboardInicial(Finanzas);
            }
            else
            {
                Finanzas = new Finanzasoperacion { IdOperacion = id };
                await IntentarInyectarCostosComercialesAsync(OperacionBase, true, true);
                CalcularDashboardInicial(Finanzas);
            }

            return Page();
        }

        private async Task IntentarInyectarCostosComercialesAsync(Operacione op, bool aplicarTerrestre = true, bool aplicarGate = true)
        {
            // A) Conexión Marítima (Protegida contra sobrescritura si ya fue conciliado o ingresado)
            if ((Finanzas.CostoMaritimoNeto ?? 0) == 0)
            {
                string nombrePol = op.IdPuertoOrigenNavigation?.NombrePuerto;
                string nombrePod = op.IdPuertoDestinoNavigation?.NombrePuerto;

                if (op.IdNaviera != null && !string.IsNullOrEmpty(nombrePol) && !string.IsNullOrEmpty(nombrePod))
                {
                    string nombrePolNorm = NormalizarTexto(nombrePol);
                    string nombrePodNorm = NormalizarTexto(nombrePod);

                    string equipoReq = (op.TipoContenedor ?? "40").ToUpper();
                    string size = equipoReq.Contains("20") ? "20" : "40";
                    string type = (equipoReq.Contains("RF") || equipoReq.Contains("REEFER") || op.IdTipoCarga == 2) ? "RF" : "DRY";

                    var tarifasNaviera = await _context.TarifasMaritimas
                        .Where(t => t.IdNaviera == op.IdNaviera && t.Pol != null && t.Pod != null)
                        .ToListAsync();

                    var tarifaMaritima = tarifasNaviera
                        .Where(t =>
                        {
                            string tPol = NormalizarTexto(t.Pol);
                            string tPod = NormalizarTexto(t.Pod);
                            string tEq = (t.Equipamiento ?? "").ToUpper();

                            bool matchPol = tPol == nombrePolNorm || tPol.Contains(nombrePolNorm) || nombrePolNorm.Contains(tPol);
                            bool matchPod = tPod == nombrePodNorm || tPod.Contains(nombrePodNorm) || nombrePodNorm.Contains(tPod);
                            bool matchEq = tEq.Contains(size) && (tEq.Contains(type) || (type == "RF" && tEq.Contains("REEFER")));

                            return matchPol && matchPod && matchEq;
                        })
                        .OrderByDescending(t => t.FechaInicioVigencia)
                        .FirstOrDefault();

                    if (tarifaMaritima != null)
                    {
                        Finanzas.CostoMaritimoNeto = tarifaMaritima.TarifaUsd;
                        TarifaMaritimaEncontrada = true;
                    }
                }
            }

            var opTerrestre = op.OperacionesTerrestres?.FirstOrDefault();

            // B) Conexión Terrestre (sin cambios, solo envuelta en el flag)
            if (aplicarTerrestre && opTerrestre != null && !string.IsNullOrEmpty(opTerrestre.EmpresaTransporte) && !string.IsNullOrEmpty(opTerrestre.PlantaCarga) && !string.IsNullOrEmpty(opTerrestre.PuertoEntrega))
            {
                var proveedor = await _context.Proveedores
                    .FirstOrDefaultAsync(p => p.NombreProveedor.Trim().ToUpper() == opTerrestre.EmpresaTransporte.Trim().ToUpper());

                if (proveedor != null)
                {
                    string puertoUpp = opTerrestre.PuertoEntrega.ToUpper();
                    string puertoAbreviado =
                        puertoUpp.Contains("SAN ANTONIO") ? "SAI" :
                        (puertoUpp.Contains("VALPARAISO") || puertoUpp.Contains("VALPARAÍSO")) ? "VAP" :
                        (puertoUpp.Contains("LIRQUEN") || puertoUpp.Contains("LIRQUÉN")) ? "LQN" :
                        puertoUpp.Contains("CORONEL") ? "COR" : puertoUpp;

                    string zonaLimpia = (opTerrestre.ZonaCarga ?? opTerrestre.PlantaCarga).ToUpper().Trim();
                    string zonaNorm = NormalizarTexto(zonaLimpia);
                    string puertoNorm = NormalizarTexto(puertoAbreviado);

                    var tarifasProveedor = await _context.Tarifasterrestres
                        .Where(t => t.IdProveedor == proveedor.Id && t.NombreTramo != null)
                        .ToListAsync();

                    var tarifaTerr = tarifasProveedor.FirstOrDefault(t =>
                    {
                        string nombreNorm = NormalizarTexto(t.NombreTramo);
                        return nombreNorm.Contains(puertoNorm) && nombreNorm.Contains(zonaNorm);
                    });

                    if (tarifaTerr != null)
                    {
                        Finanzas.CostoTerrestreNeto = tarifaTerr.ValorNeto;
                        TarifaTerrestreEncontrada = true;
                    }
                }
            }

            // C) Conexión Gate In/Out (Naviera + Depósito de retiro)
            if (aplicarGate && opTerrestre != null && op.IdNaviera != null && !string.IsNullOrEmpty(opTerrestre.DepositoRetiro))
            {
                var depositoDb = await _context.Depositos
                    .FirstOrDefaultAsync(d => d.NombreDeposito.Trim().ToUpper() == opTerrestre.DepositoRetiro.Trim().ToUpper());

                if (depositoDb != null)
                {
                    string equipoReq = (op.TipoContenedor ?? "40").ToUpper();
                    string size = equipoReq.Contains("20") ? "20" : "40";
                    int tipoCarga = op.IdTipoCarga == 0 ? 1 : op.IdTipoCarga;

                    var tarifasGate = await _context.TarifasGate
                        .Where(t => t.EsActiva && (t.IdNaviera == op.IdNaviera || t.IdNaviera == null) && t.IdDeposito == depositoDb.Id)
                        .ToListAsync();

                    var tarifasAplicables = tarifasGate
                        .Where(t =>
                            (string.IsNullOrEmpty(t.TipoContenedor) || t.TipoContenedor.Contains(size)) &&
                            (t.IdTipoCarga == null || t.IdTipoCarga == 0 || t.IdTipoCarga == tipoCarga))
                        .ToList();

                    // --- LÓGICA DE GATE SEPARADO CORREGIDA ---
                    string seleccionGate = (op.TipoGate ?? "IN/OUT").ToUpper();

                    var tarifaIn = (seleccionGate == "IN/OUT" || seleccionGate == "IN")
                        ? tarifasAplicables.Where(t => (t.TipoMovimiento ?? "").ToUpper() == "IN").OrderByDescending(t => t.FechaInicioVigencia).FirstOrDefault()
                        : null;

                    var tarifaOut = (seleccionGate == "IN/OUT" || seleccionGate == "OUT")
                        ? tarifasAplicables.Where(t => (t.TipoMovimiento ?? "").ToUpper() == "OUT").OrderByDescending(t => t.FechaInicioVigencia).FirstOrDefault()
                        : null;

                    decimal? totalGate = null;

                    if (tarifaIn != null || tarifaOut != null)
                    {
                        totalGate = (tarifaIn?.ValorNeto ?? 0m) + (tarifaOut?.ValorNeto ?? 0m);
                    }
                    else if (seleccionGate == "IN/OUT")
                    {
                        totalGate = tarifasAplicables.OrderByDescending(t => t.FechaInicioVigencia).FirstOrDefault()?.ValorNeto;
                    }

                    if (totalGate.HasValue)
                    {
                        Finanzas.CostoGateNeto = totalGate.Value;
                        TarifaGateEncontrada = true;
                    }
                }
            }
            // ================================================================
            // D) COSTOS DOCUMENTALES ESTÁNDAR Y EXTRACÓSTOS (NUEVO)
            // ================================================================
            var docOp = op.OperacionesDocumentales?.FirstOrDefault();
            if (docOp != null && (Finanzas.CostoAgenciaNeto ?? 0) == 0)
            {
                // Buscamos los honorarios de Agencia Base en nuestra nueva tabla
                var tarifaAgencia = await _context.Set<TarifasMaestra>()
                    .FirstOrDefaultAsync(t => t.Categoria == "Documental" && t.Concepto == "HONORARIOS AGENCIA" && t.EsActiva);

                if (tarifaAgencia != null)
                {
                    // Asignamos el valor neto a Finanzas
                    Finanzas.CostoAgenciaNeto = tarifaAgencia.ValorNeto;

                    // OPCIONAL: Si quieres inyectar un precio de "Venta" sugerido con un margen (Ej: Costo + 20%)
                    if ((Finanzas.VentaDocumental ?? 0) == 0)
                    {
                        Finanzas.VentaDocumental = tarifaAgencia.ValorNeto * 1.20m;
                    }
                }
            }

            // ================================================================
            // E) CONEXIÓN COMERCIAL: INYECCIÓN AUTOMÁTICA DE VENTAS (INGRESOS)
            // ================================================================
            // Buscamos los Acuerdos Comerciales (Cierres de Negocio) vigentes para ESTE cliente
            var acuerdosCliente = await _context.TarifasClientes
                .Where(t => t.IdCliente == op.IdCliente
                         && t.EsActiva
                         && t.FechaInicioVigencia <= DateTime.Now
                         && t.FechaFinVigencia >= DateTime.Now)
                .ToListAsync();

            if (acuerdosCliente.Any())
            {
                // 1. Inyectar Venta Marítima
                if ((Finanzas.VentaMaritimo ?? 0) == 0)
                {
                    string sizeOp = (op.TipoContenedor ?? "40").Contains("20") ? "20" : "40";
                    var ventaMar = acuerdosCliente.FirstOrDefault(t =>
                        t.GrupoCobro == "Marítimo" &&
                        (string.IsNullOrEmpty(t.TipoContenedor) || t.TipoContenedor.Contains(sizeOp)));

                    if (ventaMar != null)
                        Finanzas.VentaMaritimo = ventaMar.EsServicioGratuito ? 0 : ventaMar.PrecioPactado;
                }

                // 2. Inyectar Venta Terrestre (Por Zona/Planta)
                if (aplicarTerrestre && opTerrestre != null && (Finanzas.VentaTerrestre ?? 0) == 0)
                {
                    string zonaOpNorm = NormalizarTexto(opTerrestre.ZonaCarga ?? opTerrestre.PlantaCarga);
                    var ventaTer = acuerdosCliente.FirstOrDefault(t =>
                        t.GrupoCobro == "Terrestre" &&
                        !string.IsNullOrEmpty(t.ZonaPlanta) &&
                        NormalizarTexto(t.ZonaPlanta) == zonaOpNorm);

                    if (ventaTer != null)
                        Finanzas.VentaTerrestre = ventaTer.EsServicioGratuito ? 0 : ventaTer.PrecioPactado;
                }

                // 3. Inyectar Venta Gate
                if (aplicarGate && (Finanzas.VentaGate ?? 0) == 0)
                {
                    var ventaGate = acuerdosCliente.FirstOrDefault(t => t.GrupoCobro == "Gate");
                    if (ventaGate != null)
                        Finanzas.VentaGate = ventaGate.EsServicioGratuito ? 0 : ventaGate.PrecioPactado;
                }

                // 4. Inyectar Venta Documental / Honorarios
                // REGLA: Solo inyectar si Operaciones ya generó la fase Documental (docOp != null)
                if (docOp != null && (Finanzas.VentaDocumental ?? 0) == 0)
                {
                    var ventaDoc = acuerdosCliente.FirstOrDefault(t => t.GrupoCobro == "Documental");
                    if (ventaDoc != null)
                        Finanzas.VentaDocumental = ventaDoc.EsServicioGratuito ? 0 : ventaDoc.PrecioPactado;
                }
            }

            // Aquí podríamos agregar la misma lógica para inyectar automáticamente DUS o Sanitario
            // si el puente silencioso indica que Operaciones marcó "AplicaSAG = true" o "AplicaSernapesca = true"
            if (docOp != null && docOp.AplicaSag)
            {
                var tarifaSanitario = await _context.Set<TarifasMaestra>()
                    .FirstOrDefaultAsync(t => t.Concepto.Contains("SANITARIO") && t.EsActiva);
                // Lógica de inyección...
            }

        }

        // === MOTOR ATILSON: SINCRONIZADOR DE PROVISIONES HACIA EL LIBRO MAYOR ===
        // === MOTOR ATILSON: SINCRONIZADOR DE PROVISIONES HACIA EL LIBRO MAYOR ===
        private async Task SincronizarProvisionesFinancierasAsync(int idOperacion)
        {
            var fin = await _context.Finanzasoperacions.FirstOrDefaultAsync(f => f.IdOperacion == idOperacion);
            if (fin == null) return;

            var op = await _context.Operaciones
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesDocumentales)
                .Include(o => o.ExtracostosOperacions)
                .FirstOrDefaultAsync(o => o.Id == idOperacion);
            if (op == null) return;

            var txExistentes = await _context.TransaccionesFinancieras
                .Where(t => t.IdOperacion == idOperacion)
                .ToListAsync();

            string usuario = User.Identity?.Name ?? "Sistema";
            DateTime ahora = DateTime.Now;

            // Helper local para crear o actualizar provisiones sin duplicar
            void UpsertProvision(string grupo, string concepto, decimal monto, string moneda, int? idProveedor = null, int? idCliente = null, string tipoMov = "EGRESO", string responsable = "CLIENTE")
            {
                if (monto <= 0) return;

                var tx = txExistentes.FirstOrDefault(t =>
                    t.GrupoCobro.ToUpper() == grupo.ToUpper() &&
                    t.TipoMovimiento == tipoMov &&
                    t.Concepto == concepto);

                if (tx == null)
                {
                    tx = new TransaccionesFinanciera
                    {
                        IdOperacion = idOperacion,
                        GrupoCobro = grupo,
                        TipoMovimiento = tipoMov,
                        Concepto = concepto,
                        MontoNeto = monto,
                        Moneda = moneda,
                        IdProveedor = idProveedor,
                        IdCliente = idCliente,
                        ResponsablePago = responsable, // <-- SE GUARDA LA RESPONSABILIDAD
                        EstadoFila = "PROVISIÓN",
                        FechaCreacion = ahora,
                        UsuarioCreador = usuario
                    };
                    _context.TransaccionesFinancieras.Add(tx);
                    txExistentes.Add(tx);
                }
                else if (tx.EstadoFila == "PROVISIÓN" || tx.EstadoFila == "PROVISION")
                {
                    tx.MontoNeto = monto;
                    tx.Moneda = moneda;
                    tx.ResponsablePago = responsable; // <-- SE ACTUALIZA LA RESPONSABILIDAD
                    if (idProveedor.HasValue) tx.IdProveedor = idProveedor;
                    if (idCliente.HasValue) tx.IdCliente = idCliente;
                    tx.FechaModificacion = ahora;
                    tx.UsuarioModificador = usuario;
                }
            }

            // 1. Marítimo (Costo a Naviera y Venta a Cliente)
            if ((fin.CostoMaritimoNeto ?? 0) > 0)
                UpsertProvision("Marítimo", "Flete Marítimo", fin.CostoMaritimoNeto.Value, "USD", op.IdNaviera);
            if ((fin.VentaMaritimo ?? 0) > 0)
                UpsertProvision("Marítimo", "Venta Flete Marítimo", fin.VentaMaritimo.Value, "USD", null, op.IdCliente, "INGRESO");

            // 2. Terrestre (Costo a Transportista y Venta a Cliente)
            var terr = op.OperacionesTerrestres?.FirstOrDefault();
            int? idProvTerr = null;
            if (terr != null && !string.IsNullOrEmpty(terr.EmpresaTransporte))
            {
                var provDb = await _context.Proveedores.FirstOrDefaultAsync(p => p.NombreProveedor.Trim().ToUpper() == terr.EmpresaTransporte.Trim().ToUpper());
                idProvTerr = provDb?.Id;
            }
            if ((fin.CostoTerrestreNeto ?? 0) > 0)
                UpsertProvision("Terrestre", "Flete Terrestre Inland", fin.CostoTerrestreNeto.Value, "CLP", idProvTerr);
            if ((fin.VentaTerrestre ?? 0) > 0)
                UpsertProvision("Terrestre", "Venta Flete Terrestre", fin.VentaTerrestre.Value, "CLP", null, op.IdCliente, "INGRESO");

            // 3. Documental / Aduana
            var doc = op.OperacionesDocumentales?.FirstOrDefault();
            if ((fin.CostoAgenciaNeto ?? 0) > 0)
                UpsertProvision("Documental", "Trámites Agencia Aduana", fin.CostoAgenciaNeto.Value, "CLP", doc?.IdAgenciaAduana);
            if ((fin.VentaDocumental ?? 0) > 0)
                UpsertProvision("Documental", "Venta Trámites Documentales", fin.VentaDocumental.Value, "CLP", null, op.IdCliente, "INGRESO");

            // 4. Gate In / Out
            if ((fin.CostoGateNeto ?? 0) > 0)
                UpsertProvision("Gate", "Gate In / Gate Out", fin.CostoGateNeto.Value, "CLP", op.IdNaviera);
            if ((fin.VentaGate ?? 0) > 0)
                UpsertProvision("Gate", "Venta Gate In / Out", fin.VentaGate.Value, "CLP", null, op.IdCliente, "INGRESO");

            // 5. Extracostos Operativos (AQUÍ MAPEAMOS EL RESPONSABLE)
            if (op.ExtracostosOperacions != null)
            {
                foreach (var extra in op.ExtracostosOperacions.Where(e => e.Monto > 0))
                {
                    string mon = string.IsNullOrEmpty(extra.Moneda) ? "USD" : extra.Moneda;
                    string resp = string.IsNullOrEmpty(extra.Responsable) ? "CLIENTE" : extra.Responsable.ToUpper();
                    UpsertProvision("Extracosto", extra.TipoCosto ?? "Recargo Operativo", extra.Monto, mon, null, null, "EGRESO", resp);
                }
            }

            await _context.SaveChangesAsync();
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            string s = texto.ToUpper().Trim();
            s = s.Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U").Replace("Ñ", "N");
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s;
        }


        private void CalcularDashboardInicial(Finanzasoperacion fin)
        {
            decimal totalExtracostos = OperacionBase?.ExtracostosOperacions?.Sum(e => e.Monto) ?? 0m;

            InitialVenta = (fin.VentaMaritimo ?? 0) + (fin.VentaTerrestre ?? 0) + (fin.VentaDocumental ?? 0) + (fin.VentaGate ?? 0);
            InitialCosto = (fin.CostoMaritimoNeto ?? 0) + (fin.CostoTerrestreNeto ?? 0) + (fin.CostoAgenciaNeto ?? 0) + (fin.CostoGateNeto ?? 0) + totalExtracostos;
            InitialProfit = InitialVenta - InitialCosto;
            if (InitialVenta > 0) InitialMargen = (InitialProfit / InitialVenta) * 100;
        }

        public async Task<IActionResult> OnPostAsync(string AccionFinanzas)
        {
            // Se limpia el ModelState porque Finanzasoperacion es una entidad EF enlazada directo
            // al formulario y trae propiedades (navegaciones, strings no-nullable, etc.) que no
            // se envían en el form, generando un ModelState inválido "silencioso" que abortaba el
            // guardado completo (incluyendo Venta/Costo y Extracostos) y solo recargaba la página.
            // --- 🔒 REGLA EDMUNDO: CANDADO DE AUDITORÍA FINANCIERA ---
            var opDb = await _context.Operaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == Finanzas.IdOperacion);
            if (opDb != null && opDb.LockFinanzas)
            {
                // Si la operación está bloqueada por finanzas, verificamos si es Jefatura intentando desbloquear
                if (User.IsInRole("Admin") || User.IsInRole("Jefatura") || User.Identity?.Name == "Cristian")
                {
                    // Permitimos el paso solo si el usuario tiene privilegios ejecutivos
                }
                else
                {
                    TempData["ErrorMsg"] = "🚨 ACCESO DENEGADO: Esta operación ya fue liquidada o facturada en Finanzas. No se pueden modificar datos logísticos sin autorización de Jefatura.";
                    return RedirectToPage("./Index");
                }
            }

            ModelState.Clear();

            var finanzasDb = await _context.Finanzasoperacions.FirstOrDefaultAsync(f => f.IdOperacion == Finanzas.IdOperacion);
            string currentUser = User.Identity?.Name ?? "Finanzas";

            // Declaramos y obtenemos el valor original de la base de datos (o 0 si es un registro nuevo)
            decimal valorOriginalTerrestre = finanzasDb != null ? (finanzasDb.CostoTerrestreNeto ?? 0m) : 0m;

            bool costoTerrestreEditadoManualmente = Math.Abs(valorOriginalTerrestre - (Finanzas.CostoTerrestreNeto ?? 0m)) > 0.5m;

            decimal valorOriginalGate = finanzasDb != null ? (finanzasDb.CostoGateNeto ?? 0m) : 0m;

            bool costoGateEditadoManualmente = Math.Abs(valorOriginalGate - (Finanzas.CostoGateNeto ?? 0m)) > 0.5m;

            if (finanzasDb == null)
            {
                Finanzas.FechaCreacion = DateTime.Now;
                Finanzas.UsuarioCreador = currentUser;
                Finanzas.CostoTerrestreManual = costoTerrestreEditadoManualmente;
                Finanzas.CostoGateManual = costoGateEditadoManualmente;
                _context.Finanzasoperacions.Add(Finanzas);

            }
            else
            {
                finanzasDb.VentaMaritimo = Finanzas.VentaMaritimo;
                finanzasDb.CostoMaritimoNeto = Finanzas.CostoMaritimoNeto;
                finanzasDb.IdCondicionFlete = Finanzas.IdCondicionFlete;

                finanzasDb.VentaTerrestre = Finanzas.VentaTerrestre;
                finanzasDb.CostoTerrestreNeto = Finanzas.CostoTerrestreNeto;
                finanzasDb.VentaGate = Finanzas.VentaGate;
                finanzasDb.CostoGateNeto = Finanzas.CostoGateNeto;
                if (costoGateEditadoManualmente)
                {
                    finanzasDb.CostoGateManual = true;
                }
                if (costoTerrestreEditadoManualmente)
                {
                    finanzasDb.CostoTerrestreManual = true;
                }

                finanzasDb.VentaDocumental = Finanzas.VentaDocumental;
                finanzasDb.CostoAgenciaNeto = Finanzas.CostoAgenciaNeto;

                finanzasDb.FechaModificacion = DateTime.Now;
                finanzasDb.UsuarioModificador = currentUser;
            }

            // 2. CATEGORÍA 4: EXTRACostos
            var extraCostosDb = await _context.ExtracostosOperacions.Where(e => e.IdOperacion == Finanzas.IdOperacion).ToListAsync();
            foreach (var costo in extraCostosDb)
            {
                if (Request.Form.TryGetValue($"Moneda_{costo.Id}", out var monedaVal))
                    costo.Moneda = monedaVal.ToString();

                if (Request.Form.TryGetValue($"Monto_{costo.Id}", out var montoVal))
                {
                    string montoStr = montoVal.ToString().Replace(",", ".");
                    if (decimal.TryParse(montoStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal montoParseado))
                        costo.Monto = montoParseado;
                }
            }

            // 3. VALIDAR LA ACCIÓN FINAL (Bloqueo y Desbloqueo)
            if (AccionFinanzas == "Liquidar")
            {
                var operacion = await _context.Operaciones.FindAsync(Finanzas.IdOperacion);
                if (operacion != null)
                {
                    operacion.LockFinanzas = true;
                    operacion.Comentarios = $"[{DateTime.Now:dd/MM/yyyy HH:mm} FINANZAS] Operación Liquidada Exitosamente.\n" + (operacion.Comentarios ?? "");
                }
                TempData["SuccessMsg"] = "La operación ha sido Liquidada y los costos fueron cerrados con éxito.";
            }
            else if (AccionFinanzas == "Desbloquear")
            {
                var operacion = await _context.Operaciones.FindAsync(Finanzas.IdOperacion);
                if (operacion != null)
                {
                    operacion.LockFinanzas = false;
                    operacion.Comentarios = $"[{DateTime.Now:dd/MM/yyyy HH:mm} FINANZAS] Operación Desbloqueada. Devuelta a Control Operativo.\n" + (operacion.Comentarios ?? "");
                }
                TempData["SuccessMsg"] = "Operación desbloqueada y devuelta a Operaciones correctamente.";
            }

            await _context.SaveChangesAsync();

            // 3. GUARDADO DE COSTOS DOCUMENTALES (DESDE FINANZAS HACIA OPERACIONES)
            // DESPUÉS
            var docDb = await _context.OperacionesDocumentales
                .Where(d => d.IdOperacion == Finanzas.IdOperacion)
                .OrderBy(d => d.Id)
                .FirstOrDefaultAsync();
            if (docDb != null)
            {
                string usuarioFin = User.Identity?.Name ?? "Finanzas";

                decimal? FDecFin(string key)
                {
                    if (!Request.Form.TryGetValue(key, out var raw)) return null;
                    string val = raw.ToString().Trim();
                    if (string.IsNullOrEmpty(val)) return null;
                    val = val.Replace(",", ".");
                    return decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal r) ? r : (decimal?)null;
                }

                void AplicarValor(string formKey, Func<decimal?> getActual, Action<decimal> setValor, Func<string?> getLog, Action<string> setLog, string etiqueta)
                {
                    var nuevo = FDecFin(formKey);
                    if (!nuevo.HasValue) return;
                    if (getActual() == nuevo.Value) return; // sin cambios, no duplicar incidencia

                    setValor(nuevo.Value);

                    string fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    string entrada = "<div style='border-left:2px solid #16a34a; padding:6px; margin-bottom:6px; background:#f0fdf4; border-top:1px solid #bbf7d0; border-right:1px solid #bbf7d0; border-bottom:1px solid #bbf7d0;'>" +
                        "<div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:4px;'>" +
                        "<span style='font-weight:800; color:#16a34a; text-transform:uppercase; font-size:10px;'>VALORIZADO POR FINANZAS</span>" +
                        $"<span style='color:#64748b; font-size:9px;'>{fecha}</span></div>" +
                        $"<div style='color:#475569; font-size:10px; margin-bottom:3px;'>Por: <strong>{usuarioFin}</strong></div>" +
                        $"<div style='color:#1e293b; font-size:11px; line-height:1.2;'>{etiqueta}: <strong>${nuevo.Value:N0}</strong></div></div>\n";

                    setLog(entrada + (getLog() ?? ""));
                }

                // DUS / DIN (sin panel de log propio en Aduana)
                var vDus = FDecFin("Doc.ValorDus"); if (vDus.HasValue) docDb.ValorDus = vDus.Value;
                var vDin = FDecFin("Doc.ValorDin"); if (vDin.HasValue) docDb.ValorDin = vDin.Value;

                // ORIGEN (Principal + Reemisiones)
                AplicarValor("Doc.ValOri1", () => docDb.ValOri1, v => docDb.ValOri1 = v, () => docDb.LogOrigen, v => docDb.LogOrigen = v, "Cert. Origen (Principal)");
                AplicarValor("Doc.ValOri2", () => docDb.ValOri2, v => docDb.ValOri2 = v, () => docDb.LogOrigen, v => docDb.LogOrigen = v, "Cert. Origen (Reemisión 1)");
                AplicarValor("Doc.ValOri3", () => docDb.ValOri3, v => docDb.ValOri3 = v, () => docDb.LogOrigen, v => docDb.LogOrigen = v, "Cert. Origen (Reemstring nombrePodNorm = NormalizarTexto(pod?.NombrePuerto ?? nombrePod);isión 2)");
                AplicarValor("Doc.ValOri4", () => docDb.ValOri4, v => docDb.ValOri4 = v, () => docDb.LogOrigen, v => docDb.LogOrigen = v, "Cert. Origen (Reemisión 3)");

                // FITOSANITARIO
                AplicarValor("Doc.ValFit1", () => docDb.ValFit1, v => docDb.ValFit1 = v, () => docDb.LogFitosanitario, v => docDb.LogFitosanitario = v, "Fitosanitario (Principal)");
                AplicarValor("Doc.ValFit2", () => docDb.ValFit2, v => docDb.ValFit2 = v, () => docDb.LogFitosanitario, v => docDb.LogFitosanitario = v, "Fitosanitario (Reemisión 1)");
                AplicarValor("Doc.ValFit3", () => docDb.ValFit3, v => docDb.ValFit3 = v, () => docDb.LogFitosanitario, v => docDb.LogFitosanitario = v, "Fitosanitario (Reemisión 2)");
                AplicarValor("Doc.ValFit4", () => docDb.ValFit4, v => docDb.ValFit4 = v, () => docDb.LogFitosanitario, v => docDb.LogFitosanitario = v, "Fitosanitario (Reemisión 3)");

                // SANITARIO
                AplicarValor("Doc.ValSan1", () => docDb.ValSan1, v => docDb.ValSan1 = v, () => docDb.LogSanitario, v => docDb.LogSanitario = v, "Sanitario (Principal)");
                AplicarValor("Doc.ValSan2", () => docDb.ValSan2, v => docDb.ValSan2 = v, () => docDb.LogSanitario, v => docDb.LogSanitario = v, "Sanitario (Reemisión 1)");
                AplicarValor("Doc.ValSan3", () => docDb.ValSan3, v => docDb.ValSan3 = v, () => docDb.LogSanitario, v => docDb.LogSanitario = v, "Sanitario (Reemisión 2)");
                AplicarValor("Doc.ValSan4", () => docDb.ValSan4, v => docDb.ValSan4 = v, () => docDb.LogSanitario, v => docDb.LogSanitario = v, "Sanitario (Reemisión 3)");

                // CAPTURA
                AplicarValor("Doc.ValCap1", () => docDb.ValCap1, v => docDb.ValCap1 = v, () => docDb.LogCaptura, v => docDb.LogCaptura = v, "Cert. Captura (Principal)");
                AplicarValor("Doc.ValCap2", () => docDb.ValCap2, v => docDb.ValCap2 = v, () => docDb.LogCaptura, v => docDb.LogCaptura = v, "Cert. Captura (Reemisión 1)");
                AplicarValor("Doc.ValCap3", () => docDb.ValCap3, v => docDb.ValCap3 = v, () => docDb.LogCaptura, v => docDb.LogCaptura = v, "Cert. Captura (Reemisión 2)");
                AplicarValor("Doc.ValCap4", () => docDb.ValCap4, v => docDb.ValCap4 = v, () => docDb.LogCaptura, v => docDb.LogCaptura = v, "Cert. Captura (Reemisión 3)");

                // SUBCERTIFICADOS
                AplicarValor("Doc.ValCoa1", () => docDb.ValCoa1, v => docDb.ValCoa1 = v, () => docDb.LogCoa, v => docDb.LogCoa = v, "COA");
                AplicarValor("Doc.ValDt1", () => docDb.ValDt1, v => docDb.ValDt1 = v, () => docDb.LogDt, v => docDb.LogDt = v, "DT");
                AplicarValor("Doc.ValCod1", () => docDb.ValCod1, v => docDb.ValCod1 = v, () => docDb.LogCodaut, v => docDb.LogCodaut = v, "CODAUT");
                AplicarValor("Doc.ValCla1", () => docDb.ValCla1, v => docDb.ValCla1 = v, () => docDb.LogClave, v => docDb.LogClave = v, "CLAVE");
                AplicarValor("Doc.ValNep1", () => docDb.ValNep1, v => docDb.ValNep1 = v, () => docDb.LogNeppex, v => docDb.LogNeppex = v, "NEPPEX");

                await _context.SaveChangesAsync(); // <-- ESTE GUARDADO FALTABA (bug crítico)
            }

            // 🚀 INVOCACIÓN AL SINCRONIZADOR DE PROVISIONES
            await SincronizarProvisionesFinancierasAsync(Finanzas.IdOperacion);

            return RedirectToPage("./Index");
        }
    }
}