using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using AtilsonCargoSpa.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Plantas
{
    public class IndexModel : PageModel
    {
        private readonly AtilsonContext _context;

        public IndexModel(AtilsonContext context)
        {
            _context = context;
        }

        public PaginatedList<Planta> Plantas { get; set; } = default!;

        public Dictionary<int, string> CiudadesDict { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            CiudadesDict = await _context.Ciudades.ToDictionaryAsync(c => c.Id, c => c.Nombre);

            // CORRECCIÓN AQUÍ: Cambiamos p.Activo == 1 por p.Activo == true
            var query = _context.Plantas.Where(p => p.Activo == true || p.Activo == null);

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.Nombre.Contains(SearchString) || s.Direccion.Contains(SearchString));
            }

            query = query.OrderByDescending(p => p.Id);

            int pageSize = 10;
            Plantas = await PaginatedList<Planta>.CreateAsync(query.AsNoTracking(), pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var planta = await _context.Plantas.FindAsync(id);
            if (planta != null)
            {
                // CORRECCIÓN AQUÍ TAMBIÉN: Cambiamos 0 por false si el modelo lo exige, 
                // pero si el error solo fue en el == 1, dejaremos el false por seguridad.
                // NOTA: Si al compilar te da error en esta línea de "planta.Activo = false", 
                // cámbialo a planta.Activo = false; (dependerá de si es bool o int).
                // Como tu error fue de comparación, asumo que es bool.
                planta.Activo = false;
                planta.FechaModificacion = DateTime.Now;
                planta.UsuarioModificador = "Admin Atilson";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}