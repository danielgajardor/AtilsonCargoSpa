using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Threading.Tasks;
using System.Linq;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class ProformaPdfModel : PageModel
    {
        private readonly AtilsonContext _context;

        public ProformaPdfModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;
        public Finanzasoperacion Finanzas { get; set; } = default!;
        public decimal TotalVentaUSD { get; set; }
        public decimal TotalVentaCLP { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.ExtracostosOperacions)
                .Include(o => o.Unidadestecnicas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Operacion == null) return NotFound();

            Finanzas = await _context.Finanzasoperacions.FirstOrDefaultAsync(f => f.IdOperacion == id);

            // CÁLCULO DE TOTALES
            decimal fleteVenta = Finanzas != null ?
                ((Finanzas.VentaMaritimo ?? 0) + (Finanzas.VentaTerrestre ?? 0) + (Finanzas.VentaDocumental ?? 0)) : 0;

            decimal extrasUSD = Operacion.ExtracostosOperacions.Where(e => e.Moneda == "USD").Sum(e => e.Monto);
            decimal extrasCLP = Operacion.ExtracostosOperacions.Where(e => e.Moneda == "CLP").Sum(e => e.Monto);

            TotalVentaUSD = fleteVenta + extrasUSD;
            TotalVentaCLP = extrasCLP;

            return Page();
        }
    }
}