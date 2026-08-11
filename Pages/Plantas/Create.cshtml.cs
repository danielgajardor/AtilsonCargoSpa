using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.Plantas
{
    public class CreateModel : PageModel
    {
        private readonly AtilsonContext _context;

        public CreateModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Origenescarga Planta { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["IdCiudad"] = new SelectList(await _context.Ciudades.ToListAsync(), "Id", "Nombre");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            Planta.FechaCreacion = DateTime.Now;
            Planta.FechaModificacion = DateTime.Now;
            Planta.UsuarioCreador = "Admin Atilson";
            Planta.UsuarioModificador = "Admin Atilson";
            Planta.Activo = 1;

            _context.Origenescargas.Add(Planta);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}