using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.Plantas
{
    public class EditModel : PageModel
    {
        private readonly AtilsonContext _context;

        public EditModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Planta Planta { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var origen = await _context.Plantas.FirstOrDefaultAsync(m => m.Id == id);
            if (origen == null) return NotFound();

            Planta = origen;
            ViewData["IdCiudad"] = new SelectList(await _context.Ciudades.ToListAsync(), "Id", "Nombre");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Planta).State = EntityState.Modified;

            Planta.FechaModificacion = DateTime.Now;
            Planta.UsuarioModificador = "Admin Atilson";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlantaExists(Planta.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool PlantaExists(int id)
        {
            return _context.Plantas.Any(e => e.Id == id);
        }
    }
}