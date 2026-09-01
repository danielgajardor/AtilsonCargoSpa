using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class NotaCobroModel : PageModel
    {
        private readonly AtilsonContext _context;

        public NotaCobroModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;
        public List<TransaccionesFinanciera> CobrosConsolidados { get; set; } = new();
        public string NumeroNotaCobro { get; set; } = string.Empty;
        public decimal TotalCobro { get; set; }
        public string MonedaPrincipal { get; set; } = "CLP";

        public async Task<IActionResult> OnGetAsync(int operacionId, string numeroDoc)
        {
            if (operacionId <= 0 || string.IsNullOrEmpty(numeroDoc)) return NotFound();

            NumeroNotaCobro = numeroDoc.Replace("NC-", "");

            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .FirstOrDefaultAsync(o => o.Id == operacionId);

            if (Operacion == null) return NotFound();

            // Rescatamos exactamente las filas que Jane unificó bajo este folio
            CobrosConsolidados = await _context.TransaccionesFinancieras
                .Where(t => t.IdOperacion == operacionId && t.NumeroDocumento == numeroDoc)
                .ToListAsync();

            TotalCobro = CobrosConsolidados.Sum(t => t.MontoNeto);

            // Asumimos la moneda del primer cobro (usualmente son todas de la misma)
            if (CobrosConsolidados.Any())
            {
                MonedaPrincipal = CobrosConsolidados.First().Moneda;
            }

            return Page();
        }
    }
}