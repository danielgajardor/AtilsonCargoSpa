using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Threading.Tasks;
using System.Linq;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class ReservaPdfModel : PageModel
    {
        private readonly AtilsonContext _context;

        public ReservaPdfModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;
        public OperacionesTerrestre Transporte { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.Unidadestecnicas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Operacion == null) return NotFound();

            Transporte = Operacion.OperacionesTerrestres?.FirstOrDefault() ?? new OperacionesTerrestre();

            return Page();
        }
    }
}