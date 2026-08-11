using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.App.Clientes
{
    public class EditModel : PageModel
    {
        private readonly AtilsonContext _context;

        public EditModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Cliente Cliente { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // AQUI ESTA LA MAGIA: Agregamos el .Include para traer sus tarifas
            var cliente = await _context.Clientes
                .Include(c => c.TarifasClientes) // <--- Agrega esta línea
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }
            Cliente = cliente;

            // Cargamos las ciudades para el desplegable
            ViewData["IdCiudad"] = new SelectList(_context.Ciudades, "Id", "Nombre");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Marcamos el objeto como modificado
            _context.Attach(Cliente).State = EntityState.Modified;

            // Actualizamos auditoría
            Cliente.FechaModificacion = DateTime.Now;
            Cliente.UsuarioModificador = "Admin Atilson";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(Cliente.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.Id == id);
        }
    }
}