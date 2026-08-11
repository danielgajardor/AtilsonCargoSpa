using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Threading.Tasks;
using System.Linq;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class OrdenCompraPdfModel : PageModel
    {
        private readonly AtilsonContext _context;

        public OrdenCompraPdfModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;
        public OperacionesTerrestre Terrestre { get; set; } = default!;
        public Finanzasoperacion Finanzas { get; set; } = default!;

        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Finanzasoperacions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Operacion == null) return NotFound();

            Terrestre = Operacion.OperacionesTerrestres.FirstOrDefault() ?? new OperacionesTerrestre();
            Finanzas = Operacion.Finanzasoperacions.FirstOrDefault() ?? new Finanzasoperacion();

            // Cálculos financieros (Neto + 19% IVA)
            Neto = Finanzas.CostoTerrestreNeto ?? 0m;
            Iva = Neto * 0.19m;
            Total = Neto + Iva;

            return Page();
        }
    }
}