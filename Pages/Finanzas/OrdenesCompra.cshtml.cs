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

        // KPIs Inteligentes de Cuentas por Pagar (Proveedores)
        public int TotalOrdenes { get; set; }
        public decimal TotalAPagar { get; set; }
        public int OpsSinTarifa { get; set; }
        public int OpsListasEmitir { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Finanzasoperacions)
                .Include(o => o.IdPuertoOrigenNavigation) // Añadido para la ruta
                .Include(o => o.IdPuertoDestinoNavigation) // Añadido para la ruta
                .Where(o => !o.IsDeleted && o.OperacionesTerrestres.Any())
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                string s = SearchString.ToLower();
                query = query.Where(o =>
                    (o.OperacionesTerrestres.Any(t => t.EmpresaTransporte != null && t.EmpresaTransporte.ToLower().Contains(s))) ||
                    (o.NumeroBooking != null && o.NumeroBooking.ToLower().Contains(s))
                );
            }

            ListaOrdenes = await query.OrderByDescending(o => o.Id).ToListAsync();

            // Cálculo de KPIs para el Dashboard de Proveedores
            foreach (var o in ListaOrdenes)
            {
                TotalOrdenes++;
                var fin = o.Finanzasoperacions?.FirstOrDefault();

                bool tieneTarifa = fin != null && (fin.CostoTerrestreNeto ?? 0) > 0;

                if (tieneTarifa)
                {
                    TotalAPagar += (fin.CostoTerrestreNeto ?? 0);
                    OpsListasEmitir++;
                }
                else
                {
                    OpsSinTarifa++;
                }
            }
        }
    }
}