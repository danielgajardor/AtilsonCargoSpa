using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AtilsonCargoSpa.Pages.Operaciones
{
    public class OrdenCompraPdfModel : PageModel
    {
        private readonly AtilsonContext _context;

        public OrdenCompraPdfModel(AtilsonContext context)
        {
            _context = context;
        }

        public Operacione Operacion { get; set; } = default!;
        public OperacionesTerrestre Terrestre { get; set; } = default!;
        public List<TransaccionesFinanciera> TransaccionesTerrestres { get; set; } = new();

        public decimal Neto { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public string NumeroOC { get; set; } = "";

        // Se agrega 'oc' como parámetro para atraparlo directamente desde la URL
        public async Task<IActionResult> OnGetAsync(int? id, string? oc)
        {
            if (id == null || string.IsNullOrWhiteSpace(oc)) return NotFound();

            NumeroOC = oc.Trim().ToUpper();

            Operacion = await _context.Operaciones
                .Include(o => o.IdClienteNavigation)
                .Include(o => o.OperacionesTerrestres)
                .Include(o => o.TransaccionesFinancieras)
                    .ThenInclude(t => t.IdProveedorNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Operacion == null) return NotFound();

            Terrestre = Operacion.OperacionesTerrestres.FirstOrDefault() ?? new OperacionesTerrestre();

            // Filtrado ESTRICTO: Solo trae las transacciones de esta Orden de Compra específica
            TransaccionesTerrestres = Operacion.TransaccionesFinancieras
                .Where(t => t.TipoMovimiento == "EGRESO" && t.NumeroOrdenCompra == NumeroOC)
                .ToList();

            if (!TransaccionesTerrestres.Any()) return NotFound("No se encontraron costos para esta Orden de Compra.");

            // Cálculos Dinámicos
            Neto = TransaccionesTerrestres.Sum(t => t.MontoNeto);
            Iva = Neto * 0.19m;
            Total = Neto + Iva;

            return Page();
        }
    }
}