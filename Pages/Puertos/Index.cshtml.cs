using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using AtilsonCargoSpa.Helpers; // Asegúrate de tener la clase PaginatedList en esta carpeta

namespace AtilsonCargoSpa.Pages.Puertos
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        public PaginatedList<Puerto> Puertos { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            // Consulta base: solo puertos activos
            var query = _context.Puertos.Where(p => p.Activo == 1);

            // Filtro de búsqueda en tiempo real (Puerto o Terminal)
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.NombrePuerto.Contains(SearchString) ||
                                        (s.TerminalPortuario != null && s.TerminalPortuario.Contains(SearchString)));
            }

            // Ordenar alfabéticamente
            query = query.OrderBy(p => p.NombrePuerto);

            // Configuración de paginación (15 por página)
            int pageSize = 15;
            Puertos = await PaginatedList<Puerto>.CreateAsync(query.AsNoTracking(), pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var puerto = await _context.Puertos.FindAsync(id);

            if (puerto != null)
            {
                // Borrado Lógico: Cambiamos estado en lugar de eliminar la fila
                puerto.Activo = 0;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}