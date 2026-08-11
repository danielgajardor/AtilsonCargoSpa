using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.Puertos
{
    public class CreateModel : PageModel
    {
        private readonly AtilsonContext _context;

        public CreateModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Puerto Puerto { get; set; } = default!;

        public IActionResult OnGet()
        {
            // Inicializamos valores por defecto para que nazca "Activo"
            Puerto = new Puerto { Activo = 1 };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Puertos.Add(Puerto);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}