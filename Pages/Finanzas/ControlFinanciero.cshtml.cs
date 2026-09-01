using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class ControlFinancieroModel : PageModel
    {
        private readonly AtilsonContext _context;
        private readonly IWebHostEnvironment _env;

        public ControlFinancieroModel(AtilsonContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IList<Operacione> Operaciones { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Operaciones = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.OperacionesDocumentales)
                    .ThenInclude(d => d.IdAgenciaAduanaNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.OperacionesAlmacenamientos)
                    .ThenInclude(a => a.IdProveedorNavigation)
                .Include(o => o.TransaccionesFinancieras)
                    .ThenInclude(t => t.IdProveedorNavigation)
                .OrderByDescending(o => o.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGuardarNumeroDocAsync(int transaccionId, string numeroDoc)
        {
            if (transaccionId > 0 && !string.IsNullOrWhiteSpace(numeroDoc))
            {
                var transaccion = await _context.TransaccionesFinancieras.FindAsync(transaccionId);
                if (transaccion != null)
                {
                    transaccion.NumeroDocumento = numeroDoc.Trim().ToUpper();
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage("./ControlFinanciero");
        }

        public async Task<IActionResult> OnPostEmitirNotaCobroAsync(int operacionId, List<int> idsExentos)
        {
            bool tienePendientes = await _context.TransaccionesFinancieras
                .AnyAsync(t => t.IdOperacion == operacionId && t.TipoMovimiento == "INGRESO" && t.MontoNeto == 0m && !t.TarifaManual);

            bool tieneIncompletos = await _context.TransaccionesFinancieras
                .AnyAsync(t => t.IdOperacion == operacionId && t.TipoMovimiento == "INGRESO" &&
                               string.IsNullOrWhiteSpace(t.NumeroDocumento) &&
                               (t.Concepto.Contains("Origen") || t.Concepto.Contains("Fitosanitario") || t.Concepto.Contains("Sanitario") || t.Concepto.Contains("Captura")));

            if (tienePendientes || tieneIncompletos)
            {
                TempData["ErrorMsg"] = "Bloqueo de seguridad: Existen montos sin asignar o certificados INCOMPLETOS. Operaciones debe ingresar el Número de Documento.";
                return RedirectToPage("./ControlFinanciero");
            }

            if (idsExentos == null || !idsExentos.Any()) return RedirectToPage("./ControlFinanciero");

            var ultimasNc = await _context.TransaccionesFinancieras
                .Where(t => t.NumeroDocumento != null && t.NumeroDocumento.StartsWith("NC-"))
                .Select(t => t.NumeroDocumento)
                .ToListAsync();

            int maxCorrelativo = 3500;
            foreach (var nc in ultimasNc)
            {
                if (int.TryParse(nc.Replace("NC-", ""), out int num))
                {
                    if (num > maxCorrelativo) maxCorrelativo = num;
                }
            }

            string nuevoFolio = $"NC-{maxCorrelativo + 1}";

            var transacciones = await _context.TransaccionesFinancieras
                .Where(t => idsExentos.Contains(t.Id) && t.IdOperacion == operacionId)
                .ToListAsync();

            foreach (var tx in transacciones) tx.NumeroDocumento = nuevoFolio;
            await _context.SaveChangesAsync();

            return RedirectToPage("/Finanzas/NotaCobro", new { operacionId = operacionId, numeroDoc = nuevoFolio });
        }

        // --- BOTÓN 1: SUBIR FACTURA PROVEEDOR (PDF) ---
        public async Task<IActionResult> OnPostSubirFacturaAsync(int transaccionId, IFormFile archivoFactura)
        {
            if (archivoFactura != null && archivoFactura.Length > 0)
            {
                var transaccion = await _context.TransaccionesFinancieras
                    .Include(t => t.IdOperacionNavigation)
                    .FirstOrDefaultAsync(t => t.Id == transaccionId);

                if (transaccion != null)
                {
                    string safeBookingName = string.Join("_", (transaccion.IdOperacionNavigation?.NumeroBooking ?? "SIN-BOOKING").Split(Path.GetInvalidFileNameChars()));
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "facturas", safeBookingName);

                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"FACT_{Guid.NewGuid().ToString().Substring(0, 8)}_{archivoFactura.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivoFactura.CopyToAsync(fileStream);
                    }

                    // Se guarda el archivo visual pero NO cambia el estado a PAGADO
                    transaccion.RutaFactura = $"/uploads/facturas/{safeBookingName}/{uniqueFileName}";
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage("./ControlFinanciero");
        }

        // --- BOTÓN 2: SUBIR COMPROBANTE DE PAGO (TRANSFERENCIA) ---
        public async Task<IActionResult> OnPostSubirComprobanteAsync(int transaccionId, IFormFile archivoRespaldo)
        {
            if (archivoRespaldo != null && archivoRespaldo.Length > 0)
            {
                var transaccion = await _context.TransaccionesFinancieras
                    .Include(t => t.IdOperacionNavigation)
                    .FirstOrDefaultAsync(t => t.Id == transaccionId);

                if (transaccion != null)
                {
                    string safeBookingName = string.Join("_", (transaccion.IdOperacionNavigation?.NumeroBooking ?? "SIN-BOOKING").Split(Path.GetInvalidFileNameChars()));
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "comprobantes", safeBookingName);

                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"PAGO_{Guid.NewGuid().ToString().Substring(0, 8)}_{archivoRespaldo.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivoRespaldo.CopyToAsync(fileStream);
                    }

                    // El escudo: Sube el comprobante de transferencia y GATILLA el estado a PAGADO
                    transaccion.EstadoFila = "PAGADO";
                    transaccion.RutaComprobante = $"/uploads/comprobantes/{safeBookingName}/{uniqueFileName}";

                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage("./ControlFinanciero");
        }
    }
}