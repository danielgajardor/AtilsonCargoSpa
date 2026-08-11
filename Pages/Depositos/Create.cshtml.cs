using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.Depositos
{
    public class CreateModel : PageModel
    {
        private readonly AtilsonContext _context;

        public CreateModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Deposito Deposito { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["IdCiudad"] = new SelectList(await _context.Ciudades.ToListAsync(), "Id", "Nombre");
            ViewData["IdNaviera"] = new SelectList(await _context.Navieras.Where(n => n.Activo == 1).ToListAsync(), "Id", "NombreNaviera");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            Deposito.FechaCreacion = DateTime.Now;
            Deposito.FechaModificacion = DateTime.Now;
            Deposito.UsuarioCreador = "Admin Atilson";
            Deposito.UsuarioModificador = "Admin Atilson";
            Deposito.Activo = 1;

            _context.Depositos.Add(Deposito);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}