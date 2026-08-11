using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class FacturacionModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;

        // 👇 DEBES TENER SOLO ESTE CONSTRUCTOR. SI TIENES OTRO PARECIDO, BÓRRALO.
        public FacturacionModel(AtilsonContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==================== LISTAS PARA EL TABLERO ====================
        public IList<Operacione> OperacionesFacturacion { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FilterColumna { get; set; } = "ALL"; // ALL, ABIERTAS, ATRASADAS, PAGADAS, DISPUTADAS

        // ==================== METRICAS INTELIGENTES (KPIs) ====================
        public int CantidadAbiertas { get; set; }
        public int CantidadParciales { get; set; } // <-- NUEVO: Control de Pagos Parciales
        public int CantidadAtrasadas { get; set; }
        public int CantidadPagadas { get; set; }
        public int CantidadDisputadas { get; set; }

        public decimal TotalFletePendiente { get; set; }
        public decimal TotalExtrasUSD { get; set; }
        public decimal TotalExtrasCLP { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.Finanzasoperacions)
                .Include(o => o.ExtracostosOperacions)
                .Include(o => o.TransaccionesFinancieras)
                .Where(o => !o.IsDeleted && o.Finanzasoperacions.Any())
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower();
                query = query.Where(o =>
                    (o.NumeroBooking != null && o.NumeroBooking.ToLower().Contains(s)) ||
                    (o.IdClienteNavigation != null && o.IdClienteNavigation.RazonSocial != null && o.IdClienteNavigation.RazonSocial.ToLower().Contains(s))
                );
            }

            OperacionesFacturacion = await query.OrderByDescending(o => o.Id).ToListAsync();

            DateTime hoy = DateTime.Now;

            // Clasificación algorítmica para las 5 columnas estratégicas
            foreach (var op in OperacionesFacturacion)
            {
                var f = op.Finanzasoperacions.FirstOrDefault();
                var txIngresos = op.TransaccionesFinancieras?
                    .Where(t => t.TipoMovimiento == "INGRESO" && (t.EstadoFila == "FACTURADO" || t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO"))
                    .ToList() ?? new List<TransaccionesFinanciera>();

                bool tieneFacturas = txIngresos.Any();
                bool todasPagadas = tieneFacturas && txIngresos.All(t => t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO");
                bool algunaPagada = tieneFacturas && txIngresos.Any(t => t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO");
                bool enDisputa = op.Comentarios != null && (op.Comentarios.Contains("[DISPUTA]") || op.Comentarios.Contains("[RECLAMO]"));

                double diasAntiguedad = (hoy - op.FechaCreacion).Value.TotalDays;
                bool estaAtrasada = !todasPagadas && !algunaPagada && diasAntiguedad > 15;

                if (enDisputa)
                {
                    CantidadDisputadas++;
                }
                else if (todasPagadas)
                {
                    CantidadPagadas++;
                }
                else if (algunaPagada)
                {
                    CantidadParciales++; // Entra en estado "Pago Parcial"
                }
                else if (estaAtrasada)
                {
                    CantidadAtrasadas++;
                    AcumularMontoPendiente(op, f);
                }
                else
                {
                    CantidadAbiertas++;
                    if (!tieneFacturas) AcumularMontoPendiente(op, f);
                }
            }
        }

        private void AcumularMontoPendiente(Operacione op, Finanzasoperacion? f)
        {
            if (f != null)
            {
                TotalFletePendiente += (f.VentaMaritimo ?? 0) + (f.VentaTerrestre ?? 0) + (f.VentaDocumental ?? 0);
            }
            if (op.ExtracostosOperacions != null && op.ExtracostosOperacions.Any())
            {
                TotalExtrasUSD += op.ExtracostosOperacions.Where(e => e.Moneda == "USD").Sum(e => e.Monto);
                TotalExtrasCLP += op.ExtracostosOperacions.Where(e => e.Moneda == "CLP").Sum(e => e.Monto);
            }
        }

        // === MAGIA ATILSON: MOTOR DE COBRANZA Y EMISIÓN DOCUMENTAL (AR) ===
        public async Task<IActionResult> OnPostEmitirDocumentoCobroAsync(int idOperacion, string numeroFactura, string tipoEmision, decimal montoTotalConfirmado, int idContextual, string? conceptoContextual, string? monedaContextual)
        {
            ModelState.Clear();

            var op = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .FirstOrDefaultAsync(o => o.Id == idOperacion);

            if (op == null) return NotFound();

            string usuario = User.Identity?.Name ?? "Cobranzas";
            DateTime ahora = DateTime.Now;

            var ingresosDb = await _context.TransaccionesFinancieras
                .Where(t => t.IdOperacion == idOperacion && t.TipoMovimiento == "INGRESO")
                .ToListAsync();

            if (tipoEmision == "FLETE")
            {
                // Toma TODAS las provisiones de Flete que estén pendientes y las agrupa en la Factura
                var fletesPendientes = ingresosDb.Where(t => t.GrupoCobro.ToUpper() != "EXTRACOSTO" && (t.EstadoFila == "PROVISIÓN" || t.EstadoFila == "PROVISION")).ToList();

                if (fletesPendientes.Any())
                {
                    foreach (var tx in fletesPendientes)
                    {
                        tx.NumeroDocumento = numeroFactura;
                        tx.EstadoFila = "FACTURADO";
                        tx.FechaModificacion = ahora;
                        tx.UsuarioModificador = usuario;
                    }
                    op.Comentarios = $"[{ahora:dd/MM/yyyy HH:mm} COBRANZA] Factura N° {numeroFactura} emitida agrupando {fletesPendientes.Count} servicios base. Por: {usuario}.\n" + (op.Comentarios ?? "");
                }
                else
                {
                    // Fallback de seguridad
                    _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                    {
                        IdOperacion = idOperacion,
                        GrupoCobro = "Marítimo",
                        TipoMovimiento = "INGRESO",
                        Concepto = "Flete y Servicios Base",
                        MontoNeto = montoTotalConfirmado,
                        Moneda = "USD",
                        EstadoFila = "FACTURADO",
                        NumeroDocumento = numeroFactura,
                        FechaCreacion = ahora,
                        UsuarioCreador = usuario
                    });
                    op.Comentarios = $"[{ahora:dd/MM/yyyy HH:mm} COBRANZA] Factura N° {numeroFactura} emitida. Por: {usuario}.\n" + (op.Comentarios ?? "");
                }
                TempData["SuccessMsg"] = $"Factura N° {numeroFactura} emitida exitosamente para los fletes.";
            }
            else if (tipoEmision == "EXTRACOSTO_TX")
            {
                // Actualiza una provisión existente a Facturado
                var tx = await _context.TransaccionesFinancieras.FindAsync(idContextual);
                if (tx != null)
                {
                    tx.NumeroDocumento = numeroFactura;
                    tx.EstadoFila = "FACTURADO";
                    tx.MontoNeto = montoTotalConfirmado;
                    tx.FechaModificacion = ahora;
                    tx.UsuarioModificador = usuario;

                    op.Comentarios = $"[{ahora:dd/MM/yyyy HH:mm} COBRANZA] NC N° {numeroFactura} emitida por concepto de {tx.Concepto}. Por: {usuario}.\n" + (op.Comentarios ?? "");
                    TempData["SuccessMsg"] = $"Nota de Cobro N° {numeroFactura} enlazada exitosamente.";
                }
            }
            else if (tipoEmision == "EXTRACOSTO_OP")
            {
                // Crea la transacción nueva directamente desde el Extracosto Operativo con su nombre REAL
                var ex = await _context.ExtracostosOperacions.FindAsync(idContextual);
                _context.TransaccionesFinancieras.Add(new TransaccionesFinanciera
                {
                    IdOperacion = idOperacion,
                    GrupoCobro = "Extracosto",
                    TipoMovimiento = "INGRESO",
                    Concepto = conceptoContextual ?? "Extracosto Operativo", // ¡AQUÍ ESTÁ LA MAGIA DEL ENLACE!
                    MontoNeto = montoTotalConfirmado,
                    Moneda = monedaContextual ?? "USD",
                    EstadoFila = "FACTURADO",
                    NumeroDocumento = numeroFactura,
                    FechaCreacion = ahora,
                    UsuarioCreador = usuario
                });

                if (ex != null) ex.Motivo = $"[NC N° {numeroFactura}] " + (ex.Motivo ?? "");

                op.Comentarios = $"[{ahora:dd/MM/yyyy HH:mm} COBRANZA] NC N° {numeroFactura} emitida por {conceptoContextual}. Por: {usuario}.\n" + (op.Comentarios ?? "");
                TempData["SuccessMsg"] = $"Nota de Cobro N° {numeroFactura} creada y sincronizada a Operaciones.";
            }

            op.EstadoWorkflow = "FACTURADO";
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // ==================== ACCIÓN RÁPIDA: MARCAR COMO PAGADO O EN DISPUTA ====================
        public async Task<IActionResult> OnPostCambiarEstadoFacturaAsync(int idOperacion, string nuevoEstado, string? motivoDisputa)
        {
            var op = await _context.Operaciones.FindAsync(idOperacion);
            if (op == null) return NotFound();

            string usuario = User.Identity?.Name ?? "Cobranzas";
            DateTime ahora = DateTime.Now;

            var txIngresos = await _context.TransaccionesFinancieras
                .Where(t => t.IdOperacion == idOperacion && t.TipoMovimiento == "INGRESO")
                .ToListAsync();

            if (nuevoEstado == "PAGADO")
            {
                foreach (var tx in txIngresos)
                {
                    tx.EstadoFila = "PAGADO";
                    tx.FechaModificacion = ahora;
                }

                // --- CIERRE DE CICLO: La operación queda sellada y finalizada ---
                op.EstadoWorkflow = "CERRADO";

                op.Comentarios = $"[{ahora:dd/MM/yyyy HH:mm} BANCO] Factura confirmada como PAGADA/RECAUDADA por {usuario}.\n" + (op.Comentarios ?? "");
                TempData["SuccessMsg"] = $"Booking {op.NumeroBooking} cerrado y marcado como PAGADO.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // === MAGIA ATILSON: BÓVEDA FINANCIERA (SUBIDA DE RESPALDOS) ===
        public async Task<IActionResult> OnPostSubirComprobanteAsync(int idTransaccionAdjunto, IFormFile archivoComprobante)
        {
            if (archivoComprobante != null && archivoComprobante.Length > 0)
            {
                // 1. Crear carpeta segura si no existe
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "finanzas");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 2. Solo aceptamos PDF por seguridad y estandarización
                string extension = Path.GetExtension(archivoComprobante.FileName).ToLower();
                if (extension != ".pdf")
                {
                    TempData["ErrorMsg"] = "Por seguridad, la bóveda financiera solo acepta formato PDF.";
                    return RedirectToPage();
                }

                // 3. Forzar el nombre exacto ligado al ID de la Transacción
                string fileName = $"Comprobante_TX_{idTransaccionAdjunto}.pdf";
                string filePath = Path.Combine(uploadsFolder, fileName);

                // 4. Guardar archivo físico
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await archivoComprobante.CopyToAsync(fileStream);
                }

                // 5. Dejar registro en la bitácora
                var tx = await _context.TransaccionesFinancieras.FindAsync(idTransaccionAdjunto);
                if (tx != null)
                {
                    var op = await _context.Operaciones.FindAsync(tx.IdOperacion);
                    if (op != null)
                    {
                        string usuario = User.Identity?.Name ?? "Cobranzas";
                        op.Comentarios = $"[{DateTime.Now:dd/MM/yyyy HH:mm} BÓVEDA] PDF adjuntado como respaldo a (Ref: {tx.NumeroDocumento ?? tx.Concepto}). Por: {usuario}.\n" + (op.Comentarios ?? "");
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMsg"] = "Documento de respaldo guardado exitosamente en la bóveda.";
            }
            return RedirectToPage();
        }

        // ==================== ACCIÓN GRANULAR: PAGO POR DOCUMENTO INDIVIDUAL ====================
        public async Task<IActionResult> OnPostMarcarPagoTransaccionAsync(int idTransaccion)
        {
            var tx = await _context.TransaccionesFinancieras.FindAsync(idTransaccion);
            if (tx == null) return NotFound();

            tx.EstadoFila = "PAGADO";
            tx.FechaModificacion = DateTime.Now;
            string usuario = User.Identity?.Name ?? "Cobranzas";
            tx.UsuarioModificador = usuario;

            // Verificar si con este pago, la operación ya se pagó al 100%
            var todasTx = await _context.TransaccionesFinancieras
                .Where(t => t.IdOperacion == tx.IdOperacion && t.TipoMovimiento == "INGRESO" && (t.EstadoFila == "FACTURADO" || t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO"))
                .ToListAsync();

            if (todasTx.Any() && todasTx.All(t => t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO"))
            {
                var op = await _context.Operaciones.FindAsync(tx.IdOperacion);
                if (op != null)
                {
                    op.EstadoWorkflow = "CERRADO";
                    op.Comentarios = $"[{DateTime.Now:dd/MM/yyyy HH:mm} BANCO] Todos los saldos fueron recaudados. Operación 100% PAGADA por {usuario}.\n" + (op.Comentarios ?? "");
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = $"El documento {tx.NumeroDocumento} ({tx.Moneda}) ha sido marcado como PAGADO correctamente.";
            return RedirectToPage();
        }
    }
}