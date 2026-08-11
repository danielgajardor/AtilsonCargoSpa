using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.App.Clientes
{
    public class CreateModel : PageModel
    {
        private readonly AtilsonContext _context;

        public CreateModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Cliente Cliente { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            // Cargamos las ciudades para el formulario
            ViewData["IdCiudad"] = new SelectList(await _context.Ciudades.ToListAsync(), "Id", "Nombre");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            Cliente.FechaCreacion = DateTime.Now;
            Cliente.UsuarioCreador = "Admin Atilson";
            Cliente.Activo = 1;

            _context.Clientes.Add(Cliente);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}