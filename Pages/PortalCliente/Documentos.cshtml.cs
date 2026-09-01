using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.PortalCliente
{
    public class DocumentosModel : PageModel
    {
        private readonly AtilsonContext _context;

        public DocumentosModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> OperacionesDocumentales { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var claimCliente = User.FindFirst("IdCliente")?.Value;
            if (string.IsNullOrEmpty(claimCliente) || !int.TryParse(claimCliente, out int idClienteLogueado))
            {
                return RedirectToPage("/Auth/Login");
            }

            var query = _context.Operaciones
                .Include(o => o.IdPuertoOrigenNavigation)
                .Include(o => o.IdPuertoDestinoNavigation)
                .Include(o => o.OperacionesDocumentales)
                .Where(o => o.IdCliente == idClienteLogueado && !o.IsDeleted && o.OperacionesDocumentales.Any());

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(o => o.NumeroBooking != null && o.NumeroBooking.Contains(SearchString));
            }

            OperacionesDocumentales = await query.OrderByDescending(o => o.Id).ToListAsync();

            return Page();
        }
    }
}