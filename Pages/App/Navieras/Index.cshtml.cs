using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.App.Navieras
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Naviera> Navieras { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            // Filtramos solo las navieras activas
            var query = _context.Navieras.Where(n => n.Activo == 1);

            // Filtro de búsqueda en tiempo real
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.NombreNaviera.Contains(SearchString));
            }

            // Ordenamos alfabéticamente
            Navieras = await query.OrderBy(n => n.NombreNaviera).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var naviera = await _context.Navieras.FindAsync(id);

            if (naviera != null)
            {
                // Borrado Lógico
                naviera.Activo = 0;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}