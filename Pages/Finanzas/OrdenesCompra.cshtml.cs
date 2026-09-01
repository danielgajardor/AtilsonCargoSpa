using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class OrdenesCompraModel : PageModel
    {
        private readonly AtilsonContext _context;

        public OrdenesCompraModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> ListaOrdenes { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.TransaccionesFinancieras)
                    .ThenInclude(t => t.IdProveedorNavigation)
                .Where(o => !o.IsDeleted && o.TransaccionesFinancieras.Any(t =>
                    t.TipoMovimiento == "EGRESO" &&
                    t.GrupoCobro != null &&
                    (t.GrupoCobro.ToUpper().Contains("TERRESTRE") ||
                     t.GrupoCobro.ToUpper().Contains("TRANSPORTE") ||
                     t.GrupoCobro.ToUpper().Contains("EXTRACOSTO"))))
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower();
                query = query.Where(o =>
                    (o.NumeroBooking != null && o.NumeroBooking.ToLower().Contains(s)) ||
                    (o.TransaccionesFinancieras.Any(t => t.IdProveedorNavigation != null && t.IdProveedorNavigation.NombreProveedor.ToLower().Contains(s)))
                );
            }

            ListaOrdenes = await query.OrderByDescending(o => o.Id).ToListAsync();
        }

        // ==================== ASIGNACIÓN AUTOMÁTICA DEL N° DE OC ====================
        public async Task<IActionResult> OnPostAsignarOCAsync(string transaccionesIds)
        {
            if (!string.IsNullOrWhiteSpace(transaccionesIds))
            {
                var ids = transaccionesIds.Split(',').Select(int.Parse).ToList();
                var transacciones = await _context.TransaccionesFinancieras
                                        .Where(t => ids.Contains(t.Id))
                                        .ToListAsync();

                // 1. Buscar el correlativo más alto de OC actuales
                var ultimasOc = await _context.TransaccionesFinancieras
                    .Where(t => t.NumeroOrdenCompra != null && t.NumeroOrdenCompra.StartsWith("OC-"))
                    .Select(t => t.NumeroOrdenCompra)
                    .ToListAsync();

                int maxCorrelativo = 2000;
                foreach (var oc in ultimasOc)
                {
                    if (int.TryParse(oc.Replace("OC-", ""), out int num))
                    {
                        if (num > maxCorrelativo) maxCorrelativo = num;
                    }
                }

                // 2. Generar el nuevo folio
                string nuevaOc = $"OC-{maxCorrelativo + 1}";

                // 3. Asignarle el mismo número correlativo a toda la agrupación
                foreach (var tx in transacciones)
                {
                    tx.NumeroOrdenCompra = nuevaOc;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./OrdenesCompra");
        }
    }
}