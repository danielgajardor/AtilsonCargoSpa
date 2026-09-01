using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class EditModel : PageModel
    {
        private readonly AtilsonContext _context;

        public EditModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione OperacionBase { get; set; } = default!;
        public List<TransaccionesFinanciera> Ingresos { get; set; } = new();
        public List<TransaccionesFinanciera> Egresos { get; set; } = new();

        public decimal IngresosUSD { get; set; }
        public decimal IngresosCLP { get; set; }
        public decimal EgresosUSD { get; set; }
        public decimal EgresosCLP { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            OperacionBase = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.TransaccionesFinancieras)
                    .ThenInclude(t => t.IdProveedorNavigation)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (OperacionBase == null) return NotFound();

            Ingresos = OperacionBase.TransaccionesFinancieras.Where(t => t.TipoMovimiento == "INGRESO").ToList();
            Egresos = OperacionBase.TransaccionesFinancieras.Where(t => t.TipoMovimiento == "EGRESO").ToList();

            IngresosUSD = Ingresos.Where(t => t.Moneda == "USD").Sum(t => t.MontoNeto);
            IngresosCLP = Ingresos.Where(t => t.Moneda == "CLP").Sum(t => t.MontoNeto);
            EgresosUSD = Egresos.Where(t => t.Moneda == "USD").Sum(t => t.MontoNeto);
            EgresosCLP = Egresos.Where(t => t.Moneda == "CLP").Sum(t => t.MontoNeto);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string AccionFinanzas)
        {
            var op = await _context.Operaciones.FindAsync(id);
            if (op == null) return NotFound();

            if (op.LockFinanzas && AccionFinanzas != "Desbloquear")
            {
                TempData["ErrorMsg"] = "La operación está liquidada y bloqueada.";
                return RedirectToPage(new { id = id });
            }

            var txs = await _context.TransaccionesFinancieras.Where(t => t.IdOperacion == id).ToListAsync();

            // Guardar cambios en las transacciones editadas en la vista
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("monto_")))
            {
                int txId = int.Parse(key.Replace("monto_", ""));
                var tx = txs.FirstOrDefault(t => t.Id == txId);
                if (tx != null)
                {
                    string montoStr = Request.Form[key].ToString().Replace(",", ".");
                    if (decimal.TryParse(montoStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal m))
                    {
                        tx.MontoNeto = m;
                    }
                    tx.Moneda = Request.Form[$"moneda_{txId}"];
                    tx.NumeroDocumento = Request.Form[$"numdoc_{txId}"];

                    if (tx.MontoNeto > 0 && tx.EstadoFila == "PENDIENTE VALORIZAR")
                        tx.EstadoFila = "PROVISIÓN";

                    tx.FechaModificacion = DateTime.Now;
                    tx.UsuarioModificador = User.Identity?.Name ?? "Finanzas";
                }
            }

            if (AccionFinanzas == "Liquidar")
            {
                op.LockFinanzas = true;
                op.Comentarios = $"[{DateTime.Now:dd/MM/yyyy HH:mm} FINANZAS] Operación Liquidada Exitosamente.\n" + (op.Comentarios ?? "");
                TempData["SuccessMsg"] = "La operación ha sido Liquidada y los valores cerrados con éxito.";
            }
            else if (AccionFinanzas == "Desbloquear")
            {
                op.LockFinanzas = false;
                op.Comentarios = $"[{DateTime.Now:dd/MM/yyyy HH:mm} FINANZAS] Operación Desbloqueada.\n" + (op.Comentarios ?? "");
                TempData["SuccessMsg"] = "Operación desbloqueada correctamente.";
            }
            else
            {
                TempData["SuccessMsg"] = "Borrador financiero guardado correctamente.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}