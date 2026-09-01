using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.Finanzas
{
    public class SetCobroModel : PageModel
    {
        private readonly AtilsonContext _context;

        public SetCobroModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;

        public List<TransaccionesFinanciera> FacturasBase { get; set; } = new();
        public List<TransaccionesFinanciera> NotasCobroExtras { get; set; } = new();

        public decimal GranTotalUSD { get; set; }
        public decimal GranTotalCLP { get; set; }
        public decimal TotalPagadoUSD { get; set; }
        public decimal TotalPagadoCLP { get; set; }
        public decimal SaldoPendienteUSD { get; set; }
        public decimal SaldoPendienteCLP { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.IdNavieraNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.TransaccionesFinancieras)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (Operacion == null) return NotFound();

            // Solo cobramos al cliente lo que es un INGRESO para nosotros
            var ingresos = Operacion.TransaccionesFinancieras
                .Where(t => t.TipoMovimiento == "INGRESO" && t.MontoNeto > 0)
                .ToList();

            FacturasBase = ingresos.Where(t => t.GrupoCobro != "Extracosto").ToList();
            NotasCobroExtras = ingresos.Where(t => t.GrupoCobro == "Extracosto").ToList();

            GranTotalUSD = ingresos.Where(t => t.Moneda == "USD").Sum(t => t.MontoNeto);
            GranTotalCLP = ingresos.Where(t => t.Moneda == "CLP").Sum(t => t.MontoNeto);

            TotalPagadoUSD = ingresos.Where(t => t.Moneda == "USD" && (t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO")).Sum(t => t.MontoNeto);
            TotalPagadoCLP = ingresos.Where(t => t.Moneda == "CLP" && (t.EstadoFila == "PAGADO" || t.EstadoFila == "CONCILIADO")).Sum(t => t.MontoNeto);

            SaldoPendienteUSD = GranTotalUSD - TotalPagadoUSD;
            SaldoPendienteCLP = GranTotalCLP - TotalPagadoCLP;

            return Page();
        }
    }
}