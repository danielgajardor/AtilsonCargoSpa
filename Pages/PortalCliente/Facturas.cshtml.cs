using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtilsonCargoSpa.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtilsonCargoSpa.Pages.PortalCliente
{
    public class FacturasModel : PageModel
    {
        private readonly AtilsonContext _context;

        public FacturasModel(AtilsonContext context)
        {
            _context = context;
        }

        public IList<Operacione> OperacionesFinancieras { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaDesde { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaHasta { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public decimal TotalFacturadoGlobal { get; set; }
        public decimal TotalPendienteGlobal { get; set; }

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
                .Include(o => o.TransaccionesFinancieras)
                .Where(o => o.IdCliente == idClienteLogueado && !o.IsDeleted &&
                            o.TransaccionesFinancieras.Any(t => t.TipoMovimiento == "INGRESO"));

            if (FechaDesde.HasValue) query = query.Where(o => o.FechaCreacion >= FechaDesde.Value);
            if (FechaHasta.HasValue) query = query.Where(o => o.FechaCreacion <= FechaHasta.Value.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(o => o.NumeroBooking != null && o.NumeroBooking.Contains(SearchString));
            }

            OperacionesFinancieras = await query.OrderByDescending(o => o.Id).ToListAsync();

            var txs = OperacionesFinancieras.SelectMany(o => o.TransaccionesFinancieras.Where(t => t.TipoMovimiento == "INGRESO" && t.Moneda == "USD")).ToList();

            TotalFacturadoGlobal = txs.Where(t => t.EstadoFila == "FACTURADO" || t.EstadoFila == "PAGADO").Sum(t => t.MontoNeto);
            TotalPendienteGlobal = txs.Where(t => t.EstadoFila != "PAGADO").Sum(t => t.MontoNeto);

            return Page();
        }
    }
}