using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.Depositos
{
    public class EditModel : PageModel
    {
        private readonly AtilsonContext _context;

        public EditModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Deposito Deposito { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var deposito = await _context.Depositos.FirstOrDefaultAsync(m => m.Id == id);
            if (deposito == null) return NotFound();

            Deposito = deposito;

            ViewData["IdCiudad"] = new SelectList(await _context.Ciudades.ToListAsync(), "Id", "Nombre");
            ViewData["IdNaviera"] = new SelectList(await _context.Navieras.Where(n => n.Activo == 1 || n.Id == Deposito.IdNaviera).ToListAsync(), "Id", "NombreNaviera");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Deposito).State = EntityState.Modified;

            Deposito.FechaModificacion = DateTime.Now;
            Deposito.UsuarioModificador = "Admin Atilson";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepositoExists(Deposito.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool DepositoExists(int id)
        {
            return _context.Depositos.Any(e => e.Id == id);
        }
    }
}