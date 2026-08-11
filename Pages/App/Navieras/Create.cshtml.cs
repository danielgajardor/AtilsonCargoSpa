using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AtilsonCargoSpa.Models;

namespace AtilsonCargoSpa.Pages.App.Navieras
{
    public class CreateModel : PageModel
    {
        private readonly AtilsonContext _context;

        public CreateModel(AtilsonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Naviera Naviera { get; set; } = default!;

        public IActionResult OnGet()
        {
            // Inicializamos valores por defecto
            Naviera = new Naviera { Activo = 1, ColorRepresentativo = "#6c757d" };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Navieras.Add(Naviera);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}